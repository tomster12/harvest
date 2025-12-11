using UnityEngine;

public class ChoppableTree : MonoBehaviour
{
    public bool CanChop { get; private set; } = true;

    public void Hit(Vector3 hitWorld, float depth, float width, float height)
    {
        Vector2Int hitGrid = WorldToGrid(hitWorld);

        int widthGrid = Mathf.CeilToInt(width * gridColDensity);
        int heightGrid = Mathf.CeilToInt(height * gridRowDensity);

        for (int dCol = -widthGrid; dCol <= widthGrid; dCol++)
        {
            int col = (hitGrid.x + dCol + gridCols) % gridCols;

            for (int dRow = -heightGrid; dRow <= heightGrid; dRow++)
            {
                int row = hitGrid.y + dRow;
                if (row < 0 || row >= gridRows) continue;

                float colDist = Mathf.Abs(dCol) / (float)widthGrid;
                float rowDist = Mathf.Abs(dRow) / (float)heightGrid;
                float dist = Mathf.Sqrt(colDist * colDist + rowDist * rowDist);
                if (dist > 1f) continue;

                float fallOff = 1f - dist;
                currentGrid[col, row] = Mathf.Max(meshMinRadius, currentGrid[col, row] - depth * fallOff);
            }
        }

        if (CheckForSplitCondition(hitGrid.y - heightGrid, hitGrid.y + heightGrid))
        {
            PerformSplit(hitGrid.y);
        }

        RegenerateMeshFromGrid();
    }

    [Header("References")]
    [SerializeField] private MeshFilter mf;
    [SerializeField] private MeshCollider mc;
    [SerializeField] private Rigidbody rb;

    [Header("Grid")]
    [SerializeField] private float gridColDensity = 20f;
    [SerializeField] private float gridRowDensity = 20f;
    [SerializeField] private float radiusNoiseScale = 1f;
    [SerializeField] private float radiusNoiseStrength = 0.25f;
    [SerializeField] private int radiusNoiseSeed = 0;

    [Header("Mesh Generation")]
    [SerializeField] private float meshBaseRadius = 0.35f;
    [SerializeField] private float meshMinRadius = 0.05f;
    [SerializeField] private float meshGenHeight = 4f;
    [SerializeField] private float meshGenHorzNoiseScale = 10.0f;
    [SerializeField] private float meshGenVertNoiseScale = 10.0f;
    [SerializeField] private float meshGenHorzNoiseStrength = 0.04f;
    [SerializeField] private float meshGenVertNoiseStrength = 0.04f;

    [Header("Splitting")]
    [SerializeField] private float splitWidthRequired = 0.25f;

    private int gridCols;
    private int gridRows;
    private float meshHeight;
    private float[,] currentGrid;
    private float[,] baseGrid;
    private Mesh mesh;

    private float MeshBaseCircumference => 2f * Mathf.PI * meshBaseRadius;
    private int SplitColRequired => Mathf.CeilToInt(splitWidthRequired * gridColDensity);

    private void Awake()
    {
        rb.isKinematic = true;
        CanChop = true;
        GenerateTree();
    }

    [ContextMenu("Generate Tree")]
    public void GenerateTree()
    {
        GenerateTreeGrid();
        RegenerateMeshFromGrid();
    }

    private void GenerateTreeGrid()
    {
        meshHeight = meshGenHeight;
        gridCols = Mathf.Max(8, Mathf.CeilToInt(MeshBaseCircumference * gridColDensity));
        gridRows = Mathf.Max(4, Mathf.CeilToInt(meshHeight * gridRowDensity));

        baseGrid = new float[gridCols, gridRows];
        currentGrid = new float[gridCols, gridRows];

        for (int c = 0; c < gridCols; c++)
        {
            for (int r = 0; r < gridRows; r++)
            {
                float angle = (c / (float)gridCols) * Mathf.PI * 2f;
                float nx = Mathf.Cos(angle);
                float nz = Mathf.Sin(angle) + ((float)r / (gridRows - 1));

                float noise = Mathf.PerlinNoise(nx * radiusNoiseScale + radiusNoiseSeed, nz * radiusNoiseScale + radiusNoiseSeed);
                noise = (noise - 0.5f) * radiusNoiseStrength;

                baseGrid[c, r] = meshBaseRadius + noise;
                currentGrid[c, r] = baseGrid[c, r];
            }
        }
    }

    private void RegenerateMeshFromGrid()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        else mesh.Clear();

        int capVertCount = gridCols + 1;
        int capTriCount = gridCols * 3;
        int trunkVertCount = gridCols * gridRows;
        int trunkTriCount = gridCols * (gridRows - 1) * 6;

        int totalVertCount = trunkVertCount + capVertCount * 2;
        int totalTriCount = trunkTriCount + capTriCount * 2;

        Vector3[] vertices = new Vector3[totalVertCount];
        Vector3[] normals = new Vector3[totalVertCount];
        Vector2[] uvs = new Vector2[totalVertCount];
        int[] tris = new int[totalTriCount];

        int vertIndex = 0;
        int triIndex = 0;

        Vector2 GetUV(int c, int r)
        {
            return new Vector2(1.0f - currentGrid[c, r] / baseGrid[c, r], 0);
        }

        (Vector3 vertex, Vector3 normal) GetTrunkPoint(int c, int r)
        {
            Vector3 vertex = GridToLocal(c, r);
            Vector3 normal = new Vector3(vertex.x, 0f, vertex.z).normalized;
            Vector3 tangent = new Vector3(-normal.z, 0f, normal.x);

            // Noise is cyclic around the trunk and offset along height
            float angle = ((float)c / (float)gridCols) * (Mathf.PI * 2f);
            float nx = Mathf.Cos(angle);
            float nz = Mathf.Sin(angle) + ((float)r / (gridRows - 1));

            float perpNoise = Mathf.PerlinNoise(nx * meshGenHorzNoiseScale + 500f, nz * meshGenHorzNoiseScale + 600f);
            float vertNoise = Mathf.PerlinNoise(nx * meshGenVertNoiseScale + 700f, nz * meshGenVertNoiseScale + 800f);

            float perpOffset = (perpNoise - 0.5f) * meshGenHorzNoiseStrength;
            float vertOffset = (vertNoise - 0.5f) * meshGenVertNoiseStrength;
            vertex += perpOffset * tangent + vertOffset * Vector3.up;

            return (vertex, normal);
        }

        // -------------------------- Trunk --------------------------

        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                (Vector3 vertex, Vector3 normal) = GetTrunkPoint(c, r);

                vertices[vertIndex] = vertex;
                normals[vertIndex] = normal;
                uvs[vertIndex] = GetUV(c, r);

                vertIndex++;
            }
        }

        for (int r = 0; r < gridRows - 1; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                int cn = (c + 1) % gridCols;

                int A = c + r * gridCols;
                int B = cn + r * gridCols;
                int C = c + (r + 1) * gridCols;
                int D = cn + (r + 1) * gridCols;

                tris[triIndex++] = A;
                tris[triIndex++] = C;
                tris[triIndex++] = B;

                tris[triIndex++] = B;
                tris[triIndex++] = C;
                tris[triIndex++] = D;
            }
        }

        // -------------------------- Caps --------------------------

        // Bottom Cap
        int bottomCenterIndex = vertIndex;
        vertices[vertIndex] = new Vector3(0, 0, 0);
        normals[vertIndex] = Vector3.down;
        uvs[vertIndex] = new Vector2(1f, 0f);
        vertIndex++;

        for (int c = 0; c < gridCols; c++)
        {
            (Vector3 vertex, Vector3 _) = GetTrunkPoint(c, 0);

            vertices[vertIndex] = vertex;
            normals[vertIndex] = Vector3.down;
            uvs[vertIndex] = GetUV(c, 0);
            vertIndex++;
        }

        for (int c = 0; c < gridCols; c++)
        {
            int ringA = bottomCenterIndex + 1 + c;
            int ringB = bottomCenterIndex + 1 + ((c + 1) % gridCols);

            tris[triIndex++] = bottomCenterIndex;
            tris[triIndex++] = ringA;
            tris[triIndex++] = ringB;
        }

        // Top Cap
        int topCenterIndex = vertIndex;
        float yTop = meshHeight;
        vertices[vertIndex] = new Vector3(0, yTop, 0);
        normals[vertIndex] = Vector3.up;
        uvs[vertIndex] = new Vector2(1f, 0f);
        vertIndex++;

        for (int c = 0; c < gridCols; c++)
        {
            (Vector3 vertex, Vector3 _) = GetTrunkPoint(c, gridRows - 1);

            vertices[vertIndex] = vertex;
            normals[vertIndex] = Vector3.up;
            uvs[vertIndex] = GetUV(c, gridRows - 1);
            vertIndex++;
        }

        for (int c = 0; c < gridCols; c++)
        {
            int ringA = topCenterIndex + 1 + c;
            int ringB = topCenterIndex + 1 + ((c + 1) % gridCols);

            tris[triIndex++] = topCenterIndex;
            tris[triIndex++] = ringB;
            tris[triIndex++] = ringA;
        }

        // -------------------------- Finish --------------------------

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = tris;
        mesh.uv = uvs;

        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }

    private Vector2Int WorldToGrid(Vector3 posWorld)
    {
        Vector3 local = transform.InverseTransformPoint(posWorld);

        float angle = Mathf.Atan2(local.z, local.x);
        if (angle < 0) angle += Mathf.PI * 2f;

        int col = Mathf.FloorToInt(angle / (2f * Mathf.PI) * gridCols);
        int row = Mathf.FloorToInt(Mathf.Clamp01(local.y / meshHeight) * (gridRows - 1));

        return new Vector2Int(col, row);
    }

    private Vector3 GridToLocal(int col, int row)
    {
        float angle = ((float)col / (float)gridCols) * Mathf.PI * 2f;
        float radius = currentGrid[col, row];
        float y = ((float)row / (float)(gridRows - 1)) * meshHeight;
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(x, y, z);
    }

    private bool CheckForSplitCondition(int minRow, int rowMax)
    {
        minRow = Mathf.Clamp(minRow, 0, gridRows - 1);
        rowMax = Mathf.Clamp(rowMax, 0, gridRows - 1);

        for (int row = minRow; row <= rowMax; row++)
        {
            if (CheckRowForSplit(row)) return true;
        }
        return false;
    }

    private bool CheckRowForSplit(int row)
    {
        const float epsilon = 0.0001f;
        int runCount = 0;

        // Loop round the grid columns twice to ensure we check all wrap-around cases
        for (int c = 0; c < gridCols * 2; c++)
        {
            if (currentGrid[c % gridCols, row] <= meshMinRadius + epsilon) runCount++;
            else runCount = 0;
            if (runCount >= SplitColRequired) return true;
        }

        return false;
    }

    private void PerformSplit(int splitRow)
    {
        int bottomRowCount = splitRow + 1;
        int topRowCount = gridRows - bottomRowCount + 1;
        if (topRowCount <= 0) return;

        // Slice current + base grids
        float[,] bottomCurrentGrid = SliceGrid(currentGrid, 0, bottomRowCount);
        float[,] bottomBaseGrid = SliceGrid(baseGrid, 0, bottomRowCount);
        float[,] topCurrentGrid = SliceGrid(currentGrid, bottomRowCount - 1, topRowCount);
        float[,] topBaseGrid = SliceGrid(baseGrid, bottomRowCount - 1, topRowCount);

        float heightPerRow = meshHeight / gridRows;

        // Update this instance to bottom half
        currentGrid = bottomCurrentGrid;
        baseGrid = bottomBaseGrid;
        gridRows = bottomRowCount;
        meshHeight = heightPerRow * bottomRowCount;
        CanChop = false;
        RegenerateMeshFromGrid();

        // Spawn a new instance for the top half
        Vector3 spawnPos = transform.position + (bottomRowCount - 1) * heightPerRow * Vector3.up;
        GameObject go = Instantiate(gameObject, spawnPos, transform.rotation);
        ChoppableTree topTree = go.GetComponent<ChoppableTree>();
        topTree.InitFromSplit(
            topCurrentGrid,
            topBaseGrid,
            gridCols,
            topRowCount,
            heightPerRow * topRowCount
        );

        // Slight upward nudge to avoid collider intersection
        go.transform.position += Vector3.up * 0.001f;
    }

    public void InitFromSplit(float[,] currentGrid, float[,] baseGrid, int gridCols, int gridRows, float meshHeight)
    {
        this.gridCols = gridCols;
        this.gridRows = gridRows;
        this.meshHeight = meshHeight;
        this.currentGrid = currentGrid;
        this.baseGrid = baseGrid;

        RegenerateMeshFromGrid();

        // Enable physics
        rb.isKinematic = false;
        CanChop = false;
    }

    private float[,] SliceGrid(float[,] src, int startRow, int rowCount)
    {
        int cols = src.GetLength(0);
        float[,] output = new float[cols, rowCount];

        for (int r = 0; r < cols; r++)
        {
            for (int c = 0; c < rowCount; c++)
            {
                output[r, c] = src[r, startRow + c];
            }
        }
        return output;
    }
}

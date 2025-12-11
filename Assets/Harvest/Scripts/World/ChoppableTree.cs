using UnityEngine;

public sealed class TreeGrid
{
    public int Cols;
    public int Rows;
    public float ColDensity;
    public float RowDensity;
    public float Height;
    public float MinRadius;
    public float SplitWidthRequirement;
    public int SplitColRequirement;
    public float[,] Radius;
    public float[,] BaseRadius;
    public Vector3[,] Offsets;

    public static TreeGrid FromDensity(float colDensity, float rowDensity, float radius, float height, float minRadius, float splitWidthRequired)
    {
        var cols = Mathf.Max(8, Mathf.CeilToInt((2f * Mathf.PI * radius) * colDensity));
        var rows = Mathf.Max(4, Mathf.CeilToInt(height * rowDensity));

        return new(cols, rows, colDensity, rowDensity, height, minRadius, splitWidthRequired);
    }

    public TreeGrid(int cols, int rows, float colDensity, float rowDensity, float height, float minRadius, float splitWidthRequired)
    {
        Cols = cols;
        Rows = rows;
        ColDensity = colDensity;
        RowDensity = rowDensity;
        Height = height;
        MinRadius = minRadius;
        SplitWidthRequirement = splitWidthRequired;
        SplitColRequirement = Mathf.CeilToInt(splitWidthRequired * colDensity);
        Radius = new float[Cols, Rows];
        BaseRadius = new float[Cols, Rows];
        Offsets = new Vector3[Cols, Rows];
    }

    public void GenerateBaseRadius(float baseRadius, float noiseScale, float noiseStrength, int seed)
    {
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                float angle = (c / (float)Cols) * Mathf.PI * 2f;
                float nx = Mathf.Cos(angle);
                float nz = Mathf.Sin(angle) + (r / (float)(Rows - 1));
                float radiusNoise = Mathf.PerlinNoise(nx * noiseScale + seed, nz * noiseScale + seed);
                radiusNoise = (radiusNoise - 0.5f) * noiseStrength;

                float rad = baseRadius + radiusNoise;
                BaseRadius[c, r] = rad;
                Radius[c, r] = rad;
            }
        }
    }

    public void GeneratePerVertexOffsets(float horzScale, float horzStrength, float vertScale, float vertStrength)
    {
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                float angle = (c / (float)Cols) * Mathf.PI * 2f;
                float nx = Mathf.Cos(angle);
                float nz = Mathf.Sin(angle) + (r / (float)(Rows - 1));
                float p = Mathf.PerlinNoise(nx * horzScale + 500f, nz * horzScale + 600f);
                float v = Mathf.PerlinNoise(nx * vertScale + 700f, nz * vertScale + 800f);
                float perpOffset = (p - 0.5f) * horzStrength;
                float vertOffset = (v - 0.5f) * vertStrength;

                Offsets[c, r] = new(perpOffset, vertOffset, 0f);
            }
        }
    }

    public bool ApplyHit(Vector2Int hitGrid, float depth, float width, float height)
    {
        int hitCols = Mathf.CeilToInt(width * ColDensity);
        int hitRows = Mathf.CeilToInt(height * RowDensity);

        for (int dc = -hitCols; dc <= hitCols; dc++)
        {
            int c = (hitGrid.x + dc + Cols) % Cols;

            for (int dr = -hitRows; dr <= hitRows; dr++)
            {
                int r = hitGrid.y + dr;
                if (r < 0 || r >= Rows) continue;

                float colDist = Mathf.Abs(dc) / (float)hitCols;
                float rowDist = Mathf.Abs(dr) / (float)hitRows;
                float dist = Mathf.Sqrt(colDist * colDist + rowDist * rowDist);
                if (dist > 1f) continue;

                float falloff = 1f - dist;
                Radius[c, r] = Mathf.Max(MinRadius, Radius[c, r] - depth * falloff);
            }
        }

        return CheckSplit(hitGrid.y - hitRows, hitGrid.y + hitRows);
    }

    public bool CheckSplit(int lowRow, int highRow)
    {
        lowRow = Mathf.Clamp(lowRow, 0, Rows - 1);
        highRow = Mathf.Clamp(highRow, 0, Rows - 1);

        for (int r = lowRow; r <= highRow; r++)
        {
            if (CheckRowSplitRequirement(r)) return true;
        }

        return false;
    }

    private bool CheckRowSplitRequirement(int r)
    {
        const float eps = 0.0001f;
        int runCount = 0;
        for (int c = 0; c < Cols * 2; c++)
        {
            if (Radius[c % Cols, r] <= MinRadius + eps) runCount++;
            else runCount = 0;
            if (runCount >= SplitColRequirement) return true;
        }
        return false;
    }

    public (TreeGrid bottom, TreeGrid top) SplitAtRow(int splitRow)
    {
        int bottomRows = splitRow + 1;
        int topRows = Rows - bottomRows + 1;
        if (topRows <= 0) return (this, null);

        float heightPerRow = Height / Rows;
        var bottom = Slice(0, bottomRows, bottomRows * heightPerRow);
        var top = Slice(bottomRows - 1, topRows, topRows * heightPerRow);
        return (bottom, top);
    }

    private TreeGrid Slice(int rowStart, int rowCount, float newHeight)
    {
        var newGrid = new TreeGrid(Cols, rowCount, ColDensity, RowDensity, newHeight, MinRadius, SplitWidthRequirement);
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < rowCount; r++)
            {
                newGrid.Radius[c, r] = Radius[c, rowStart + r];
                newGrid.BaseRadius[c, r] = BaseRadius[c, rowStart + r];
                newGrid.Offsets[c, r] = Offsets[c, rowStart + r];
            }
        }
        return newGrid;
    }
}

public class ChoppableTree : MonoBehaviour
{
    public bool CanChop { get; private set; } = true;

    public void Hit(Vector3 posWorld, float depth, float width, float height)
    {
        Vector2Int hit = WorldToGrid(posWorld);

        bool causesSplit = grid.ApplyHit(hit, depth, width, height);

        if (causesSplit)
        {
            var (bottomGrid, topGrid) = grid.SplitAtRow(hit.y);
            grid = bottomGrid;
            CanChop = false;
            RebuildMesh();
            if (topGrid != null) SpawnTop(topGrid);
            return;
        }

        RebuildMesh();
    }

    [Header("References")]
    [SerializeField] private MeshFilter mf;
    [SerializeField] private MeshCollider mc;
    [SerializeField] private Rigidbody rb;

    [Header("Config")]
    [SerializeField] private float genColDensity = 20f;
    [SerializeField] private float genRowDensity = 20f;
    [SerializeField] private float genRadiusNoiseScale = 1f;
    [SerializeField] private float genRadiusNoiseStrength = 0.25f;
    [SerializeField] private int genRadiusNoiseSeed = 0;
    [SerializeField] private float genRadius = 0.35f;
    [SerializeField] private float genMinRadius = 0.05f;
    [SerializeField] private float genHeight = 4f;
    [SerializeField] private float genHorzNoiseScale = 10.0f;
    [SerializeField] private float genVertNoiseScale = 10.0f;
    [SerializeField] private float genHorzNoiseStrength = 0.04f;
    [SerializeField] private float genVertNoiseStrength = 0.04f;
    [SerializeField] private float splitWidthRequirement = 0.25f;

    private TreeGrid grid;
    private Mesh mesh;

    private void Awake()
    {
        GenerateNewTree();
        rb.isKinematic = true;
        CanChop = true;
    }

    public void InitFromGrid(TreeGrid grid)
    {
        this.grid = grid;
        RebuildMesh();
        rb.isKinematic = false;
        CanChop = false;
    }

    [ContextMenu("Generate New Tree")]
    private void GenerateNewTree()
    {
        grid = TreeGrid.FromDensity(genColDensity, genRowDensity, genRadius, genHeight, genMinRadius, splitWidthRequirement);
        grid.GenerateBaseRadius(genRadius, genRadiusNoiseScale, genRadiusNoiseStrength, genRadiusNoiseSeed);
        grid.GeneratePerVertexOffsets(genHorzNoiseScale, genHorzNoiseStrength, genVertNoiseScale, genVertNoiseStrength);

        RebuildMesh();
    }

    private void SpawnTop(TreeGrid top)
    {
        Vector3 pos = transform.position + Vector3.up * (grid.Rows - 1) * (grid.Height / grid.Rows) + Vector3.up * 0.01f;
        var go = Instantiate(gameObject, pos, transform.rotation);
        var tree = go.GetComponent<ChoppableTree>();
        tree.InitFromGrid(top);
    }

    private void RebuildMesh()
    {
        if (mesh == null) mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        else mesh.Clear();

        int trunkVerts = grid.Cols * grid.Rows;
        int trunkTris = grid.Cols * (grid.Rows - 1) * 6;
        int capVerts = grid.Cols + 1;
        int capTris = grid.Cols * 3;
        int totalVerts = trunkVerts + capVerts * 2;
        int totalTris = trunkTris + capTris * 2;

        Vector3[] verts = new Vector3[totalVerts];
        Vector3[] norms = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];
        int[] tris = new int[totalTris];

        int v = 0;
        int t = 0;

        (Vector3 point, Vector3 normal) SamplePoint(int c, int r)
        {
            float angle = (c / (float)grid.Cols) * Mathf.PI * 2f;
            float radius = grid.Radius[c, r];
            float y = (r / (float)(grid.Rows - 1)) * grid.Height;

            Vector3 p = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
            Vector3 n = new Vector3(p.x, 0f, p.z).normalized;

            var off = grid.Offsets[c, r];
            Vector3 tangent = new(-n.z, 0f, n.x);

            p += tangent * off.x;
            p += Vector3.up * off.y;

            return (p, n);
        }

        Vector2 UV(int c, int r) => new(1.0f - grid.Radius[c, r] / grid.BaseRadius[c, r], 0);

        // ---------------------------- Trunk ----------------------------

        for (int r = 0; r < grid.Rows; r++)
        {
            for (int c = 0; c < grid.Cols; c++)
            {
                var (p, n) = SamplePoint(c, r);
                verts[v] = p;
                norms[v] = n;
                uvs[v] = UV(c, r);
                v++;
            }
        }

        for (int r = 0; r < grid.Rows - 1; r++)
        {
            for (int c = 0; c < grid.Cols; c++)
            {
                int cn = (c + 1) % grid.Cols;

                int A = c + r * grid.Cols;
                int B = cn + r * grid.Cols;
                int C = c + (r + 1) * grid.Cols;
                int D = cn + (r + 1) * grid.Cols;

                tris[t++] = A;
                tris[t++] = C;
                tris[t++] = B;

                tris[t++] = B;
                tris[t++] = C;
                tris[t++] = D;
            }
        }

        // ---------------------------- Bottom Cap ----------------------------

        int bottomCenter = v;
        verts[v] = new Vector3(0f, 0f, 0f);
        norms[v] = Vector3.down;
        uvs[v] = new Vector2(0.5f, 0f);
        v++;

        for (int c = 0; c < grid.Cols; c++)
        {
            var (p, _) = SamplePoint(c, 0);
            verts[v] = p;
            norms[v] = Vector3.down;
            uvs[v] = UV(c, 0);
            v++;
        }

        for (int c = 0; c < grid.Cols; c++)
        {
            int A = bottomCenter;
            int B = bottomCenter + 1 + c;
            int C = bottomCenter + 1 + ((c + 1) % grid.Cols);

            tris[t++] = A;
            tris[t++] = B;
            tris[t++] = C;
        }

        // ---------------------------- Top Cap ----------------------------

        int topCenter = v;
        verts[v] = new Vector3(0f, grid.Height, 0f);
        norms[v] = Vector3.up;
        uvs[v] = new Vector2(0.5f, 1f);
        v++;

        for (int c = 0; c < grid.Cols; c++)
        {
            var (p, _) = SamplePoint(c, grid.Rows - 1);
            verts[v] = p;
            norms[v] = Vector3.up;
            uvs[v] = UV(c, grid.Rows - 1);
            v++;
        }

        for (int c = 0; c < grid.Cols; c++)
        {
            int A = topCenter;
            int B = topCenter + 1 + ((c + 1) % grid.Cols);
            int C = topCenter + 1 + c;

            tris[t++] = A;
            tris[t++] = B;
            tris[t++] = C;
        }

        // ---------------------------- Build ----------------------------

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;

        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }

    private Vector2Int WorldToGrid(Vector3 posWorld)
    {
        Vector3 local = transform.InverseTransformPoint(posWorld);

        float angle = Mathf.Atan2(local.z, local.x);
        if (angle < 0) angle += Mathf.PI * 2f;

        int col = Mathf.FloorToInt(angle / (Mathf.PI * 2f) * grid.Cols);
        int row = Mathf.Clamp(Mathf.RoundToInt((local.y / grid.Height) * (grid.Rows - 1)), 0, grid.Rows - 1);

        return new Vector2Int(col, row);
    }
}

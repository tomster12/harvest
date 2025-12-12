using UnityEngine;

public struct TreeGridGenConfig
{
    public float Radius;
    public float Height;
    public float ColDensity;
    public float RowDensity;
    public float MinRadius;
    public float SplitWidthRequirement;
    public float RadiusNoiseScale;
    public float RadiusNoiseStrength;
    public int RadiusNoiseSeed;
    public float HorzNoiseScale;
    public float HorzNoiseStrength;
    public float VertNoiseScale;
    public float VertNoiseStrength;
}

public sealed class TreeGridConstants
{
    public readonly float ColDensity;
    public readonly float RowDensity;
    public readonly float MinRadius;
    public readonly float SplitWidthRequirement;
    public readonly int SplitColRequirement;
    public readonly float HeightPerRow;

    public TreeGridConstants(float colDensity, float rowDensity, float minRadius, float splitWidthRequired, float heightPerRow)
    {
        ColDensity = colDensity;
        RowDensity = rowDensity;
        MinRadius = minRadius;
        SplitWidthRequirement = splitWidthRequired;
        SplitColRequirement = Mathf.CeilToInt(splitWidthRequired * colDensity);
        HeightPerRow = heightPerRow;
    }
}

public sealed class TreeGrid
{
    public readonly TreeGridConstants Constants;
    public readonly int Cols;
    public readonly int Rows;
    public readonly float[,] Radius;
    public readonly float[,] BaseRadius;
    public readonly Vector3[,] Offsets;

    public float Height => Rows * Constants.HeightPerRow;

    public static TreeGrid Generate(TreeGridGenConfig config)
    {
        int cols = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * config.Radius * config.ColDensity));
        int rows = Mathf.Max(4, Mathf.CeilToInt(config.Height * config.RowDensity));
        float heightPerRow = config.Height / rows;

        var constants = new TreeGridConstants(
            config.ColDensity,
            config.RowDensity,
            config.MinRadius,
            config.SplitWidthRequirement,
            heightPerRow
        );

        var grid = new TreeGrid(cols, rows, constants);

        grid.GenerateBaseRadius(
            config.Radius,
            config.RadiusNoiseScale,
            config.RadiusNoiseStrength,
            config.RadiusNoiseSeed
        );

        grid.GeneratePerVertexOffsets(
            config.HorzNoiseScale,
            config.HorzNoiseStrength,
            config.VertNoiseScale,
            config.VertNoiseStrength
        );

        return grid;
    }

    public TreeGrid(int cols, int rows, TreeGridConstants constants)
    {
        Constants = constants;
        Cols = cols;
        Rows = rows;
        Radius = new float[cols, rows];
        BaseRadius = new float[cols, rows];
        Offsets = new Vector3[cols, rows];
    }

    private void GenerateBaseRadius(float baseRadius, float noiseScale, float noiseStrength, int seed)
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

                float radius = baseRadius + radiusNoise;
                BaseRadius[c, r] = radius;
                Radius[c, r] = radius;
            }
        }
    }

    private void GeneratePerVertexOffsets(float horzScale, float horzStrength, float vertScale, float vertStrength)
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
        int hitCols = Mathf.CeilToInt(width * Constants.ColDensity);
        int hitRows = Mathf.CeilToInt(height * Constants.RowDensity);

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
                Radius[c, r] = Mathf.Max(Constants.MinRadius, Radius[c, r] - depth * falloff);
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
            if (Radius[c % Cols, r] <= Constants.MinRadius + eps) runCount++;
            else runCount = 0;
            if (runCount >= Constants.SplitColRequirement) return true;
        }
        return false;
    }

    public (TreeGrid bottom, TreeGrid top) SplitAtRow(int splitRow)
    {
        int bottomRows = splitRow + 1;
        int topRows = Rows - bottomRows;
        if (topRows <= 0) return (this, null);

        var bottom = Slice(0, bottomRows);
        var top = Slice(bottomRows, topRows);
        return (bottom, top);
    }

    private TreeGrid Slice(int startRow, int rows)
    {
        var grid = new TreeGrid(Cols, rows, Constants);

        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                int sourceRow = startRow + r;
                grid.Radius[c, r] = Radius[c, sourceRow];
                grid.BaseRadius[c, r] = BaseRadius[c, sourceRow];
                grid.Offsets[c, r] = Offsets[c, sourceRow];
            }
        }

        return grid;
    }

    public Vector3 GridToLocal(int c, int r)
    {
        float angle = (c / (float)Cols) * Mathf.PI * 2f;
        float radius = Radius[c, r];
        float y = (r / (float)(Rows - 1)) * Height;

        Vector3 p = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);

        var off = Offsets[c, r];
        Vector3 n = new Vector3(p.x, 0f, p.z).normalized;
        Vector3 tangent = new(-n.z, 0f, n.x);

        p += tangent * off.x;
        p += Vector3.up * off.y;

        return p;
    }

    public Vector2Int LocalToGrid(Vector3 localPos)
    {
        float angle = Mathf.Atan2(localPos.z, localPos.x);
        if (angle < 0) angle += Mathf.PI * 2f;

        int col = Mathf.FloorToInt(angle / (Mathf.PI * 2f) * Cols);
        int row = Mathf.Clamp(Mathf.RoundToInt((localPos.y / Height) * (Rows - 1)), 0, Rows - 1);

        return new Vector2Int(col, row);
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

    [Header("Generation Config")]
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
        RegenerateTree();
        rb.isKinematic = true;
        CanChop = true;
    }

    [ContextMenu("Regenerate Tree")]
    private void RegenerateTree()
    {
        grid = TreeGrid.Generate(new()
        {
            Radius = genRadius,
            Height = genHeight,
            ColDensity = genColDensity,
            RowDensity = genRowDensity,
            MinRadius = genMinRadius,
            SplitWidthRequirement = splitWidthRequirement,
            RadiusNoiseScale = genRadiusNoiseScale,
            RadiusNoiseStrength = genRadiusNoiseStrength,
            RadiusNoiseSeed = genRadiusNoiseSeed,
            HorzNoiseScale = genHorzNoiseScale,
            HorzNoiseStrength = genHorzNoiseStrength,
            VertNoiseScale = genVertNoiseScale,
            VertNoiseStrength = genVertNoiseStrength
        });

        RebuildMesh();
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
            Vector3 p = grid.GridToLocal(c, r);
            Vector3 n = new Vector3(p.x, 0f, p.z).normalized;
            Vector3 tangent = new(-n.z, 0f, n.x);

            p += tangent * grid.Offsets[c, r].x;
            p += Vector3.up * grid.Offsets[c, r].y;

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

    private void SpawnTop(TreeGrid topGrid)
    {
        Vector3 topPos = transform.position + Vector3.up * (grid.Rows - 1) * (grid.Height / grid.Rows) + Vector3.up * 0.01f;
        var go = Instantiate(gameObject, topPos, transform.rotation);
        var tree = go.GetComponent<ChoppableTree>();

        tree.grid = topGrid;
        tree.RebuildMesh();
        tree.rb.isKinematic = false;
        tree.CanChop = false;
    }

    private Vector2Int WorldToGrid(Vector3 posWorld)
    {
        Vector3 local = transform.InverseTransformPoint(posWorld);
        return grid.LocalToGrid(local);
    }
}

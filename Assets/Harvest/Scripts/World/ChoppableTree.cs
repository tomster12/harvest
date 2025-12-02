using UnityEngine;

public class ChoppableTree : MonoBehaviour
{
    public void Hit(Vector3 hitWorld, float depth, float width, float height)
    {
        Vector3 local = transform.InverseTransformPoint(hitWorld);
        Vector2Int c = LocalToGrid(local);

        int radT = Mathf.CeilToInt(width);
        int radH = Mathf.CeilToInt(height);

        for (int dTi = -radT; dTi <= radT; dTi++)
        {
            int ti = (c.x + dTi + gridThetaCount) % gridThetaCount;

            for (int dHi = -radH; dHi <= radH; dHi++)
            {
                int hi = c.y + dHi;
                if (hi < 0 || hi >= gridHeightCount) continue;

                float ndt = Mathf.Abs(dTi) / (float)radT;
                float ndh = Mathf.Abs(dHi) / (float)radH;
                float dist = Mathf.Sqrt(ndt * ndt + ndh * ndh);
                if (dist > 1f) continue;

                float fall = 1f - dist;

                currentRadiusGrid[ti, hi] = Mathf.Max(0f, currentRadiusGrid[ti, hi] - depth * fall);
            }
        }

        RegenerateMesh();
    }

    [Header("References")]
    [SerializeField] private MeshFilter mf;
    [SerializeField] private MeshCollider mc;

    [Header("Grid Config")]
    [SerializeField] private int gridThetaCount = 128;
    [SerializeField] private int gridHeightCount = 256;
    [SerializeField] private float radiusNoiseScale = 1f;
    [SerializeField] private float radiusNoiseStrength = 0.25f;
    [SerializeField] private int radiusNoiseSeed = 0;

    [Header("Geometry")]
    [SerializeField] private float baseRadius = 0.35f;
    [SerializeField] private float meshHeight = 8f;
    [SerializeField] private float meshHorizontalNoiseScale = 10.0f;
    [SerializeField] private float meshVerticalNoiseScale = 10.0f;
    [SerializeField] private float meshHorizontalNoiseStrength = 0.04f;
    [SerializeField] private float meshVerticalNoiseStrength = 0.04f;

    private float[,] currentRadiusGrid;
    private float[,] baseRadiusGrid;
    private Mesh mesh;

    private void Awake()
    {
        GenerateBaseGrid();
        RegenerateMesh();
    }

    [ContextMenu("Regenerate Tree")]
    public void RegenerateTree()
    {
        GenerateBaseGrid();
        RegenerateMesh();
    }

    private void GenerateBaseGrid()
    {
        baseRadiusGrid = new float[gridThetaCount, gridHeightCount];
        currentRadiusGrid = new float[gridThetaCount, gridHeightCount];

        for (int ti = 0; ti < gridThetaCount; ti++)
        {
            for (int hi = 0; hi < gridHeightCount; hi++)
            {
                float theta = (ti / (float)gridThetaCount) * Mathf.PI * 2f;
                float nx = Mathf.Cos(theta);
                float nz = Mathf.Sin(theta) + (float)hi / (gridHeightCount - 1);
                float n = Mathf.PerlinNoise(nx * radiusNoiseScale + radiusNoiseSeed, nz * radiusNoiseScale + radiusNoiseSeed);

                float noiseOffset = (n - 0.5f) * radiusNoiseStrength;
                baseRadiusGrid[ti, hi] = baseRadius + noiseOffset;
                currentRadiusGrid[ti, hi] = baseRadiusGrid[ti, hi];
            }
        }
    }

    private void RegenerateMesh()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        else mesh.Clear();

        int vCount = gridThetaCount * gridHeightCount;
        int tCount = (gridThetaCount * (gridHeightCount - 1)) * 6;
        Vector3[] vertices = new Vector3[vCount];
        Vector3[] normals = new Vector3[vCount];
        Vector2[] uvs = new Vector2[vCount];
        int[] tris = new int[tCount];

        // Build vertices and normals
        int vi = 0;
        for (int hi = 0; hi < gridHeightCount; hi++)
        {
            for (int ti = 0; ti < gridThetaCount; ti++)
            {
                Vector3 vertex = GridToLocal(ti, hi);
                Vector3 normal = new Vector3(vertex.x, 0f, vertex.z).normalized;
                Vector3 tangent = new Vector3(-normal.z, 0f, normal.x);

                float theta = (ti / (float)gridThetaCount) * Mathf.PI * 2f;
                float nx = Mathf.Cos(theta);
                float nz = Mathf.Sin(theta) + (float)hi / (gridHeightCount - 1);
                float perpNoise = Mathf.PerlinNoise(nx * meshHorizontalNoiseScale + 100f, nz * meshHorizontalNoiseScale + 200f);
                float vertNoise = Mathf.PerlinNoise(nx * meshVerticalNoiseScale + 300f, nz * meshVerticalNoiseScale + 400f);
                Vector3 perpOffset = tangent * ((perpNoise - 0.5f) * meshHorizontalNoiseStrength);
                float vertOffset = (vertNoise - 0.5f) * meshVerticalNoiseStrength;

                vertex += perpOffset;
                vertex.y += vertOffset;
                vertices[vi] = vertex;
                normals[vi] = normal;

                // 1 = full bark, 0 = cut
                float barkPct = 1f - Mathf.Clamp01(Mathf.Abs(baseRadiusGrid[ti, hi] - currentRadiusGrid[ti, hi]) / 0.1f);
                uvs[vi] = new Vector2(barkPct, 0.0f);

                vi++;
            }
        }

        // Build triangles
        int tiOut = 0;
        for (int hi = 0; hi < gridHeightCount - 1; hi++)
        {
            for (int ti = 0; ti < gridThetaCount; ti++)
            {
                int tiNext = (ti + 1) % gridThetaCount;

                int A = ti + hi * gridThetaCount;
                int B = tiNext + hi * gridThetaCount;
                int C = ti + (hi + 1) * gridThetaCount;
                int D = tiNext + (hi + 1) * gridThetaCount;

                tris[tiOut++] = A;
                tris[tiOut++] = C;
                tris[tiOut++] = B;

                tris[tiOut++] = B;
                tris[tiOut++] = C;
                tris[tiOut++] = D;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }

    private Vector2Int LocalToGrid(Vector3 pLocal)
    {
        float angle = Mathf.Atan2(pLocal.z, pLocal.x);
        if (angle < 0) angle += Mathf.PI * 2f;

        int ti = Mathf.FloorToInt((angle / (2f * Mathf.PI)) * gridThetaCount);

        float hNorm = Mathf.Clamp01(pLocal.y / meshHeight);
        int hi = Mathf.FloorToInt(hNorm * (gridHeightCount - 1));

        return new Vector2Int(ti, hi);
    }

    private Vector3 GridToLocal(int ti, int hi)
    {
        float angle = (ti / (float)gridThetaCount) * Mathf.PI * 2f;
        float y = (hi / (float)(gridHeightCount - 1)) * meshHeight;
        float r = currentRadiusGrid[ti, hi];

        return new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
    }
}

using UnityEngine;

public class ChoppableTree : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshFilter mf;

    [Header("Grid Config")]
    [SerializeField] private int gridThetaCount = 128;
    [SerializeField] private int gridHeightCount = 256;

    [Header("Geometry")]
    [SerializeField] private float baseRadius = 0.35f;
    [SerializeField] private float meshHeight = 8f;

    private float[,] radiusGrid;

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
        radiusGrid = new float[gridThetaCount, gridHeightCount];

        for (int ti = 0; ti < gridThetaCount; ti++)
        {
            for (int hi = 0; hi < gridHeightCount; hi++)
            {
                radiusGrid[ti, hi] = baseRadius;
            }
        }
    }

    private void RegenerateMesh()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mf.sharedMesh = mesh;
        }
        else
        {
            mesh.Clear();
        }

        int vCount = gridThetaCount * gridHeightCount;
        int tCount = (gridThetaCount * (gridHeightCount - 1)) * 6;
        Vector3[] vertices = new Vector3[vCount];
        Vector3[] normals = new Vector3[vCount];
        int[] tris = new int[tCount];

        // Build vertices
        int vi = 0;
        for (int hi = 0; hi < gridHeightCount; hi++)
        {
            for (int ti = 0; ti < gridThetaCount; ti++)
            {
                Vector3 p = GridToLocal(ti, hi);
                vertices[vi] = p;

                // For cylindrical normals
                Vector3 n = new Vector3(p.x, 0, p.z).normalized;
                normals[vi] = n;

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
        mesh.RecalculateBounds();
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
        float r = radiusGrid[ti, hi];

        return new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
    }

    public void Hit(Vector3 hitWorld, float depth, float width, float height)
    {
        Vector3 local = transform.InverseTransformPoint(hitWorld);
        Vector2Int c = LocalToGrid(local);

        int radT = Mathf.CeilToInt(width);
        int radH = Mathf.CeilToInt(height);

        for (int dti = -radT; dti <= radT; dti++)
        {
            int ti = (c.x + dti + gridThetaCount) % gridThetaCount;

            for (int dhi = -radH; dhi <= radH; dhi++)
            {
                int hi = c.y + dhi;
                if (hi < 0 || hi >= gridHeightCount) continue;

                float ndt = Mathf.Abs(dti) / (float)radT;
                float ndh = Mathf.Abs(dhi) / (float)radH;
                float dist = Mathf.Sqrt(ndt * ndt + ndh * ndh);
                if (dist > 1f) continue;

                float fall = 1f - dist;

                radiusGrid[ti, hi] = Mathf.Max(
                    0f,
                    radiusGrid[ti, hi] - depth * fall
                );
            }
        }

        RegenerateMesh();
    }
}

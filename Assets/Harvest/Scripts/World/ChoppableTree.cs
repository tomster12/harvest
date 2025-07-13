using System.Collections.Generic;
using UnityEngine;

public class ChopPoint
{
    public Vector3 point;
    public Vector3 normal;
    public ChopRing chopRing = null;
    public int pointIndex = 0;
}

public class ChopRing
{
    public Vector3 center;
    public float centerAngle;
    public float[] pointDepths;
}

public class ChoppableTree : MonoBehaviour
{
    public void Hit(ChopPoint chopPoint, float strength, float accuracy)
    {
        // If we have no chop ring then create a new one
        if (chopPoint.chopRing == null)
        {
            Debug.Log($"Creating new chop ring at {chopPoint.point} with normal {chopPoint.normal}");
            chopPoint.chopRing = new ChopRing
            {
                center = chopPoint.point,
                centerAngle = Mathf.Atan2(chopPoint.point.z, chopPoint.point.x),
                pointDepths = new float[CHOP_RING_POINT_COUNT]
            };
            for (int i = 0; i < CHOP_RING_POINT_COUNT; i++)
            {
                chopPoint.chopRing.pointDepths[i] = 0.5f;
            }
            chopRings.Add(chopPoint.chopRing);

            // Create the topology for the new ring
            niceMesh.MakeRingCut(chopPoint.point.y + 0.2f);
            niceMesh.MakeRingCut(chopPoint.point.y - 0.2f);

            // TODO: Remove vertices between ring cuts
        }

        // Now add to the existing ring
        Debug.Log($"Adding chop point at {chopPoint.point} with normal {chopPoint.normal} to ring at {chopPoint.chopRing.center}");
        float depth = chopPoint.chopRing.pointDepths[chopPoint.pointIndex];
        depth += strength * accuracy;
        Debug.Log($"Chop point depth reduced to {depth} at index {chopPoint.pointIndex}");
        chopPoint.chopRing.pointDepths[chopPoint.pointIndex] = depth;

        // And update the vertices with the depth
    }

    public ChopPoint GetChopPoint(RaycastHit hit)
    {
        // If we are within the y value threshold of a chop ring, return the chop point
        foreach (var ring in chopRings)
        {
            if (Mathf.Abs(hit.point.y - ring.center.y) < CHOP_RING_MIN_DIST)
            {
                // Find the closest point on the ring
                float posAngle = Mathf.Atan2(hit.point.z - ring.center.z, hit.point.x - ring.center.x);
                float pointDelta = 2 * Mathf.PI / CHOP_RING_POINT_COUNT;
                int pointIndex = Mathf.RoundToInt((posAngle - ring.centerAngle) / pointDelta) % CHOP_RING_POINT_COUNT;

                // Convert the point back to a world position
                Vector3 chopPointPos = new(
                    ring.center.x + Mathf.Cos(posAngle) * ring.pointDepths[pointIndex],
                    ring.center.y + 0f,
                    ring.center.z + Mathf.Sin(posAngle) * ring.pointDepths[pointIndex]);

                // Return a chop at the closest point on this chop ring
                return new ChopPoint { point = chopPointPos, normal = hit.point - ring.center, chopRing = ring, pointIndex = pointIndex };
            }
        }

        // If no chop ring found, return a new chop point at the hit position
        return new ChopPoint { point = hit.point, normal = hit.normal, chopRing = null, pointIndex = 0 };
    }

    private static readonly float CHOP_RING_MIN_DIST = 0.4f;
    private static readonly int CHOP_RING_POINT_COUNT = 8;

    [Header("References")]
    [SerializeField] private MeshFilter mf;

    [Header("Config")]
    [SerializeField] private float meshHeight = 5.0f;
    [SerializeField] private float meshRadius = 0.2f;
    [SerializeField] private int meshRingCount = 10;
    [SerializeField] private int meshRingVerticesCount = 16;

    private DeformableMesh niceMesh;
    private List<ChopRing> chopRings = new();

    [ContextMenu("Generate Mesh")]
    private void GenerateMesh()
    {
        // Generate a simple cylinder mesh for the tree
        List<Vector3> vertexList = new();
        List<int> triangleList = new();

        // Vertices and triangles for each ring
        for (int i = 0; i < meshRingCount; i++)
        {
            for (int j = 0; j < meshRingVerticesCount; j++)
            {
                float angle = j * (360.0f / meshRingVerticesCount) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * meshRadius;
                float y = i * meshHeight / (meshRingCount - 1);
                float z = Mathf.Sin(angle) * meshRadius;
                x += Random.Range(-0.05f, 0.05f);
                if (i != 0 && i != meshRingCount - 1) y += Random.Range(-0.05f, 0.05f);
                z += Random.Range(-0.05f, 0.05f);
                vertexList.Add(new(x, y, z));
            }
        }
        for (int i = 0; i < meshRingCount - 1; i++)
        {
            for (int j = 0; j < meshRingVerticesCount; j++)
            {
                int v0 = i * meshRingVerticesCount + j;
                int v1 = i * meshRingVerticesCount + (j + 1) % meshRingVerticesCount;
                int v2 = v0 + meshRingVerticesCount;
                int v3 = v1 + meshRingVerticesCount;
                triangleList.Add(v0);
                triangleList.Add(v2);
                triangleList.Add(v1);
                triangleList.Add(v1);
                triangleList.Add(v2);
                triangleList.Add(v3);
            }
        }

        // Vertex and triangles for top ring + top vertex
        int topStart = vertexList.Count;
        vertexList.Add(new(0, meshHeight, 0));
        for (int j = 0; j < meshRingVerticesCount; j++)
        {
            vertexList.Add(vertexList[topStart - meshRingVerticesCount + j]);
        }
        for (int j = 0; j < meshRingVerticesCount; j++)
        {
            int v0 = topStart + 1 + j;
            int v1 = topStart + 1 + (j + 1) % meshRingVerticesCount;
            int v2 = topStart;
            triangleList.Add(v0);
            triangleList.Add(v2);
            triangleList.Add(v1);
        }

        niceMesh = new DeformableMesh(mf, vertexList, triangleList);
    }

    [ContextMenu("Make Cut")]
    private void DebugMakeCut()
    {
        niceMesh.MakeRingCut(meshHeight / 2.0f);
    }
}

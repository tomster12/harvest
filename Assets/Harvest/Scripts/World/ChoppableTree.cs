using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.UIElements;
using static DeformableMesh;
using static UnityEngine.EventSystems.EventTrigger;

public class ChopTarget
{
    public Vector3 pos;
    public Vector3 normal;
    public ChopRing ring = null;
    public int ringPoint = 0;
}

public class ChopRingPointVertex
{
    public int topVertex;
    public int bottomVertex;
    public Vector3 basePos;
    public float angle;
}

public class ChopRingPoint
{
    public int index;
    public Vector3 basePos;
    public float baseDistance = 0f;
    public float angle = 0f;
    public float depth = 0f;
    public float height = 0f;
    public bool isMiddleVertexGenerated = false;
    public List<ChopRingPointVertex> movableVertices;
    public List<ChopRingPointVertex> bottomMeshVertices;
    public int middleMeshVertex;
}

public class ChopRing
{
    public Vector3 centre;
    public float radius;
    public Vector3 origin;
    public RingCut cut;
    public ChopRingPoint[] points;
}

public class ChoppableTree : MonoBehaviour
{
    public ChopTarget GetChopTarget(RaycastHit hit)
    {
        // If within a threshold of an existing ring lock onto it
        foreach (var ring in chopRings)
        {
            if (Mathf.Abs(hit.point.y - ring.origin.y) < CHOP_RING_MIN_DIST)
            {
                int pointIndex = GetChopRingPointIndex(ring, hit.point);
                Vector3 pos = GetChopRingPointPos(ring, pointIndex);
                Vector3 normal = new(pos.x - ring.centre.x, 0, pos.z - ring.centre.z);
                return new ChopTarget { pos = pos, normal = normal.normalized, ring = ring, ringPoint = pointIndex };
            }
        }

        // Otherwise return a free floating chop point at the hit position
        return new ChopTarget { pos = hit.point, normal = hit.normal, ring = null, ringPoint = 0 };
    }

    public void Hit(ChopTarget target, float depth, float width, float height)
    {
        target.ring ??= CreateChopRing(target.pos);
        ApplyChop(target.ring, target.ringPoint, depth, width, height);
        UpdateChopTarget(target);
    }

    private static readonly float CHOP_RING_MIN_DIST = 0.8f;
    private static readonly int CHOP_RING_POINT_COUNT = 16;
    private static readonly float CHOP_RING_POINT_DELTA = 2 * Mathf.PI / CHOP_RING_POINT_COUNT;

    [Header("References")]
    [SerializeField] private MeshFilter mf;

    [Header("Config")]
    [SerializeField] private float meshHeight = 5.0f;
    [SerializeField] private float meshRadius = 0.2f;
    [SerializeField] private int meshRingCount = 10;
    [SerializeField] private int meshRingVerticesCount = 16;

    private DeformableMesh deformMesh;

    private List<ChopRing> chopRings = new();

    [ContextMenu("Generate Mesh")]
    private void GenerateMesh()
    {
        chopRings.Clear();

        // Generate a simple cylinder mesh for the tree
        List<Vector3> vertexPositions = new();
        List<Vector3> vertexList = new();
        List<int> triangleList = new();

        void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            vertexList.Add(a);
            vertexList.Add(b);
            vertexList.Add(c);
            triangleList.Add(vertexList.Count - 3);
            triangleList.Add(vertexList.Count - 2);
            triangleList.Add(vertexList.Count - 1);
        }

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
                vertexPositions.Add(new(x, y, z));
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
                AddTriangle(vertexPositions[v0], vertexPositions[v2], vertexPositions[v1]);
                AddTriangle(vertexPositions[v1], vertexPositions[v2], vertexPositions[v3]);
            }
        }

        // Vertex and triangles for top ring + top vertex
        int topStart = vertexPositions.Count;
        vertexPositions.Add(new(0, meshHeight, 0));
        for (int j = 0; j < meshRingVerticesCount; j++)
        {
            vertexPositions.Add(vertexPositions[topStart - meshRingVerticesCount + j]);
        }
        for (int j = 0; j < meshRingVerticesCount; j++)
        {
            int v0 = topStart + 1 + j;
            int v1 = topStart + 1 + (j + 1) % meshRingVerticesCount;
            int v2 = topStart;
            AddTriangle(vertexPositions[v0], vertexPositions[v2], vertexPositions[v1]);
        }

        deformMesh = new DeformableMesh(mf, vertexList, triangleList);
    }

    [ContextMenu("Chop")]
    private void DebugChop()
    {
        ChopTarget chopPoint = new()
        {
            pos = new Vector3(transform.position.x - meshRadius, transform.position.y + meshHeight * 0.5f, transform.position.z),
            normal = Vector3.zero,
            ring = null,
            ringPoint = 0
        };

        Hit(chopPoint, 0.1f, 0.3f, 0.03f);
    }

    private ChopRing CreateChopRing(Vector3 origin)
    {
        // Perform a mesh ring cut at the chop rings height
        RingCut cut = deformMesh.CreateRingCut(origin.y);

        // Process the mesh cut vertices into a chop ring
        Vector3 centre = cut.cutVertices.Select(v => deformMesh.Vertices[v])
            .Aggregate(Vector3.zero, (acc, v) => acc + v) / cut.cutVertices.Count;

        float radius = cut.cutVertices
            .Select(v => Vector3.Distance(deformMesh.Vertices[v], centre))
            .Aggregate(0f, (acc, d) => acc + d) / cut.cutVertices.Count;

        ChopRing ring = new()
        {
            centre = centre,
            radius = radius,
            origin = origin,
            cut = cut,
            points = new ChopRingPoint[CHOP_RING_POINT_COUNT],
        };

        // Prepare the ring cut vertices for the points
        var cutVertexInfos = cut.cutVertices
            .Select(v => new
            {
                vertexTop = v,
                vertexBottom = deformMesh.DuplicateVertex(v, (int t) =>
                    cut.aboveVertices.Contains(deformMesh.Triangles[t * 3 + 0]) ||
                    cut.aboveVertices.Contains(deformMesh.Triangles[t * 3 + 1]) ||
                    cut.aboveVertices.Contains(deformMesh.Triangles[t * 3 + 2])),
                angle = VectorUtil.GetPosAngle(deformMesh.Vertices[v] - centre)
            })
            .OrderBy(v => v.angle)
            .ToArray();

        // Produce chop points at equal angles around the chop ring
        for (int i = 0; i < CHOP_RING_POINT_COUNT; i++)
        {
            float angle = i * CHOP_RING_POINT_DELTA;

            // Find a position along the ring cut for the chop point
            int aIndex = Enumerable.Range(0, cutVertexInfos.Length + 1)
                .FirstOrDefault(j => VectorUtil.IsAngleBetween(angle, cutVertexInfos[j].angle, cutVertexInfos[(j + 1) % cutVertexInfos.Length].angle));
            int bIndex = (aIndex + 1) % cutVertexInfos.Length;
            var a = cutVertexInfos[aIndex];
            var b = cutVertexInfos[bIndex];

            Debug.Assert(VectorUtil.RaySegmentIntersection(
                new(deformMesh.Vertices[a.vertexTop].x, deformMesh.Vertices[a.vertexTop].z),
                new(deformMesh.Vertices[b.vertexTop].x, deformMesh.Vertices[b.vertexTop].z),
                new(centre.x, centre.z), angle, out Vector2 splitPosFlat), "Failed to find chop ring point intersection");
            Vector3 basePos = new(splitPosFlat.x, deformMesh.Vertices[a.vertexTop].y, splitPosFlat.y);

            ring.points[i] = new ChopRingPoint
            {
                index = i,
                basePos = basePos,
                baseDistance = Vector3.Distance(basePos, centre),
                angle = angle,
                depth = 0f,
                height = 0f,
                isMiddleVertexGenerated = false,
                movableVertices = new(),
                middleMeshVertex = -1
            };

            // create extra vertices at the points without moving existing vertices
            int addedTopVertex = deformMesh.SplitEdge(a.vertexTop, b.vertexTop, basePos);
            int adddedBottomVertex = deformMesh.SplitEdge(a.vertexBottom, b.vertexBottom, basePos);
            ring.points[i].movableVertices.Add(new ChopRingPointVertex
            {
                topVertex = addedTopVertex,
                bottomVertex = adddedBottomVertex,
                basePos = deformMesh.Vertices[addedTopVertex],
                angle = angle
            });
        }

        // Now add each cut vertex to the closest chop point
        foreach (var cutVertexInfo in cutVertexInfos)
        {
            // Find the closest chop point to the cut vertex using angles
            int closestPointIndex = Enumerable.Range(0, CHOP_RING_POINT_COUNT)
                .OrderBy(i => VectorUtil.GetAngleDifference(cutVertexInfo.angle, ring.points[i].angle))
                .First();
            ChopRingPoint closestPoint = ring.points[closestPointIndex];

            // Make sure it is within 1 point segment of difference and then add
            float distSq = Vector3.SqrMagnitude(closestPoint.basePos - deformMesh.Vertices[cutVertexInfo.vertexTop]);
            float segmentDst = CHOP_RING_POINT_DELTA * ring.radius;
            if (distSq < segmentDst * segmentDst)
            {
                closestPoint.movableVertices.Add(new ChopRingPointVertex
                {
                    topVertex = cutVertexInfo.vertexTop,
                    bottomVertex = cutVertexInfo.vertexBottom,
                    basePos = deformMesh.Vertices[cutVertexInfo.vertexTop],
                    angle = cutVertexInfo.angle
                });
            }
        }

        // Now we can sort the movable vertices by angle
        foreach (ChopRingPoint point in ring.points)
        {
            point.movableVertices = point.movableVertices.OrderBy(v => v.angle).ToList();
        }

        deformMesh.UpdateMesh();
        chopRings.Add(ring);
        return ring;
    }

    private void ApplyChop(ChopRing ring, int pointIndex, float depth, float width, float height)
    {
        // Figure out how many chop points are affected
        ChopRingPoint targetPoint = ring.points[pointIndex];
        int pointReach = (int)Mathf.Floor(width / (CHOP_RING_POINT_DELTA * ring.radius));
        var affectedPoints = Enumerable.Range(-pointReach, pointReach * 2 + 1)
            .Select(i => ring.points[(pointIndex + i + CHOP_RING_POINT_COUNT) % CHOP_RING_POINT_COUNT])
            .Where(p => VectorUtil.GetAngleDifference(targetPoint.angle, p.angle) < Mathf.PI / 2f)
            .ToList();

        // The chop is flat and perpendicular to the target point
        // Any point within the reach is affected by AND affects the chop
        // We need to find how far away each affected point is from the perpendicular line of the target point
        // Make a triangle around the centre with a known angle and hypotenuse, and calculate adjacent
        float minAlignedDepth = float.MaxValue;
        Dictionary<ChopRingPoint, float> alignedDepths = new();
        foreach (ChopRingPoint point in affectedPoints)
        {
            float centreDist = point.baseDistance - point.depth;
            float alignedCentreDist = Mathf.Cos(point.angle - targetPoint.angle) * centreDist;
            float alignedPointDepth = Mathf.Max(targetPoint.baseDistance - alignedCentreDist, 0f);
            if (alignedPointDepth < minAlignedDepth) minAlignedDepth = alignedPointDepth;
            alignedDepths[point] = alignedPointDepth;
        }

        // Now we know the minimum chop depth we can apply it to all affected points
        foreach (ChopRingPoint point in affectedPoints)
        {
            float newAlignedPointDepth = Mathf.Min(minAlignedDepth + depth, alignedDepths[point] + depth);
            float newAlignedCentreDist = targetPoint.baseDistance - newAlignedPointDepth;
            float newCentreDist = newAlignedCentreDist / Mathf.Cos(point.angle - targetPoint.angle);
            point.depth = point.baseDistance - newCentreDist;
            point.height = Mathf.Max(point.height, height);
        }

        foreach (ChopRingPoint point in affectedPoints)
        {
            UpdateChopPointMesh(ring, point.index);
        }
    }

    private void UpdateChopPointMesh(ChopRing ring, int pointIndex)
    {
        // We need to keep the mesh updated with the rings topology
        // Each chop ring point has the centre point that moves backwards, then a set of "movable vertices"
        // Each movable vertex is a point that is mirrored top and bottom that was setup when the ring was created
        // If the next point along we can just produce triangles between (our furthest right point, nexts furthest left point, our middle vertex)
        // Otherwise we need to produce a mesh of triangles that connect all of the movable vertices and middle vertices of each point

        ChopRingPoint point = ring.points[pointIndex];

        // Create the internal topology if it doesn't exist
        if (!point.isMiddleVertexGenerated)
        {
            point.middleMeshVertex = deformMesh.AddVertex(point.basePos);
            point.isMiddleVertexGenerated = true;
        }

        // Update the topology with the depth and width
        deformMesh.MoveVertex(point.middleMeshVertex, point.basePos + (ring.centre - point.basePos).normalized * point.depth);
        foreach (ChopRingPointVertex mv in point.movableVertices)
        {
            deformMesh.MoveVertex(mv.topVertex, mv.basePos + Vector3.up * point.height);
            deformMesh.MoveVertex(mv.bottomVertex, mv.basePos - Vector3.up * point.height);
        }

        deformMesh.UpdateMesh();
    }

    private void UpdateChopTarget(ChopTarget target)
    {
        target.pos = GetChopRingPointPos(target.ring, target.ringPoint);
        target.normal = (new Vector3(target.pos.x - target.ring.centre.x, 0, target.pos.z - target.ring.centre.z)).normalized;
    }

    private Vector3 GetChopRingPointPos(ChopRing ring, int pointIndex)
    {
        ChopRingPoint point = ring.points[pointIndex];
        if (point.isMiddleVertexGenerated) return deformMesh.Vertices[point.middleMeshVertex];
        else return point.basePos;
    }

    private int GetChopRingPointIndex(ChopRing ring, Vector3 pos)
    {
        float posAngle = VectorUtil.GetPosAngle(pos - ring.centre);
        return Mathf.RoundToInt(posAngle / CHOP_RING_POINT_DELTA) % CHOP_RING_POINT_COUNT;
    }

    private void OnDrawGizmosSelected()
    {
        if (chopRings != null)
        {
            foreach (ChopRing chopRing in chopRings)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.TransformPoint(chopRing.centre), 0.03f);

                foreach (ChopRingPoint point in chopRing.points)
                {
                    float pct = point.angle / (Mathf.PI * 2);
                    Gizmos.color = Color.Lerp(Color.red, Color.green, pct);
                    Vector3 basePos = transform.TransformPoint(point.basePos);
                    Vector3 pointPos = transform.TransformPoint(GetChopRingPointPos(chopRing, point.index));
                    Gizmos.DrawSphere(basePos, 0.02f);
                    Gizmos.DrawSphere(pointPos, 0.01f);
                    Gizmos.DrawLine(basePos, pointPos);
                }
            }
        }
    }
}

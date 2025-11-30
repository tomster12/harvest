using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DeformableMesh;

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
    public bool isDisplaced = false;
    public List<ChopRingPointVertex> movableVertices;
    public List<ChopRingPointVertex> bottomMeshVertices;
    public int middleMeshVertex;
}

public class ChopRing
{
    public Vector3 centre;
    public float radius;
    public float angleOffset;
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
            if (Mathf.Abs(hit.point.y - ring.origin.y) < chopRingMinDist)
            {
                int pointIndex = GetChopRingPointIndex(ring, hit.point);
                Vector3 pos = GetChopRingPointPos(ring, pointIndex);
                Vector3 normal = new(pos.x - ring.centre.x, 0, pos.z - ring.centre.z);
                return new ChopTarget { pos = transform.TransformPoint(pos), normal = normal.normalized, ring = ring, ringPoint = pointIndex };
            }
        }

        // Otherwise return a free floating chop point at the hit position
        return new ChopTarget { pos = hit.point, normal = hit.normal, ring = null, ringPoint = -1 };
    }

    public void Hit(ChopTarget target, float depth, float width, float height)
    {
        if (target.ring == null)
        {
            target.ring = CreateChopRing(target.pos);
            target.ringPoint = 0;
        }
        ApplyChop(target.ring, target.ringPoint, depth, width, height);
        UpdateChopTarget(target);
    }

    [Header("References")]
    [SerializeField] private MeshFilter mf;
    [SerializeField] private MeshCollider mc;

    [Header("Config")]
    [SerializeField] private float meshHeight = 5.0f;
    [SerializeField] private float meshRadius = 0.2f;
    [SerializeField] private int meshRingCount = 10;
    [SerializeField] private int meshRingVerticesCount = 16;
    [SerializeField] private float chopRingMinDist = 0.1f;
    [SerializeField] private int chopRingPointCount = 16;
    [SerializeField] private float dbgChopDepth = 0.1f;
    [SerializeField] private float dbgChopWidth = 0.3f;
    [SerializeField] private float dbgChopHeight = 0.1f;

    private DeformableMesh deformMesh;
    private List<ChopRing> chopRings = new();

    private float ChopRingPointAngleDelta => 2 * Mathf.PI / chopRingPointCount;

    private void Awake()
    {
        GenerateMesh();
    }

    [ContextMenu("Generate Mesh")]
    private void GenerateMesh()
    {
        chopRings.Clear();

        // Generate a simple cylinder mesh for the tree
        List<Vector3> vertexList = new();
        List<int> triangleList = new();

        void AddTriangle(int a, int b, int c)
        {
            triangleList.Add(a);
            triangleList.Add(b);
            triangleList.Add(c);
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
                x += Random.Range(-0.012f, 0.012f);
                if (i != 0 && i != meshRingCount - 1) y += Random.Range(-0.012f, 0.012f);
                z += Random.Range(-0.012f, 0.012f);
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
                AddTriangle(v0, v2, v1);
                AddTriangle(v1, v2, v3);
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
            AddTriangle(v0, v2, v1);
        }

        deformMesh = new DeformableMesh(mf, vertexList, triangleList);
        mc.sharedMesh = deformMesh.Mesh;
    }

    [ContextMenu("Chop")]
    private void DebugChop()
    {
        GenerateMesh();

        ChopTarget chopPoint = new()
        {
            pos = new Vector3(transform.position.x - meshRadius, transform.position.y + meshHeight * 0.5f, transform.position.z),
            normal = Vector3.zero,
            ring = null,
            ringPoint = -1
        };

        Hit(chopPoint, dbgChopDepth, dbgChopWidth, dbgChopHeight);
    }

    private ChopRing CreateChopRing(Vector3 origin)
    {
        // Perform a mesh ring cut at the chop rings height
        RingCut cut = deformMesh.CreateRingCut(origin.y);

        // Use the cut vertices to calculate and create the ring
        Vector3 centre = cut.cutVertices.Select(v => deformMesh.Vertices[v])
            .Aggregate(Vector3.zero, (acc, v) => acc + v) / cut.cutVertices.Count;

        float radius = cut.cutVertices
            .Select(v => Vector3.Distance(deformMesh.Vertices[v], centre))
            .Aggregate(0f, (acc, d) => acc + d) / cut.cutVertices.Count;

        Vector3 originLocal = transform.InverseTransformPoint(origin);
        float angleOffset = VectorUtil.GetPosAngle(originLocal - centre);

        ChopRing ring = new()
        {
            centre = centre,
            radius = radius,
            origin = origin,
            angleOffset = angleOffset,
            cut = cut,
            points = new ChopRingPoint[chopRingPointCount],
        };

        // Prepare the ring cut vertices for the points
        // Each bottom vertex is a copy of the top with the lower triangles seperated
        var cutVertexInfos = cut.cutVertices
            .Select(v => new
            {
                vertexTop = v,
                vertexBottom = deformMesh.DuplicateVertex(v, (int t) =>
                    !cut.aboveVertices.Contains(deformMesh.Triangles[t * 3 + 0]) &&
                    !cut.aboveVertices.Contains(deformMesh.Triangles[t * 3 + 1]) &&
                    !cut.aboveVertices.Contains(deformMesh.Triangles[t * 3 + 2])),
                angle = VectorUtil.GetPosAngle(deformMesh.Vertices[v] - centre)
            })
            .OrderBy(v => v.angle)
            .ToArray();

        // Produce chop points at equal angles around the chop ring
        for (int i = 0; i < chopRingPointCount; i++)
        {
            float angle = (i * ChopRingPointAngleDelta + angleOffset) % (2 * Mathf.PI);

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
                isDisplaced = false,
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
            int closestPointIndex = Enumerable.Range(0, chopRingPointCount)
                .OrderBy(i => VectorUtil.GetAngleDifference(cutVertexInfo.angle, ring.points[i].angle))
                .First();
            ChopRingPoint closestPoint = ring.points[closestPointIndex];

            // Make sure it is within 1 point segment of difference and then add
            float distSq = Vector3.SqrMagnitude(closestPoint.basePos - deformMesh.Vertices[cutVertexInfo.vertexTop]);
            float segmentDst = ChopRingPointAngleDelta * ring.radius;
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

        // Now we can sort the movable vertices by angle starting from previous point
        // We want the angle to *always* be positive clockwise angle from the previous point, we know it is less than 2 PI
        for (int i = 0; i < ring.points.Length; i++)
        {
            ChopRingPoint point = ring.points[i];
            float previousAngle = (i - 1 + ring.points.Length) % ring.points.Length;
            point.movableVertices = point.movableVertices.OrderBy(v => v.angle - previousAngle + (v.angle < previousAngle ? (Mathf.PI * 2) : 0)).ToList();
        }

        deformMesh.UpdateMesh();
        chopRings.Add(ring);
        return ring;
    }

    private void ApplyChop(ChopRing ring, int pointIndex, float depth, float width, float height)
    {
        ChopRingPoint targetPoint = ring.points[pointIndex];

        // Figure out how many chop points are affected
        int affectedPointReach;
        if (width > 2 * ring.radius)
        {
            // Can at most cover half the ring
            affectedPointReach = (int)Mathf.Floor((Mathf.PI / 2f) / ChopRingPointAngleDelta);
        }
        else
        {
            // Calculate it with the angle of the triangle from the chord of the chop width
            float chopAngleCovered = 2 * Mathf.Asin(width / (2 * ring.radius));
            affectedPointReach = (int)Mathf.Floor(chopAngleCovered / ChopRingPointAngleDelta);
        }

        // Can now find which points are affected by the chop
        var affectedPoints = Enumerable.Range(-affectedPointReach, affectedPointReach * 2 + 1)
            .Select(i => ring.points[(pointIndex + i + chopRingPointCount) % chopRingPointCount])
            .Where(p => VectorUtil.GetAngleDifference(targetPoint.angle, p.angle) < Mathf.PI / 2f)
            .ToList();

        // The chop is flat and perpendicular to the target point
        // Any point within the reach is affected by AND affects the chop
        // We need to find how far away each affected point is from the perpendicular line of the target point
        // Consider a triangle at the centre with a known angle and hypotenuse, and calculate adjacent
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

        // Now we know the minimum chop depth we can apply it to each point
        // Find how deep we should go on each point then reverse to find its actual depth
        foreach (ChopRingPoint point in affectedPoints)
        {
            point.height = Mathf.Max(point.height, height);
            if (minAlignedDepth + depth < alignedDepths[point]) continue;
            float newAlignedPointDepth = Mathf.Min(minAlignedDepth + depth, alignedDepths[point] + depth);
            float newAlignedCentreDist = Mathf.Max(point.baseDistance - newAlignedPointDepth, 0f);
            float newCentreDist = newAlignedCentreDist / Mathf.Cos(point.angle - targetPoint.angle);
            point.depth = point.baseDistance - newCentreDist;
        }

        foreach (ChopRingPoint point in affectedPoints)
        {
            UpdateChopRingMesh(ring, point.index);
        }

        deformMesh.UpdateMesh();
    }

    private void UpdateChopRingMesh(ChopRing ring, int pointIndex)
    {
        // We need to keep the mesh updated with the rings topology
        // Each chop ring point has the centre point that moves backwards, then a set of vertically "movable vertices"
        // Each movable vertex is a point that is mirrored top and bottom that was setup and ordered when the ring was created
        // If we have the next point along we can just produce triangles (our furthest right point, nexts furthest left point, our middle vertex)
        // Otherwise we need to produce a mesh of triangles that connect all of the movable vertices and middle vertices of each point

        ChopRingPoint point = ring.points[pointIndex];

        // Create the internal topology if it doesn't exist
        if (!point.isDisplaced)
        {
            point.middleMeshVertex = deformMesh.AddVertex(point.basePos);
            point.isDisplaced = true;

            int nextIndex = (pointIndex + 1) % chopRingPointCount;
            int previousIndex = (pointIndex - 1 + chopRingPointCount) % chopRingPointCount;
            ChopRingPoint nextPoint = ring.points[nextIndex];
            ChopRingPoint previousPoint = ring.points[previousIndex];

            int thisRightTop = point.movableVertices.Last().topVertex;
            int thisRightBottom = point.movableVertices.Last().bottomVertex;
            int thisLeftTop = point.movableVertices.First().topVertex;
            int thisLeftBottom = point.movableVertices.First().bottomVertex;
            int nextLeftTop = nextPoint.movableVertices.First().topVertex;
            int nextLeftBottom = nextPoint.movableVertices.First().bottomVertex;
            int prevRightTop = previousPoint.movableVertices.Last().topVertex;
            int prevRightBottom = previousPoint.movableVertices.Last().bottomVertex;

            // If next is already created then fill in the *diamond* shape being opened up
            if (nextPoint.isDisplaced)
            {
                int nextMiddle = nextPoint.middleMeshVertex;
                deformMesh.AddTriangle(point.middleMeshVertex, thisRightTop, nextMiddle);
                deformMesh.AddTriangle(point.middleMeshVertex, nextMiddle, thisRightBottom);
            }

            // Otherwise then create triangles that will become the *hourglass* shape when the next point is displaced
            else
            {
                deformMesh.AddTriangle(point.middleMeshVertex, thisRightTop, nextLeftTop);
                deformMesh.AddTriangle(point.middleMeshVertex, nextLeftBottom, thisRightBottom);
            }

            // and the same logic for the leftside
            if (previousPoint.isDisplaced)
            {
                int prevMiddle = previousPoint.middleMeshVertex;
                deformMesh.AddTriangle(point.middleMeshVertex, prevMiddle, thisLeftTop);
                deformMesh.AddTriangle(point.middleMeshVertex, thisLeftBottom, prevMiddle);
            }
            else
            {
                deformMesh.AddTriangle(point.middleMeshVertex, prevRightTop, thisLeftTop);
                deformMesh.AddTriangle(point.middleMeshVertex, thisLeftBottom, prevRightBottom);
            }

            // Now create the triangle fan for each of the movable vertices
            for (int i = 0; i < point.movableVertices.Count - 1; i++)
            {
                ChopRingPointVertex mv = point.movableVertices[i];
                ChopRingPointVertex nextMv = point.movableVertices[i + 1];
                deformMesh.AddTriangle(point.middleMeshVertex, mv.topVertex, nextMv.topVertex);
                deformMesh.AddTriangle(point.middleMeshVertex, nextMv.bottomVertex, mv.bottomVertex);
            }
        }

        // Update the topology with the depth and width
        deformMesh.MoveVertex(point.middleMeshVertex, point.basePos + (ring.centre - point.basePos).normalized * point.depth);
        foreach (ChopRingPointVertex mv in point.movableVertices)
        {
            deformMesh.MoveVertex(mv.topVertex, mv.basePos + Vector3.up * point.height);
            deformMesh.MoveVertex(mv.bottomVertex, mv.basePos - Vector3.up * point.height);
        }
    }

    private void UpdateChopTarget(ChopTarget target)
    {
        target.pos = GetChopRingPointPos(target.ring, target.ringPoint);
        target.normal = (new Vector3(target.pos.x - target.ring.centre.x, 0, target.pos.z - target.ring.centre.z)).normalized;
    }

    private Vector3 GetChopRingPointPos(ChopRing ring, int pointIndex)
    {
        ChopRingPoint point = ring.points[pointIndex];
        if (point.isDisplaced) return deformMesh.Vertices[point.middleMeshVertex];
        else return point.basePos;
    }

    private int GetChopRingPointIndex(ChopRing ring, Vector3 worldPos)
    {
        float angle = (GetChopRingPosAngle(ring, worldPos) + 4 * Mathf.PI) % (2 * Mathf.PI);
        return Mathf.RoundToInt(angle / ChopRingPointAngleDelta) % chopRingPointCount;
    }

    private float GetChopRingPosAngle(ChopRing ring, Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        return VectorUtil.GetPosAngle(localPos - ring.centre) - ring.angleOffset;
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

                    if (point.isDisplaced) Gizmos.color = Color.grey;
                    else Gizmos.color = Color.HSVToRGB(pct, 1f, 1f);

                    Vector3 basePos = transform.TransformPoint(point.basePos);
                    Gizmos.DrawSphere(basePos, 0.004f);

                    if (point.isDisplaced)
                    {
                        Gizmos.color = Color.HSVToRGB(pct, 1f, 1f);

                        Vector3 pointPos = transform.TransformPoint(GetChopRingPointPos(chopRing, point.index));
                        Gizmos.DrawSphere(pointPos, 0.006f);
                        Gizmos.DrawLine(basePos, pointPos);

                        for (int i = 0; i < point.movableVertices.Count; i++)
                        {
                            ChopRingPointVertex mv = point.movableVertices[i];
                            Vector3 topPos = transform.TransformPoint(deformMesh.Vertices[mv.topVertex]);
                            Vector3 bottomPos = transform.TransformPoint(deformMesh.Vertices[mv.bottomVertex]);
                            float pr = i == 0 ? 0.002f : i == point.movableVertices.Count - 1 ? 0.006f : 0.004f;
                            Gizmos.DrawSphere(topPos, pr);
                            Gizmos.DrawSphere(bottomPos, pr);
                            Gizmos.DrawLine(pointPos, topPos);
                            Gizmos.DrawLine(pointPos, bottomPos);
                        }
                    }
                    else
                    {
                        Gizmos.color = Color.grey;
                        for (int i = 0; i < point.movableVertices.Count; i++)
                        {
                            ChopRingPointVertex mv = point.movableVertices[i];
                            Vector3 topPos = transform.TransformPoint(deformMesh.Vertices[mv.topVertex]);
                            Gizmos.DrawSphere(topPos, 0.002f);
                        }
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DeformableMesh
{
    public Mesh Mesh => mesh;
    public List<Vector3> Vertices => vertices;
    public List<int> Triangles => triangles;

    public DeformableMesh(MeshFilter mf, List<Vector3> vertices, List<int> triangles)
    {
        this.mesh = new Mesh();
        this.vertices = vertices;
        this.triangles = triangles;
        mf.mesh = mesh;
        UpdateMesh();
    }

    public int AddVertex(Vector3 position)
    {
        vertices.Add(position);
        return vertices.Count - 1;
    }

    public void MoveVertex(int index, Vector3 newPosition)
    {
        if (index < 0 || index >= vertices.Count)
        {
            Debug.LogError("Vertex index out of bounds");
            return;
        }
        vertices[index] = newPosition;
    }

    public void RemoveVertex(int index)
    {
        if (index < 0 || index >= vertices.Count)
        {
            Debug.LogError("Vertex index out of bounds");
            return;
        }
        vertices.RemoveAt(index);
        for (int i = triangles.Count - 1; i >= 0; i--)
        {
            if (triangles[i] == index) RemoveTriangle(i);
            else if (triangles[i] > index) triangles[i]--;
        }
    }

    public int DuplicateVertex(int index, Func<int, bool> isTriangleTransferred)
    {
        if (index < 0 || index >= vertices.Count)
        {
            Debug.LogError("Vertex index out of bounds");
            return -1;
        }
        Vector3 newPosition = vertices[index];
        int newIndex = AddVertex(newPosition);
        for (int i = 0; i < triangles.Count; i += 3)
        {
            // If the triangle contains the vertex then transfer the vertex if needed
            if (triangles[i] == index || triangles[i + 1] == index || triangles[i + 2] == index)
            {
                if (isTriangleTransferred(i / 3))
                {
                    if (triangles[i] == index) triangles[i] = newIndex;
                    if (triangles[i + 1] == index) triangles[i + 1] = newIndex;
                    if (triangles[i + 2] == index) triangles[i + 2] = newIndex;
                }
            }
        }

        return newIndex;
    }

    public int SplitEdge(int a, int b, Vector3 pos)
    {
        if (a < 0 || a >= vertices.Count || b < 0 || b >= vertices.Count)
        {
            Debug.LogError("Vertex index out of bounds");
            return -1;
        }
        int newIndex = AddVertex(pos);
        for (int i = 0; i < triangles.Count; i += 3)
        {
            int c = -1;
            bool clockwise = true;
            if (triangles[i] == a && triangles[i + 1] == b) { c = triangles[i + 2]; clockwise = true; }
            else if (triangles[i] == b && triangles[i + 1] == a) { c = triangles[i + 2]; clockwise = false; }
            else if (triangles[i + 1] == a && triangles[i + 2] == b) { c = triangles[i]; clockwise = true; }
            else if (triangles[i + 1] == b && triangles[i + 2] == a) { c = triangles[i]; clockwise = false; }
            else if (triangles[i + 2] == a && triangles[i] == b) { c = triangles[i + 1]; clockwise = true; }
            else if (triangles[i + 2] == b && triangles[i] == a) { c = triangles[i + 1]; clockwise = false; }
            if (c == -1) continue;

            RemoveTriangle(i);
            if (clockwise)
            {
                AddTriangle(newIndex, b, c);
                AddTriangle(newIndex, c, a);
            }
            else
            {
                AddTriangle(newIndex, c, b);
                AddTriangle(newIndex, a, c);
            }
            i -= 3;
        }
        return newIndex;
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    public void AddTriangleQuad(int a, int b, int c, int d)
    {
        AddTriangle(a, b, c);
        AddTriangle(a, c, d);
    }

    public void RemoveTriangle(int index)
    {
        if (index < 0 || index >= triangles.Count)
        {
            Debug.LogError("Triangle index out of bounds");
            return;
        }
        triangles.RemoveRange(index, 3);
    }

    public void RemoveTriangles(List<int> indices)
    {
        if (indices == null || indices.Count == 0) return;
        indices.Sort((a, b) => b.CompareTo(a));
        foreach (int index in indices)
        {
            RemoveTriangle(index);
        }
    }

    public Vector3 GetTriangleCentre(int triangleIndex)
    {
        if (triangleIndex < 0 || triangleIndex >= triangles.Count)
        {
            Debug.LogError("Triangle index out of bounds");
            return Vector3.zero;
        }
        int a = triangles[triangleIndex];
        int b = triangles[triangleIndex + 1];
        int c = triangles[triangleIndex + 2];
        return (vertices[a] + vertices[b] + vertices[c]) / 3f;
    }

    public List<int> FloodFillTriangles(int start, Func<int, int, int, bool> triangleFilter = null)
    {
        if (start < 0 || start >= triangles.Count)
        {
            Debug.LogError("Start index out of bounds");
            return new List<int>();
        }

        // Prepare the vertex-to-triangles mapping for quick access
        Dictionary<int, List<int>> vertexToTriangles = new();
        for (int i = 0; i < triangles.Count; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                int vertexIndex = triangles[i + j];
                if (!vertexToTriangles.ContainsKey(vertexIndex))
                {
                    vertexToTriangles[vertexIndex] = new List<int>();
                }
                vertexToTriangles[vertexIndex].Add(i);
            }
        }

        // Perform a BFS to find all connected triangles
        List<int> result = new();
        Queue<int> queue = new();
        HashSet<int> visited = new();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            int currentTriangle = queue.Dequeue();
            if (visited.Contains(currentTriangle)) continue;

            visited.Add(currentTriangle);
            result.Add(currentTriangle);

            // Queue each triangle attached to the current triangle taking into account the filter
            for (int i = 0; i < 3; i++)
            {
                int vertexIndex = triangles[currentTriangle + i];
                if (vertexToTriangles.TryGetValue(vertexIndex, out List<int> connectedTriangles))
                {
                    foreach (int connectedTriangle in connectedTriangles)
                    {
                        if (!visited.Contains(connectedTriangle) && connectedTriangle != currentTriangle &&
                            (triangleFilter == null || triangleFilter(triangles[connectedTriangle], triangles[connectedTriangle + 1], triangles[connectedTriangle + 2])))
                        {
                            queue.Enqueue(connectedTriangle);
                        }
                    }
                }
            }
        }

        return result;
    }

    public RingCut CreateRingCut(float y)
    {
        // Go over every triangle and change the geometry to include a ring cut at the specified y value
        // Each triangle has either 0 edges or 2 edges that intersect the y value
        // When a triangle is cut  need to turn it into 3 new triangles
        Dictionary<(int, int), int> edgeCuts = new();

        // Loop backwards to allow triangle array modification without index issues
        RingCut ringCut = new();
        for (int i = triangles.Count - 3; i >= 0; i -= 3)
        {
            int aIdx = triangles[i];
            int bIdx = triangles[i + 1];
            int cIdx = triangles[i + 2];
            bool aUp = vertices[aIdx].y >= y;
            bool bUp = vertices[bIdx].y >= y;
            bool cUp = vertices[cIdx].y >= y;

            // All above or all below so ignore
            if (aUp && bUp && cUp) continue;
            else if (!aUp && !bUp && !cUp) continue;

            // 1 point must be on opposite side to the other 2
            int paIdx = -1, pbIdx = -1, pcIdx = -1;
            if (aUp != bUp && bUp == cUp) { paIdx = aIdx; pbIdx = bIdx; pcIdx = cIdx; }
            else if (bUp != aUp && aUp == cUp) { paIdx = bIdx; pbIdx = cIdx; pcIdx = aIdx; }
            else if (cUp != aUp && aUp == bUp) { paIdx = cIdx; pbIdx = aIdx; pcIdx = bIdx; }
            else Debug.LogError("Unexpected triangle configuration: " + aUp + ", " + bUp + ", " + cUp);

            // Now create the edge cuts or re-use if already created
            (int, int) edgeAB = paIdx < pbIdx ? (paIdx, pbIdx) : (pbIdx, paIdx);
            (int, int) edgeAC = paIdx < pcIdx ? (paIdx, pcIdx) : (pcIdx, paIdx);
            if (!edgeCuts.ContainsKey(edgeAB))
            {
                edgeCuts[edgeAB] = AddVertex(Vector3.Lerp(vertices[paIdx], vertices[pbIdx], (y - vertices[paIdx].y) / (vertices[pbIdx].y - vertices[paIdx].y)));
            }
            if (!edgeCuts.ContainsKey(edgeAC))
            {
                edgeCuts[edgeAC] = AddVertex(Vector3.Lerp(vertices[paIdx], vertices[pcIdx], (y - vertices[paIdx].y) / (vertices[pcIdx].y - vertices[paIdx].y)));
            }
            int abCutIdx = edgeCuts[edgeAB];
            int acCutIdx = edgeCuts[edgeAC];

            // Update the topology
            RemoveTriangle(i);
            AddTriangle(paIdx, abCutIdx, acCutIdx);
            AddTriangleQuad(abCutIdx, pbIdx, pcIdx, acCutIdx);

            // Update the lists
            if (aUp) ringCut.aboveVertices.Add(aIdx);
            else ringCut.belowVertices.Add(aIdx);
            if (bUp) ringCut.aboveVertices.Add(bIdx);
            else ringCut.belowVertices.Add(bIdx);
            if (cUp) ringCut.aboveVertices.Add(cIdx);
            else ringCut.belowVertices.Add(cIdx);
            ringCut.cutVertices.Add(abCutIdx);
            ringCut.cutVertices.Add(acCutIdx);
        }

        return ringCut;
    }

    public void UpdateMesh()
    {
        List<Vector3> meshVertices = new(triangles.Count);
        List<int> meshTriangles = new(triangles.Count);

        for (int i = 0; i < triangles.Count; i += 3)
        {
            meshVertices.Add(vertices[triangles[i]]);
            meshVertices.Add(vertices[triangles[i + 1]]);
            meshVertices.Add(vertices[triangles[i + 2]]);
            meshTriangles.Add(i);
            meshTriangles.Add(i + 1);
            meshTriangles.Add(i + 2);
        }

        mesh.Clear();
        mesh.vertices = meshVertices.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public class RingCut
    {
        public HashSet<int> aboveVertices = new();
        public HashSet<int> cutVertices = new();
        public HashSet<int> belowVertices = new();
    }

    private readonly Mesh mesh;
    private readonly List<Vector3> vertices;
    private readonly List<int> triangles;
}

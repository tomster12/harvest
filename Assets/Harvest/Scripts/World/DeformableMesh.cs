using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DeformableMesh
{
    public Mesh Mesh => mesh;

    public DeformableMesh(MeshFilter mf, List<Vector3> vertices, List<int> triangles)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        mesh = new Mesh();
        mf.mesh = mesh;
        UpdateMesh();
    }

    private class EdgeCut
    {
        public int cutIdx;
    }

    public void MakeRingCut(float y)
    {
        // Go over every triangle and change the geometry to include a ring cut at the specified y value
        // Each triangle has either 0 edges or 2 edges that intersect the y value
        // When a triangle is cut  need to turn it into 3 new triangles
        var vertices = mesh.vertices;
        Dictionary<(int, int), EdgeCut> edgeCuts = new();

        // Loop backwards to allow triangle array modification without index issues
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
            (int, int) edgeAB = (Mathf.Min(paIdx, pbIdx), Mathf.Max(paIdx, pbIdx));
            (int, int) edgeAC = (Mathf.Min(paIdx, pcIdx), Mathf.Max(paIdx, pcIdx));
            if (!edgeCuts.TryGetValue(edgeAB, out EdgeCut _))
            {
                Vector3 abCut = Vector3.Lerp(vertices[paIdx], vertices[pbIdx], (y - vertices[paIdx].y) / (vertices[pbIdx].y - vertices[paIdx].y));
                edgeCuts[edgeAB] = new EdgeCut { cutIdx = AddVertex(abCut) };
            }
            if (!edgeCuts.TryGetValue(edgeAC, out EdgeCut _))
            {
                Vector3 acCut = Vector3.Lerp(vertices[paIdx], vertices[pcIdx], (y - vertices[paIdx].y) / (vertices[pcIdx].y - vertices[paIdx].y));
                edgeCuts[edgeAC] = new EdgeCut { cutIdx = AddVertex(acCut) };
            }
            int abCutIdx = edgeCuts[edgeAB].cutIdx;
            int acCutIdx = edgeCuts[edgeAC].cutIdx;

            // Update the topology with the new vertices
            RemoveTriangle(i);
            AddTriangle(paIdx, abCutIdx, acCutIdx);
            AddTriangleQuad(abCutIdx, pbIdx, pcIdx, acCutIdx);
        }

        UpdateMesh();
    }

    private Mesh mesh;
    private List<Vector3> vertices;
    private List<int> triangles;

    private void MoveVertex(int index, Vector3 newPosition)
    {
        if (index < 0 || index >= vertices.Count)
        {
            Debug.LogError("Vertex index out of bounds");
            return;
        }
        vertices[index] = newPosition;
    }

    private int AddVertex(Vector3 position)
    {
        vertices.Add(position);
        return vertices.Count - 1;
    }

    private void AddTriangle(int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    private void AddTriangleQuad(int a, int b, int c, int d)
    {
        AddTriangle(a, b, c);
        AddTriangle(a, c, d);
    }

    private void RemoveTriangle(int index)
    {
        if (index < 0 || index >= triangles.Count)
        {
            Debug.LogError("Triangle index out of bounds");
            return;
        }
        triangles.RemoveRange(index, 3);
    }

    private void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}

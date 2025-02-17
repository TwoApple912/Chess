using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CreateMeshFromLineRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private MeshFilter meshFilter;
    private Mesh mesh;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    private void Update()
    {
        CreateMesh();
    }

    void CreateMesh()
    {
        int steps = lineRenderer.positionCount;
        Vector3[] vertices = new Vector3[steps + 1];
        int[] triangles = new int[steps * 3];

        // Center vertex
        vertices[0] = Vector3.zero;

        // Define vertices
        for (int i = 0; i < steps; i++)
        {
            vertices[i + 1] = lineRenderer.GetPosition(i);
        }

        // Define triangles with reversed order
        for (int i = 0; i < steps; i++)
        {
            int startIndex = i * 3;
            triangles[startIndex] = 0;
            triangles[startIndex + 1] = (i + 1) % steps + 1;
            triangles[startIndex + 2] = i + 1;
        }

        // Assign vertices and triangles to the mesh
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}

using System;
using UnityEngine;

public class PromoteAnimationController : MonoBehaviour
{
    [Header("References")]
    private Morpher morpher;

    private void Awake()
    {
        morpher = GetComponent<Morpher>();
    }

    private void Start()
    {
        morpher._newMesh = CreateCircleMesh(1, 60, new Vector3(0, 1, 0));
        //morpher._newMesh = CreateSphereMesh(1, 30, 30, new Vector3(0, 1, 0));
    }

    public Mesh CreateCircleMesh(float radius, int segments, Vector3 offset)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return null;

        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        Vector3 cameraUp = mainCamera.transform.up;

        vertices[0] = offset;
        float angleStep = 360.0f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            Vector3 direction = Mathf.Cos(angle) * cameraRight + Mathf.Sin(angle) * cameraUp;
            vertices[i + 1] = direction * radius + offset;
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = (i + 1) % segments + 1;
            triangles[i * 3 + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
    
    public Mesh CreateSphereMesh(float radius, int longitudeSegments, int latitudeSegments, Vector3 offset)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(longitudeSegments + 1) * (latitudeSegments + 1)];
        int[] triangles = new int[longitudeSegments * latitudeSegments * 6];

        int vertIndex = 0;
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float theta = lat * Mathf.PI / latitudeSegments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float phi = lon * 2 * Mathf.PI / longitudeSegments;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                Vector3 vertex = new Vector3(
                    cosPhi * sinTheta,
                    cosTheta,
                    sinPhi * sinTheta
                ) * radius + offset;

                vertices[vertIndex++] = vertex;
            }
        }

        int triIndex = 0;
        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int current = lat * (longitudeSegments + 1) + lon;
                int next = current + longitudeSegments + 1;

                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
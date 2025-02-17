using UnityEngine;

public class DrawCircle : MonoBehaviour
{
    public LineRenderer line;

    private void Start()
    {
        DrawCircleLine(60, 1);
        CreateMeshFromLineRenderer();
    }

    void DrawCircleLine(int steps, float radius)
    {
        line.positionCount = steps;

        for (int currentStep = 0; currentStep < steps; currentStep++)
        {
            float circumferenceProgress = (float)currentStep / steps;
            
            float currentRadian = Mathf.PI * 2 * circumferenceProgress;

            float xScaled = Mathf.Cos(currentRadian);
            float yScaled = Mathf.Sin(currentRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            Vector3 currentPosition = new Vector3(x, y, 0);
            
            line.SetPosition(currentStep, currentPosition);
        }
    }

    void CreateMeshFromLineRenderer()
    {
        Mesh mesh = new Mesh();
        int steps = line.positionCount;
        Vector3[] vertices = new Vector3[steps + 1];
        int[] triangles = new int[steps * 3];

        // Center vertex
        vertices[0] = Vector3.zero;

        // Define vertices
        for (int i = 0; i < steps; i++)
        {
            vertices[i + 1] = line.GetPosition(i);
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
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // Assign the mesh to the MeshFilter
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }
}

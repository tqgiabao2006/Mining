        using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elip : MonoBehaviour
{
    // Start is called before the first frame update
    List<Vector3> elipVertices = new List<Vector3>();
    List<int> triangles = new List<int>();  
    Vector2 center = new Vector2(0.5f, 0.5f);
    public float a = 0.3f;
     public float b = 0.5f;

     private MeshFilter meshFilter;
    

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        AddElipVertices(center, triangles, elipVertices, 180, 360, 20);
        mesh.vertices = elipVertices.ToArray();
        mesh.triangles = triangles.ToArray();
        meshFilter.mesh = mesh; 
    }

    private void AddElipVertices(Vector2 nodePos, List<int> triangles, List<Vector3> vertices ,float startAngle, float endAngle, int smoothness)
    {
        float RoadWidth = 0.2f;
        float halfWidth = RoadWidth / 2f;  
        // Vector3 center = new Vector3(0.7f, 1f, 0.0f);   
        Vector3 center = new Vector3(0.7f, 1f, 0.0f);   
        
        // Center is already calculated with nodePos, so no need to add nodePos.x/y here.
        // Vector3 triangleOrigin = new Vector3(nodePos.x + halfWidth, nodePos.x + halfWidth * 2.4f);
        Vector3 triangleOrigin = new Vector3(center.x, center.y - 0.3f, 0.0f);   

        float a = 0.05f;
        float b = 0.12f;

        // Convert to rad:
        startAngle *= Mathf.Deg2Rad;
        endAngle *= Mathf.Deg2Rad;
        
        // Add triangles for the ellipse
        int startIndex = vertices.Count;
        
        // Generate ellipse vertices
        for (int i = 0; i <= smoothness; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)smoothness);
            float x = center.x + a * Mathf.Cos(angle);  // Do not add nodePos here
            float y = center.y + b * Mathf.Sin(angle);  // Do not add nodePos here

            vertices.Add(new Vector3(x, y, 0));
        }

        // Add center vertex
        int triangleOriginIndex = vertices.Count;
        vertices.Add(triangleOrigin); 
        
        for (int i = startIndex; i < vertices.Count - 1; i++)
        {
            triangles.AddRange(new int[] { triangleOriginIndex, i, i + 1 });
        }
    }

    // private void OnDrawGizmos()
    // {
    //     if(elipVertices.Count == 0) return;
    //     foreach (var vertex in elipVertices)
    //     {
    //         Gizmos.DrawLine(center, vertex);
    //     }
    // }
    
}

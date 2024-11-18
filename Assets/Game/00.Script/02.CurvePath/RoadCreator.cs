using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(CurvePathCreator))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RoadCreator : MonoBehaviour
{
    public float roadWidth = 1f;
    [Range(0.1f, 1.5f)] public float spacing = 1f;

    public bool autoUpdate;
    public float tilling = 1;
    
    public void UpdateRoad()
    {
        CurvePath path = GetComponent<CurvePathCreator>().path;
        Vector2[] points = path.CalculateEvenlySpacedPoints(spacing);
        GetComponent<MeshFilter>().mesh = CreateRoadMesh(points, path.IsClosed);
        
        int texturesRepeat = Mathf.RoundToInt(tilling * points.Length * spacing * 0.5f); //maintain constant size of a white line
        GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, texturesRepeat);
    }

    Mesh CreateRoadMesh(Vector2[] points, bool isClosed)
    {
        // n_vertices = 2n
        // n_triangles = 2(n-1)

        Vector3[] vertices = new Vector3[points.Length * 2];
        //UVs:
        Vector2[] UVs = new Vector2[vertices.Length];
            
        int numbTriangles = 2 * (points.Length - 1) + ((isClosed) ? 2 : 0); //If closed, adding 2 more triangles to bride the gap
        int[] triangles = new int[numbTriangles * 3]; //*3 because 3 vertices per triangle
        int vertexIndex = 0;
        int triangleIndex = 0;
        for (int i = 0; i < points.Length; i++)
        {
            // Get forward vector
            Vector2 forward = Vector2.zero;
            if (i < points.Length - 1 || isClosed)
            {
                forward += points[(i + 1)% points.Length] - points[i];
            }

            if (i > 0 || isClosed)
            {
                forward += points[i] - points[(i - 1 + points.Length) % points.Length]; // add points.Length to always positive
            }

            forward.Normalize();

            // Calculate the left vector perpendicular to the forward vector
            Vector2 left = new Vector2(-forward.y, forward.x);

            // Set the vertices for left and right side of the road
            vertices[vertexIndex] = points[i] + left * roadWidth * 0.5f; // Left side vertex
            vertices[vertexIndex + 1] = points[i] - left * roadWidth * 0.5f; // Right side vertex
            
            //Setting verticles:
            // (0,0) bottom left of texture, (1,0) bottom right
            // (0, 0.5) middle left, (1, 0.5) middle right
            // (0,1) top left, (1,1) top right
            float completionPercent = i/(float)(points.Length - 1); 
            /*Core idea: Mapping UV half of map go 0->1 then 1 ->0 in 2nd half
                y = 1 -|2x-1|
             */
            float v = 1 - Mathf.Abs(2 * completionPercent - 1);
            
            
            UVs[vertexIndex] = new Vector2(0,v);
            UVs[vertexIndex + 1] = new Vector2(1, v);


            // Create triangles
            if (i < points.Length - 1 || isClosed)
            {
                /*
                   v1----v3
                    |   / |
                    |  /  |
                    | /   |
                   v2----v4
                 */
                // First triangle (v -> v+2 -> v+1)
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = (vertexIndex + 2) % vertices.Length; //Loop index avoid out bound
                triangles[triangleIndex + 2] = vertexIndex + 1;

                // Second triangle (V+1 -> v+2 -> v+3)
                triangles[triangleIndex + 3] = vertexIndex + 1;
                triangles[triangleIndex + 4] = (vertexIndex + 2) % vertices.Length;
                triangles[triangleIndex + 5] =(vertexIndex + 3) % vertices.Length;

                triangleIndex += 6;
            }

            vertexIndex += 2;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = UVs;
        mesh.RecalculateNormals();
        return mesh;
    }

    
    
}
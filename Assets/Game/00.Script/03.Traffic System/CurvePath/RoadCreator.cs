using DG.Tweening.Plugins.Core.PathCore;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.CurvePath
{
    [RequireComponent(typeof(PathCreator))]
    [RequireComponent(typeof(MeshRenderer))] 
    [RequireComponent(typeof(MeshFilter))]
    public class RoadCreator: MonoBehaviour
    {
        [Range(0.05f, 1.5f)]
        public float spacing = 1;
        public float roadWidth = 0.4f;
        public bool autoUpdate = true;

        public void UpdateRoad()
        {
            CurvePath path = this.GetComponent<PathCreator>().Path;
            Vector2[] points = path.GetEvenlyPoint(spacing);
            this.GetComponent<MeshFilter>().mesh = CreateRoadMesh(points, path.IsClosed);
        }
        
        public Mesh CreateRoadMesh(Vector2[] points, bool isClosed)
        {
            Vector3[] verts = new Vector3[points.Length * 2];
            int numbTris = 2 * (points.Length - 1) + (isClosed ? 2 : 0);
            int[] tris = new int[numbTris* 3];
            Vector2[] uvs = new Vector2[verts.Length];
            
            int vertIndex = 0;
            int triIndex = 0;

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 forward = Vector2.zero;

                //Blend betweeen two
                if (i < points.Length - 1 || isClosed)
                {
                    forward += points[(i + 1)%points.Length] - points[i];
                }

                if (i > 0 || isClosed)
                {
                    forward += points[i] - points[(i - 1 + points.Length)%points.Length];
                }
                
                forward.Normalize();
                
                //Orthogonal vector
                Vector2 left = new Vector2(-forward.y, forward.x);
                
                verts[vertIndex] = points[i] + left * roadWidth *0.5f;
                verts[vertIndex + 1] = points[i] - left * roadWidth *0.5f;

                float completePer = i / (float)(points.Length - 1);
                uvs[vertIndex] = new Vector2(0, completePer);
                uvs[vertIndex + 1] = new Vector2(1, completePer);
                
                if (i < points.Length - 1 || isClosed)
                {
                    tris[triIndex] = vertIndex;
                    tris[triIndex + 1] = (vertIndex + 2)%verts.Length;
                    tris[triIndex + 2] = vertIndex + 1;
                    
                    tris[triIndex + 3] = vertIndex + 1;
                    tris[triIndex + 4] = (vertIndex + 2)%verts.Length;
                    tris[triIndex + 5] = (vertIndex + 3)%verts.Length;
                }
                
                vertIndex += 2;
                triIndex += 6;
            }
            
            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            return mesh;
        }
    }
}
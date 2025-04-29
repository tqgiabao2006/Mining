using System.Collections;
using System.Collections.Generic;
using Game._00.Script._03.Traffic_System.CurvePath;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class CurveRoadMesh
{

    [Range(0.05f, 1.5f)] private float _spacing = 1;
    private float _roadWidth = 0.5f;
    private bool _autoUpdate = true;

    private MeshFilter _meshFilter;

    private CurvePath _bezierPath;

    public CurveRoadMesh(MeshFilter meshFilter, float spacing, float roadWidth, bool autoUpdate)
    {
        _spacing = spacing;
        _roadWidth = roadWidth;
        _autoUpdate = autoUpdate;

        _meshFilter = meshFilter;

    }
    
    public void UpdateRoadMesh(CurvePath bezierPath)
    {
        Vector2[] points = _bezierPath.GetEvenlyPoint(_spacing);
        _meshFilter.mesh = CreateRoadMesh(points, false);
    }

    public void UpdateRoadMesh(CatmullRomSpline spline)
    {
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < spline.NumbSeg; i++)
        {
            points.AddRange(spline[i].GetEvenlySpacingPoints(_spacing));
        }
        _meshFilter.mesh = CreateRoadMesh(points.ToArray(), false);
    }
    public Mesh CreateRoadMesh(Vector2[] points, bool isClosed)
    {
        Vector3[] verts = new Vector3[points.Length * 2];
        int numbTris = 2 * (points.Length - 1) + (isClosed ? 2 : 0);
        int[] tris = new int[numbTris * 3];
        Vector2[] uvs = new Vector2[verts.Length];

        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 forward = Vector2.zero;

            //Blend betweeen two
            if (i < points.Length - 1 || isClosed)
            {
                forward += points[(i + 1) % points.Length] - points[i];
            }

            if (i > 0 || isClosed)
            {
                forward += points[i] - points[(i - 1 + points.Length) % points.Length];
            }

            forward.Normalize();

            //Orthogonal vector
            Vector2 left = new Vector2(-forward.y, forward.x);

            verts[vertIndex] = points[i] + left * _roadWidth * 0.5f;
            verts[vertIndex + 1] = points[i] - left * _roadWidth * 0.5f;

            float completePer = i / (float)(points.Length - 1);
            uvs[vertIndex] = new Vector2(0, completePer);
            uvs[vertIndex + 1] = new Vector2(1, completePer);

            if (i < points.Length - 1 || isClosed)
            {
                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 5] = (vertIndex + 3) % verts.Length;
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

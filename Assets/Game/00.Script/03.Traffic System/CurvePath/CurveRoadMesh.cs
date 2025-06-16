using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game._00.Script._03.Traffic_System.CurvePath;
using Game._00.Script._03.Traffic_System.Road;
using Game._00.Script._04.Timer.CurvePath;
using Unity.Entities.UniversalDelegates;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CurveRoadMesh:MonoBehaviour
{
    [Header("Debug settings")]
    [SerializeField] private bool isGizmos;
    
    [SerializeField] private bool showLine;
    
    [SerializeField] private bool showPoint;
    
    [Header("Mesh settings")]

    [Tooltip("The smaller, the smoother the curve")]
    [Range(0.05f, 0.2f)] public readonly static float spacing = 0.05f;
    
    [Tooltip("The larger, the smoother the curve")] 
    [Range(1, 20)] public readonly static int curveSmooth = 10;
    
    private float _roadWidth = 0.5f;
    
    private MeshFilter _meshFilter;

    private Dictionary<BezierSpline, CombineInstance> _splines;
    
    private List<Vector2> _points;

    private List<Vector3> _vertices;
    
    private void Start()
    {
        _meshFilter = this.GetComponent<MeshFilter>();

        _splines = new Dictionary<BezierSpline, CombineInstance>();
        
        _roadWidth = RoadManager.RoadWidth;
        
        _points = new List<Vector2>();
        
        _vertices = new List<Vector3>();
    }

    public BezierSpline CreateSpline()
    {
        BezierSpline spline = new BezierSpline(CreateSpline ,CreateRoadMesh, UpdateRoadMesh, CreateIntersection,spacing, curveSmooth);
        _splines.Add(spline, new CombineInstance());   
        return spline;
    }
    private void UpdateRoadMesh(BezierSpline spline)
    {
        if (!_splines.ContainsKey(spline)) return;

        // Recreate this spline's mesh
        CombineInstance updated = new CombineInstance();
        updated.mesh = spline.Mesh;
        updated.transform = Matrix4x4.identity;
        
        _splines[spline] = updated;

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(_splines.Values.ToArray(), true, true);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        _meshFilter.mesh = mesh;
    }
    
    
    private void CreateIntersection(Vector3[] points, Vector3 center)
    {
        for (int i = 0; i < points.Length; i++)
        {
            Debug.Log(i + " " + points[i]);
        }
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        verts.Add(center);

        for (int i = 1; i < points.Length; i += 2)
        {
            int next = (i + 1) % points.Length; // wrap around
            Vector2 dir = (points[next] - points[i]).normalized;
            
            Vector2 mid = Vector2.Lerp(points[i], points[next], 0.5f);
            Vector2 centerMid = Vector2.Lerp(mid, center, 0.4f);
            
            Debug.Log($"{i} Lerp({points[i]}, {points[next]},0,5) =  mid " + mid);
            Debug.Log($"{i} center " + centerMid);

            //Straight line case
            if (Mathf.Approximately(dir.x, 0) ||  Mathf.Approximately(dir.y, 0))
            {
                verts.Add(points[i]);
                verts.Add(points[next]);
            }
            else
            {
                for (int j = 0; j <= curveSmooth; j++)
                {
                    float t = j /(float)curveSmooth;
                    verts.Add(BezierCurve.GetPoint(points[i], centerMid ,centerMid, points[next], t));
                }
            }
            
        }
        
        for (int i = 1; i < verts.Count - 1; i++)
        {
            tris.AddRange(new int[] { 0, i, i + 1 });
        }
        
        //Close the loop
        tris.AddRange(new int[] { 0, verts.Count - 1, 1 });
        
        Mesh updatedMesh = new Mesh();
        updatedMesh.vertices = verts.ToArray();
        updatedMesh.triangles = tris.ToArray();

        updatedMesh.RecalculateBounds();
        updatedMesh.RecalculateNormals();

        _vertices = verts;
        
        Debug.Log("V " + updatedMesh.vertices.Length);
        Debug.Log("Tri " + updatedMesh.triangles.Length);

        BezierSpline dummy = new BezierSpline(null, null, null, null, spacing, curveSmooth);
        _splines[dummy] = new CombineInstance
        {
            mesh = updatedMesh,
            transform = Matrix4x4.identity
        };

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(_splines.Values.ToArray(), true, true);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        _meshFilter.mesh = mesh;
    }
    private Mesh CreateRoadMesh(Vector3[] points)
    {
        Vector3[] verts = new Vector3[points.Length * 2];
        int numbTri = 2 * (points.Length - 1);
        int[] tris = new int[numbTri * 3];
        Vector2[] uvs = new Vector2[verts.Length];

        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {

            Vector2 forward = Vector2.zero;

            if (i < points.Length - 1)
            {
                forward += (Vector2)points[(i + 1) % points.Length] - (Vector2)points[i];
            }

            if (i > 0)
            {
                forward += (Vector2)points[i] - (Vector2)points[(i - 1 + points.Length) % points.Length];
            }

            forward.Normalize();

            //Orthogonal vector
            Vector2 left = new Vector2(-forward.y, forward.x);

            verts[vertIndex] = (Vector2)points[i] + left * _roadWidth * 0.5f;
            verts[vertIndex + 1] = (Vector2)points[i] - left * _roadWidth * 0.5f;

            float completePer = i / (float)(points.Length - 1);
            uvs[vertIndex] = new Vector2(0, completePer);
            uvs[vertIndex + 1] = new Vector2(1, completePer);

            if (i < points.Length - 1)
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

    private void OnGUI()
    {
        if (!isGizmos || _splines == null )
        {
            return;
        }
        
        GUI.Label(new Rect(10, 20, 200, 200), $"Splines count: {_splines.Count}", new GUIStyle()
        {
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
            fontSize = 20
        });

    }

    private void OnDrawGizmos()
    {
        if (!isGizmos || _splines == null || _points == null || _vertices==null)
        {
            return;
        }

        foreach (Vector3 v in _vertices)
        {
            Gizmos.DrawSphere(v, 0.1f);
        }

        foreach (Vector2 point in _points)
        {
            Gizmos.DrawSphere(point, 0.02f);
        }
        
        
        foreach (BezierSpline spline in _splines.Keys)
        {
            if (showLine)
            {
                Vector3[] points = spline.GetEvenlySpacedPoints(spacing, curveSmooth);

                foreach (Vector3 p in points)
                {
                    Gizmos.DrawSphere(p, 0.02f);
                }
            }
            if (showPoint)
            {
                for (int k = 0; k < spline.NumbSeg; k++)
                {
                    Vector2[] points = spline.GetPointInSegment(k);

                    for (int i = 0; i < points.Length; i++)
                    {
                        if (i % 3 == 0)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(points[i], 0.05f);
                        }
                        else
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawSphere(points[i], 0.02f);
                        }
                    }
                }
             
            }
        }

    }
}

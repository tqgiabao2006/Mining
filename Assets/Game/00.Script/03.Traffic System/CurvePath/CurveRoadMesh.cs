using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game._00.Script._03.Traffic_System.CurvePath;
using Game._00.Script._03.Traffic_System.Road;
using Game._00.Script._04.Timer.CurvePath;
using Unity.Entities.UniversalDelegates;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CurveRoadMesh:MonoBehaviour
{
    [Header("Debug settings")]
    [SerializeField] private bool isGizmos;
    
    [SerializeField] private bool showCurve;
    
    [SerializeField] private bool showPoint;
    
    [Header("Mesh settings")]
    [Tooltip("The larger, the closer to straight line")]
    [SerializeField] [Range(0, 1)] private float alpha;

    [Tooltip("The smaller, the smoother the curve")]
    [SerializeField] [Range(0.05f, 0.2f)] private float spacing = 1;
    
    [Tooltip("The larger, the smoother the curve")]
    [SerializeField] [Range(1, 20)] private int curveSmooth = 10;
    
    private float _roadWidth = 0.5f;
    
    private MeshFilter _meshFilter;

    private Dictionary<BezierSpline, CombineInstance> _splines;
    
    private List<Vector2> _points;
    
    private void Start()
    {
        _meshFilter = this.GetComponent<MeshFilter>();

        _splines = new Dictionary<BezierSpline, CombineInstance>();
        
        _roadWidth = RoadManager.RoadWidth;
        
        _points = new List<Vector2>();
    }

    public BezierSpline CreateSpline()
    {
        BezierSpline spline = new BezierSpline(alpha, CreateRoadMesh, spacing, curveSmooth);
        _splines.Add(spline, new CombineInstance());   
        return spline;
    }
    public void UpdateRoadMesh(BezierSpline spline)
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

    
    private Mesh CreateRoadMesh(Vector2[] points)
    {
        Vector3[] verts = new Vector3[points.Length * 2];
        int numbTris = 2 * (points.Length - 1);
        int[] tris = new int[numbTris * 3];
        Vector2[] uvs = new Vector2[verts.Length];

        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 forward = Vector2.zero;

            if (i < points.Length - 1)
            {
                forward += points[(i + 1) % points.Length] - points[i];
            }

            if (i > 0)
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

    private void OnDrawGizmos()
    {
        if (!isGizmos || _splines == null || _points == null)
        {
            return;
        }

        foreach (Vector2 point in _points)
        {
            Gizmos.DrawSphere(point, 0.02f);
        }

        foreach (BezierSpline spline in _splines.Keys)
        {
            if (showCurve)
            {
                for (int i = 0; i < spline.NumbSeg; i++)
                {
                    Gizmos.color = spline[i].IsCurve ?  Color.blue : Color.green;
                    Gizmos.DrawLine(spline[i].P0, spline[i].P3);
                }
            }

            if (showPoint)
            {
                for (int i = 0; i < spline.PointCount; i++)
                { 
                    
                    if (i % 3 == 0)
                    {
                        Gizmos.color = Color.yellow;
                
                        if (i + 3 < spline.PointCount)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawLine(spline[i,true], spline[i+1,true]);
                            Gizmos.DrawLine(spline[i+1, true], spline[i+2,true]);
                            Gizmos.DrawLine(spline[i+2, true], spline[i+3,true]);
                        }
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(spline[i,true], 0.1f);
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(spline[i,true], 0.05f);
                    }
                }
            }
        }

    }
}

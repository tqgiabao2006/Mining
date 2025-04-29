using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatmullRomSpline : MonoBehaviour
{
    [Tooltip("The larger, the closer to straight line")]
    [SerializeField] [Range(0, 1)] private float alpha;
    
    private List<CatmullRomCurve> _segments;

    private List<Vector2> _controlPoints;

    public int NumbSeg
    {
        get
        {
            return _segments.Count;
        }
    }

    public CatmullRomCurve this[int index]
    {
        get
        {
            return _segments[index];
        }
    }
    
    private void Start()
    {
        _segments = new List<CatmullRomCurve>();
        
        _controlPoints = new List<Vector2>();
    }

    public void AddPoint(Vector2 point)
    {
        _controlPoints.Add(point);
        int cnt = _controlPoints.Count;

        if (cnt < 2)
        {
            return;
        }
        
        if (cnt == 2) //Single segment
        {
            _segments.Add(new CatmullRomCurve(_controlPoints[0], _controlPoints[0], _controlPoints[1], _controlPoints[1], alpha));
        }
        else
        {
            //Set last segment p4 to new one
            _segments[NumbSeg - 1] = new CatmullRomCurve(_segments[NumbSeg-1].p0, _segments[NumbSeg-1].p1, _segments[NumbSeg-1].p2, _controlPoints[cnt-1], alpha);
            
            //Add new
            _segments.Add(new CatmullRomCurve(_controlPoints[Mathf.Max(0,cnt - 3)], _controlPoints[cnt - 2], _controlPoints[cnt - 1], _controlPoints[cnt - 1], alpha));
            
        }
    }

    public CatmullRomCurve GetCurve(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= _segments.Count)
        {
            return new  CatmullRomCurve();
        }
        
        return _segments[segmentIndex];
    }
}


// a single catmull-rom curve
public struct CatmullRomCurve
{
    public Vector2 p0, p1, p2, p3;
    public float alpha;

    public CatmullRomCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float alpha)
    {
        (this.p0, this.p1, this.p2, this.p3) = (p0, p1, p2, p3);
        this.alpha = alpha;
    }

    private Vector2 GetPoint(float t)
    {
        // calculate knots
        const float k0 = 0;
        float k1 = GetKnotInterval(p0, p1);
        float k2 = GetKnotInterval(p1, p2) + k1;
        float k3 = GetKnotInterval(p2, p3) + k2;

        // evaluate the point
        float u = Mathf.LerpUnclamped(k1, k2, t);
        Vector2 A1 = Remap(k0, k1, p0, p1, u);
        Vector2 A2 = Remap(k1, k2, p1, p2, u);
        Vector2 A3 = Remap(k2, k3, p2, p3, u);
        Vector2 B1 = Remap(k0, k2, A1, A2, u);
        Vector2 B2 = Remap(k1, k3, A2, A3, u);
        return Remap(k1, k2, B1, B2, u);
    }

    public Vector2[] GetEvenlySpacingPoints(float spacing)
    {
        spacing = Mathf.Max(spacing, 0.005f);
        float step = 1 / spacing;
        List<Vector2> points = new List<Vector2>();

        for (float i = 0; i <= 1; i += spacing)
        {
            float t = Mathf.Min(1, i);
            points.Add(GetPoint(t));
        }

        return points.ToArray();
    }

    static Vector2 Remap(float a, float b, Vector2 c, Vector2 d, float u)
    {
        return Vector2.LerpUnclamped(c, d, (u - a) / (b - a));
    }

    float GetKnotInterval(Vector2 a, Vector2 b)
    {
        float sqrMag = Vector2.SqrMagnitude(a - b);
        return Mathf.Max(Mathf.Pow(sqrMag, 0.5f * alpha), 1e-4f); // Avoid zero interval 
    }
}


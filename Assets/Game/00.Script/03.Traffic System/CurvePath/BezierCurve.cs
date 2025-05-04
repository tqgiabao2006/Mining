using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BezierCurve
{
    public Vector2 P0, P1, P2, P3;
    
    public bool IsCurve; //Mesh optimziation: straight mesh need 2 tri, 4 vertices max
    
    public BezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, bool isCurve)
    {
        (this.P0, this.P1, this.P2, this.P3) = (p0, p1, p2, p3);
        this.IsCurve = isCurve;
    }
    public Vector2 GetPoint(float t)
    {
        float omt = (1f - t);
        float omt2 = omt * omt;
        float t2 = t * t;

        return P0 * (omt * omt2) 
               + P1 * (3 * t * omt2) 
               + P2 * (3 * t2 * omt) 
               + P3 * t*t2;
    }

    public Vector2 GetTangent(float t)
    {
        float omt = (1f - t);
        float omt2 = omt * omt;
        float t2 = t * t;
        
        //Tangent = e - d
        Vector2 tangent = P0 * (-omt2)
                + P1 * (3*omt2 - 2 * omt)
                + P2 * (-3 * t2 + 2*t)
                + P3 * (t2);
        
        return tangent.normalized;
    }
    
}

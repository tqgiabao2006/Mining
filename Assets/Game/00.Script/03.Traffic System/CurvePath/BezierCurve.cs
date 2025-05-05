using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public static class BezierCurve
{
    public static Vector2 GetPoint(Vector2 P0, Vector2 P1, Vector2 P2, Vector2 P3,float t)
    {
        float omt = (1f - t);
        float omt2 = omt * omt;
        float t2 = t * t;

        return P0 * (omt * omt2) 
               + P1 * (3 * t * omt2) 
               + P2 * (3 * t2 * omt) 
               + P3 * t*t2;
    }

    public static Vector2 GetTangent(Vector2 P0, Vector2 P1, Vector2 P2, Vector2 P3, float t)
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

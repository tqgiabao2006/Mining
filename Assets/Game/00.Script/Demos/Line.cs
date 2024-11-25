using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;

public class Line
{
    const float verticalLineGradient = 1e5f;
    float gradient;
    float y_intercept;
    float gradientPerpendicular;

    UnityEngine.Vector2 pointOnLine_1;
    UnityEngine.Vector2 pointOnLine_2;
    bool approachSide;

    // Line: y = mx + C

    public Line(UnityEngine.Vector2 pointOnLine, UnityEngine.Vector2 pointPerpendicularToLine)
    {
       float dx = pointOnLine.x - pointPerpendicularToLine.x;
		float dy = pointOnLine.y - pointPerpendicularToLine.y;

		if (dx == 0) {
			gradientPerpendicular = verticalLineGradient;
		} else {
			gradientPerpendicular = dy / dx;
		}

		if (gradientPerpendicular == 0) {
			gradient = verticalLineGradient;
		} else {
			gradient = -1 / gradientPerpendicular;
		}

		y_intercept = pointOnLine.y - gradient * pointOnLine.x;
		pointOnLine_1 = pointOnLine;
		pointOnLine_2 = pointOnLine + new UnityEngine.Vector2 (1, gradient);

		approachSide = false;
		approachSide = GetSide (pointPerpendicularToLine);

    }


    //Whether the point pass the line
    bool GetSide(UnityEngine.Vector2 point)
    {

        //Cross product
        return (point.x - pointOnLine_1.x) * (pointOnLine_2.y - pointOnLine_1.y) > (point.y - pointOnLine_1.y) * (pointOnLine_2.x - pointOnLine_1.x);

        /*
        If the cross product is:

            Positive: The point lies on one side of the line.
            Negative: The point lies on the other side.
            Zero: The point is exactly on the line.


            point.x - pointOnLine_1.x) and (point.y - pointOnLine_1.y) compute the vector from pointOnLine_1 to the point.
            (pointOnLine_2.y - pointOnLine_1.y) and (pointOnLine_2.x - pointOnLine_1.x) compute the vector from pointOnLine_1 to pointOnLine_2.
            The cross product of these vectors determines the orientation of the point relative to the line. If the result is positive, the point is on the positive side of the line; otherwise, it's on the negative side.
        */
    }


    public bool HasCrossedLine(UnityEngine.Vector2 point)
    {
        return GetSide(point) != approachSide;

    }

    public float DistanceFromPoint(UnityEngine.Vector2 point)
    {
        //Calculate the distance (duong vuong goc' voi tiep tuyen )
        /* mecanism:
           Khi vuong goc y1 = y2 => a1.x + b1= a2.x + b2 => x = (b2-b1)/(a1-a2);
        */

        //In Vietnamese, "gradient" in math is called "đạo hàm bậc nhất"
     float yInterceptPerpendicular = point.y - gradientPerpendicular * point.x;
		float intersectX = (yInterceptPerpendicular - y_intercept) / (gradient - gradientPerpendicular);
		float intersectY = gradient * intersectX + y_intercept;
		return UnityEngine.Vector2.Distance (point, new UnityEngine.Vector2 (intersectX, intersectY));

    }

    public void DrawWithGizmos(float length)
    {
        UnityEngine.Vector2 lineDir = new UnityEngine.Vector2(1, gradient).normalized;
        UnityEngine.Vector2 lineCenter = new UnityEngine.Vector2(pointOnLine_1.x, pointOnLine_1.y);
        Gizmos.DrawLine(lineCenter - lineDir * length / 2f, lineCenter + lineDir * length / 2f);
    }





}

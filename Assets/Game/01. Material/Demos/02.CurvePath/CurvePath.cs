// using System.Collections;
// using System.Collections.Generic;
// using Game._00.Script._02.CurvePath;
// using UnityEngine;
//
// [System.Serializable]
// public class CurvePath
// {
//     [SerializeField, HideInInspector]
//     List<Vector2> points;
//
//     [SerializeField, HideInInspector]
//     bool isClosed;
//
//     [SerializeField, HideInInspector]
//     bool autoSetControlPoints; // Changed to plural for consistency
//     
//    
//
//     public CurvePath(Vector2 centre)
//     {
//         // center is in middle of a curve
//         points = new List<Vector2>
//         {
//             centre + Vector2.left, // bottom left
//             centre + (Vector2.left + Vector2.up) * .5f, // up left
//             centre + (Vector2.right + Vector2.down) * .5f, // bottom right
//             centre + Vector2.right // top right
//         };
//     }
//
//     #region Basic Info
//     public Vector2 this[int i]
//     {
//         get
//         {
//             return points[i];
//         }
//     }
//
//     public int NumPoints
//     {
//         get
//         {
//             return points.Count;
//         }
//     }
//
//     public int NumSegments
//     {
//         get
//         {
//             // Each segment just has 3 new points, sharing 1 with a previous segment
//             // If closed segment => points.Count / 3
//             return points.Count / 3;
//         }
//     }
//     // Segment = đoạn đường cong
//
//     public void SplitSegment(Vector2 anchorPos, int segmentIndex)
//     {
//         /*
//          * Index:       0      1      2      3      4      5      6
//            Points: [Anchor0, Control1, Control2, Anchor1, Control3, Control4, Anchor2]
//            Segment:    S0                          S1
//          */
//         //Add control point of new anchor
//         points.InsertRange(segmentIndex * 3 + 2, new Vector2[] { Vector2.zero, anchorPos , Vector2.zero});
//         if (autoSetControlPoints)
//         {
//             AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3); //next anchor point after split
//         }
//         else
//         {
//             AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3); //next anchor point after split
//         }
//
//     }
//
//      public void AddSegment(Vector2 anchorPos)
//     {
//         // AnchorPos is a position we place the anchor
//         points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]); // Add control point
//         points.Add((points[points.Count - 1] + anchorPos) * .5f); // Add mid control point
//         points.Add(anchorPos); // Add anchor position
//
//         if (autoSetControlPoints)
//         {
//             AutoSetAllAffectedControlPoints(points.Count - 1); 
//         }
//     }
//
//     // Each segment requires 4 points
//     public Vector2[] GetPointsInSegment(int i)
//     {
//         return new Vector2[] { points[i * 3], points[i * 3 + 1], points[i * 3 + 2], points[LoopIndex(i * 3 + 3)] };
//     }
//
//     public void MovePoint(int i, Vector2 pos)
//     {
//         Vector2 deltaMove = pos - points[i]; // Change in position
//         points[i] = pos;
//
//         if (autoSetControlPoints)
//         {
//             AutoSetAllAffectedControlPoints(i);
//         }
//         else
//         {
//             if (i % 3 == 0) // Check if it is an anchor point
//             {
//                 if (i + 1 < points.Count || isClosed) // If there is a point immediately following the anchor point
//                 {
//                     points[LoopIndex(i + 1)] += deltaMove; // Move the controller point to the anchor
//                 }
//
//                 if (i - 1 >= 0 || isClosed) // If there is a point immediately before the anchor point
//                 {
//                     points[LoopIndex(i - 1)] += deltaMove; // Move the previous point to the anchor
//                 }
//             }
//             else
//             {
//                 // Get anchor Index
//                 bool nextPointIsAnchor = (i + 1) % 3 == 0;
//                 int correspondingControlIndex = (nextPointIsAnchor) ? i + 2 : i - 2;
//                 int anchorIndex = (nextPointIsAnchor) ? i + 1 : i - 1;
//
//                 if (correspondingControlIndex >= 0 && correspondingControlIndex < points.Count || isClosed) // Avoid out of bound
//                 {
//                     float dst = (points[LoopIndex(anchorIndex)] - points[LoopIndex(correspondingControlIndex)]).magnitude;
//                     Vector2 dir = (points[LoopIndex(anchorIndex)] - pos).normalized;
//                     points[LoopIndex(correspondingControlIndex)] = points[LoopIndex(anchorIndex)] + dir * dst;
//                 }
//             }
//         }
//     }
//     
//
//     #endregion
//
//     #region AutoSetControlPoints
//
//     public bool AutoSetControlPoints
//     {
//         get
//         {
//             return autoSetControlPoints;
//         }
//         set
//         {
//             if (autoSetControlPoints != value)
//             {
//                 autoSetControlPoints = value;
//                 if (autoSetControlPoints)
//                 {
//                     AutoSetAllControlPoints();  
//                 }
//             }
//         }
//     }
//     
//     void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
//     {
//         for (int i = updatedAnchorIndex - 3; i <= updatedAnchorIndex + 3; i += 3)
//         {
//             if (i >= 0 && i < points.Count || isClosed)
//             {
//                 AutoSetAnchorControlPoints(LoopIndex(i));
//             }
//         }
//         AutoSetStartAndEndControls();
//     }
//
//     void AutoSetAllControlPoints()
//     {
//         for (int i = 0; i < points.Count; i += 3) // Loop through all anchor points
//         {
//             AutoSetAnchorControlPoints(i);
//         }
//
//         AutoSetStartAndEndControls(); // Call this method to handle start and end controls
//     }
//
//     void AutoSetAnchorControlPoints(int anchorIndex)
//     {
//         Vector2 anchorPos = points[anchorIndex];
//         Vector2 dir = Vector2.zero;
//         float[] neighbourDistances = new float[2];
//
//         if (anchorIndex - 3 >= 0 || isClosed) // If previous anchor point exists
//         {
//             Vector2 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
//             dir += offset.normalized; // Calculate the vector from this to the previous anchor point (+)
//             neighbourDistances[0] = offset.magnitude; // Store the distance
//         }
//         if (anchorIndex + 3 < points.Count || isClosed) // If next anchor point exists
//         {
//             Vector2 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
//             dir -= offset.normalized; // Calculate the vector from this to the next anchor point (-)
//             neighbourDistances[1] = -offset.magnitude; // Store the distance
//         }
//
//         dir.Normalize(); // Normalize the buildingDirection vector
//
//         for (int i = 0; i < 2; i++)
//         {
//             int controlIndex = anchorIndex + i * 2 - 1; // Calculate control point index
//             if (controlIndex >= 0 && controlIndex < points.Count || isClosed)
//             {
//                 points[LoopIndex(controlIndex)] = anchorPos + dir * neighbourDistances[i] * .5f; // Set control point position
//             }
//         }
//     }
//
//     // Auto set two control points between this anchor point and the next
//     void AutoSetStartAndEndControls()
//     {
//         if (!isClosed)
//         {
//             points[1] = (points[0] + points[2]) * .5f; // Set the first control point
//             points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * .5f; // Set the last control point
//         }
//     }
//
//
//     #endregion
//     
//     #region Close
//
//     public bool IsClosed
//     {
//         get
//         {
//             return isClosed;
//         }
//         set
//         {
//             if (isClosed != value)
//             {
//                 isClosed = value;
//
//                 if (isClosed)
//                 {
//                     // Add last control points
//                     points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
//                     points.Add(points[0] * 2 - points[1]); 
//                     if (autoSetControlPoints)
//                     {
//                         AutoSetAnchorControlPoints(0);
//                         AutoSetAnchorControlPoints(points.Count - 3);
//                     }
//                 }
//                 else // When opened again, remove last 2 points
//                 {
//                     points.RemoveRange(points.Count - 2, 2);
//                     if (autoSetControlPoints)
//                     {
//                         AutoSetStartAndEndControls();
//                     }
//                 }
//                 
//             }
//         }
//     }
//     // Wrap around index to avoid out of bound
//     // Ex count = 7 => if 8 index is 1
//     int LoopIndex(int i)
//     {
//         // Handle negative i value
//         return (i + points.Count) % points.Count;
//     }
//     
//     #endregion
//     
//     #region Delete Point In Segment
//     
//     /* Coore Logic:
//        i = deleted index (i % 3 ==0)
//        default case: i-1, i+1 (2 control points)
//        *first point: i==0
//        if open > delete 0,1,2 
//        if close => p[-1] = p[2] (avoid the control point is index 0) , delete 0,1,2
//       
//       *last anchor:
//       if i == -1 && open => delete i-1, i-2, i
//      */
//     
//     public void DeleteSegment(int anchorIndex)
//     {
//         if (NumSegments >= 2 || !isClosed && NumSegments == 1)
//         {
//             if (anchorIndex == 0) // if anchorIndex = first point 
//             {
//                 if (isClosed)
//                 {
//                     points[points.Count - 1] = points[2];
//                 }
//                 points.RemoveRange(0,3);
//             }else if (anchorIndex == points.Count - 1) // if anchorIndex = last point
//             {
//                 points.RemoveRange(points.Count - 2, 3);
//             }
//             else //default case
//             {
//                 points.RemoveRange(anchorIndex-1,3);
//             
//             }
//             
//         }
//
//         
//         
//     }
//     
//     #endregion
//     
//     #region Evenly Spaced Points
//
//     public Vector2[] CalculateEvenlySpacedPoints(float spacing, float resolution = 1)
//     {
//         List<Vector2> evenlySpacedPoints = new List<Vector2>();
//         evenlySpacedPoints.Add(points[0]);
//         Vector2 previousPoint = points[0];
//         float dstSinceLastEvenPoint = 0;
//
//         for (int segmentIndex = 0; segmentIndex < NumSegments; segmentIndex++)
//         {
//             Vector2[] p = GetPointsInSegment(segmentIndex);
//             int divisions = Mathf.CeilToInt(EstimatedCurveLength(p) * resolution * 10);
//             float t = 0;
//             while (t <= 1)
//             {
//                 t += 1f/divisions;
//                 Vector2 pointOnCurve = Bezier.EvaluateCubic(p[0], p[1], p[2], p[3], t);
//                 dstSinceLastEvenPoint += Vector2.Distance(previousPoint, pointOnCurve);
//
//                 while (dstSinceLastEvenPoint >= spacing)
//                 {
//                     float overshootDst = dstSinceLastEvenPoint - spacing;
//                     Vector2 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDst;
//                     evenlySpacedPoints.Add(newEvenlySpacedPoint);
//                     dstSinceLastEvenPoint = overshootDst;
//                     previousPoint = newEvenlySpacedPoint;
//                 }
//
//                 previousPoint = pointOnCurve;
//             }
//         }
//
//         return evenlySpacedPoints.ToArray();
//     }
//
//     private float EstimatedCurveLength(Vector2[] p)
//     {
//         //Estimated length = distance 2 anchorPoint + 1/2 (controlNetLength)
//         float controlNetLength = Vector2.Distance(p[0], p[1]) + Vector2.Distance(p[1], p[2]) + Vector2.Distance(p[2], p[3]);
//         float estimatedCurveLength = Vector2.Distance(p[0], p[3]) + controlNetLength / 2f;
//         return estimatedCurveLength;
//         
//     }
//     #endregion
//     
//     
// }

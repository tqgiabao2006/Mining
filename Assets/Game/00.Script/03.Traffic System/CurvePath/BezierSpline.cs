using System;
using System.Collections.Generic;
using System.Linq;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Road;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Splines;

namespace Game._00.Script._04.Timer.CurvePath
{
    public class BezierSpline
    {
        private Mesh _mesh;

        private Func<Vector3[], Mesh> _meshCreator;

        private Func<BezierSpline> _createSpline;
        
        private Action<BezierSpline> _updateRoadMesh;

        private Action<Vector3[], Vector3> _createIntersection;

        private float _spacing;

        private int _curveSmoothness;
        
        private List<Vector2> _points;

        private float _alpha;

        private float _radius;

        private float _diameter;

        private float _halfWidth;
        public int NumbSeg
        {
            get
            {
                return _points.Count/3;
            }
        }
        
        public Mesh Mesh
        {
            get
            {
                return _mesh;
            }
        }
        
        public BezierSpline(Func<BezierSpline> createSplineFunc, Func<Vector3[], Mesh> meshCreator,  Action<BezierSpline> updateRoadMesh,Action<Vector3[], Vector3> createIntersection ,float spacing, int curveSmoothness)
        {
            _createIntersection = createIntersection;
            
            _updateRoadMesh = updateRoadMesh;
            
            _createSpline = createSplineFunc;
            
            _meshCreator = meshCreator; 
            
            _spacing = spacing;
            
            _curveSmoothness = curveSmoothness;
            
            _points = new List<Vector2>();
            
            _radius = GridManager.NodeRadius; 
            
            _diameter = GridManager.NodeDiameter;

            _halfWidth = RoadManager.HalfWidth;
        }

        
        /// <summary>
        /// Add raw anchor point, the bezier curve automatically set other control point to create smooth transition amongs line
        /// </summary>
        /// <param name="point">anchor point</param>
        public void AddRawPoint(Vector2 point)
        {
            //Not enough point to form a segment
            if (_points.Count < 1)
            {
                _points.Add(point);
            }//Single segment case
            else if (_points.Count == 1)
            {
                Vector2 forDir =  (point - _points[0]).normalized;
                _points.Add(_points[0] + forDir * _radius);
                _points.Add(point - forDir * _radius);
                _points.Add(point);
            }
            else
            {
                Vector2 forDir = (point - _points[_points.Count - 1]).normalized;
                Vector2 prevDir = (_points[Mathf.Max(_points.Count - 4, 0)] - _points[_points.Count - 1]).normalized; 
                
                //If straight, do not add, just move last two point foward
                if (!IsCurve(_points[_points.Count - 1], _points[_points.Count - 2], point, point))
                {
                     //It last segment is curve, add new instead of expanding the curve
                    if (IsCurve(_points[_points.Count - 1], _points[_points.Count - 2], _points[_points.Count - 3], _points[_points.Count - 4])
                        && !IsCurve(_points[_points.Count-1], _points[_points.Count -2], point))
                    {
                        _points.Add(_points[_points.Count-1] + forDir * _radius);
                        _points.Add(point - forDir * _radius);
                        _points.Add(point);
                    }
                    else
                    {
                        _points[_points.Count - 2] = point - forDir * _radius;
                        _points[_points.Count - 1] = point;
                    }
                }
                else 
                {
                    if (IsCurve(_points[Mathf.Max(_points.Count - 4, 0)], _points[_points.Count - 1], _points[Mathf.Max(_points.Count - 5,1)])   
                        && NumbSeg >= 3
                        && !IsPerpendicular(_points[_points.Count - 1], _points[Mathf.Max(_points.Count - 3,1)], _points[Mathf.Max(_points.Count - 4,0)])) //Check if U-turn connection, anchor - middle control point - anchor
                        
                        // -5 is the index of first control point of 2rd, because when straight to curve,
                        // the 3rd anchor point is set back so can not use it to change if curve
                    {
                        //Vector tangent blend between prev and new point, between calculate vector orthogonal
                        //Change tangent in, and out for last anchor point
                        //Lower the range of radius
                        
                        float halfRadius = _radius * 0.5f;
                        _points[_points.Count - 2] = _points[_points.Count - 1] + (prevDir - forDir) * halfRadius;
                        _points.Add(_points[_points.Count - 1] + (forDir - prevDir) * halfRadius);

                        Vector2 controlPoint = point - forDir * halfRadius;
                        _points.Add(controlPoint);
                        _points.Add(point);

                    }//Check if U-turn connection, anchor - middle control point - anchor
                    else if(IsPerpendicular(_points[_points.Count - 1], _points[Mathf.Max(_points.Count - 3,1)],  _points[Mathf.Max(_points.Count - 4,0)]))
                    {
                        Debug.Log("Is Per");
                        
                        //Move last point backward
                        Vector2 backDir = (_points[_points.Count - 2] - _points[_points.Count - 1]).normalized;
                        
                        _points[_points.Count - 1] += backDir * _radius;
                      
                        Vector2 mid = _points[_points.Count - 1] - backDir * GridManager.NodeRadius;
                        _points.Add(mid);
                        _points.Add(point + (mid -point).normalized * GridManager.NodeRadius);
                        _points.Add(point);

                    }else //Straight to perpendicular case
                    {
                        Vector2 mid = _points[_points.Count - 1];
                                                
                        _points[_points.Count - 1] += prevDir * _radius;
                        _points[_points.Count - 2] += prevDir * _radius;
                                            
                        //Add new control point for last anchor point
                        _points.Add(_points[_points.Count - 1] + (mid - _points[_points.Count - 1]).normalized  * _radius);
                        
                        //Add new control point for added anchor point
                        Vector2 controlPoint = point + (mid - point).normalized * _radius;
                        _points.Add(controlPoint);
                                            
                        //Add new anchor point
                        _points.Add(point);
                    }
                }
                
            }
            UpdateMesh();
        }

        /// <summary>
        /// Add the point already calculated, normally a point in bezier curve   
        /// </summary>
        /// <param name="point">point already in bezier curve</param>
        public void AddPreCalculatedPoint(Vector2 point)
        {
            _points.Add(point);
        }

        private void UpdateMesh()
        {
            if (_meshCreator == null)
            {
                return;
            }
            
            if (NumbSeg == 0)
            {
                _mesh = new Mesh();
                return;
            } 
            
            _mesh =  _meshCreator.Invoke(GetEvenlySpacedPoints(_spacing, _curveSmoothness));
            _updateRoadMesh(this);
        }
        
        /// <summary>
        /// Get evenly spaced poings, with optimization, repersenting straight line by only 1 point
        /// </summary>
        /// <param name="spacing"></param>
        /// <param name="curveSmoothness"></param>
        /// <returns></returns>
        public Vector3[] GetEvenlySpacedPoints(float spacing, float curveSmoothness)
        {
            spacing = Mathf.Max(spacing, 0.005f);
            spacing = Mathf.Min(spacing, 1f);
            List<Vector3> evenlySpacedPoints = new List<Vector3>();
            
            for(int i = 0 ; i < _points.Count -3 ; i += 3)
            {
                evenlySpacedPoints.Add(_points[i]);

                Vector2 previousPoint = _points[i];
                float distanceSinceLastPoint = 0f;
                
                if (IsCurve(_points[i], _points[i + 1], _points[i + 2], _points[i+3]))
                {
                    for (int j = 1; j <= curveSmoothness; j++)
                    {
                        float t = j / (float)curveSmoothness;
                        Vector2 currentSample = BezierCurve.GetPoint(_points[i], _points[i+1], _points[i+2], _points[i+3],t);
                        float distance = Vector2.Distance(previousPoint, currentSample);
                        
                        if (distanceSinceLastPoint + distance >= spacing)
                        {
                            float overshoot = spacing - distanceSinceLastPoint;
                            Vector2 newPoint = Vector2.Lerp(previousPoint, currentSample, overshoot / distance);
                            evenlySpacedPoints.Add(newPoint);

                            distanceSinceLastPoint = 0f;
                            previousPoint = newPoint;
                            j--;
                        }
                        else
                        {
                            distanceSinceLastPoint += distance;
                            previousPoint = currentSample;
                        }

                    }
                }
                else
                {
                    //Only have to add end point for straight line
                    evenlySpacedPoints.Add(BezierCurve.GetPoint(_points[i], _points[i+1], _points[i+2], _points[i+3],1));
                }
            }

            return evenlySpacedPoints.ToArray();
        }
        public void Pop()
        {
            if (!IsCurve(_points[_points.Count - 1], _points[_points.Count - 2], _points[_points.Count - 3], _points[_points.Count - 4]))
            {
                Vector2 prevDir = (_points[_points.Count - 4] - _points[_points.Count - 1]).normalized;
                _points[_points.Count - 2] += prevDir * _diameter;
                _points[_points.Count - 1] += prevDir * _diameter;

                if (Vector2.Distance(_points[_points.Count - 1], _points[_points.Count - 4]) <= 0.005f)
                {
                    _points.RemoveRange(_points.Count - 3, 3);
                }

                if (_points.Count == 1)
                {
                    _points.Clear();
                }
            }
            else
            {
                _points.RemoveRange(_points.Count-3, 3); 
                
                //If straight to curve deletion, delete current, and move part forward
                if(!IsCurve(_points[_points.Count-1], _points[_points.Count -2], _points[_points.Count - 3]))
                {
                    Vector2 prevDir = (_points[_points.Count - 4] -  _points[_points.Count - 1]).normalized;
                                    
                    //Move forward the straight line curve
                    _points[_points.Count - 1] -= prevDir * _diameter;
                    _points[_points.Count - 2] -= prevDir * _diameter;
                }
            }
            UpdateMesh();
        }

        /// <summary>
        /// Get point in segment
        /// </summary>
        /// <param name="segmentIndex">segment index</param>
        /// <returns></returns>
        public Vector2[] GetPointInSegment(int segmentIndex)
        {
            if (segmentIndex< 0 || segmentIndex >= NumbSeg)
            {
                return new Vector2[]{};
            }
            return new[] {_points[segmentIndex*3], _points[segmentIndex*3+1], _points[segmentIndex*3+2], _points[segmentIndex*3+3]};
        }

        public void SplitSegment(Node intersection)
        {
            Debug.Log("Split ");
            
            Vector2 intersecPos = intersection.WorldPosition;
            int ctrlCnt = 0;
            int anchorCnt = 0;
            
            Vector2 lastDir = (_points[_points.Count - 2] - _points[_points.Count - 1]).normalized;
            
            Vector2 lastPoint = _points[_points.Count - 1] + lastDir * _radius;
            _points[_points.Count - 1] = lastPoint;
            _points[_points.Count - 2] += lastDir * _radius;
            
            Debug.Log("Last point " + lastPoint);
            Debug.Log("last dir " + lastDir);

            for (int i = 0; i < _points.Count; i++)
            {
                if (IsInSide(_points[i], intersection.WorldPosition.x, intersection.WorldPosition.y,
                        GridManager.NodeRadius, GridManager.NodeRadius))
                {
                    if (i % 3 == 0)
                    {
                        anchorCnt++;
                    }
                    else
                    {
                        ctrlCnt++;
                    }
                }
            }

            Debug.Log("anchor cnt " + anchorCnt);
            Debug.Log("control cnt " +  ctrlCnt);
            for (int i = 0; i < NumbSeg; i ++)
            {
                Vector2 p0 = _points[i*3];
                Vector2 p1 = _points[i*3+1];
                Vector2 p2 = _points[i*3+2];
                Vector2 p3 = _points[i*3+3];

                for (float t = 0; t <= 1; t += 0.05f)
                {
                    Vector2 p = BezierCurve.GetPoint(p0, p1, p2, p3, t);

                    if (IsInSide(p, intersection.WorldPosition.x, intersection.WorldPosition.y, GridManager.NodeRadius, GridManager.NodeRadius)
                        && Vector2.Distance(p, intersecPos) < GridManager.NodeRadius)
                    {
                        if (anchorCnt == 0)
                        {
                            Vector2 prev = (p0-p1).normalized;
                            Vector2 next = (p3-p2).normalized;

                            if (IsCurve(p0,p1,p2,p3) && ctrlCnt == 1)
                            {
                                next = (p2-p1).normalized;
                            }
                            
                            Debug.Log("Prev " + prev);
                            Debug.Log("Next " + next);
                        
                            //Last index of an original segment
                            Vector2 lastAnchorPoint = intersecPos + prev * _radius;
                            Vector2 lastControlPoint = intersecPos + prev * (_radius * 1.5f);
                        
                            Vector2 newAnchorPoint = intersecPos + next * _radius;
                            Vector2 newControlPoint = intersecPos + next * (_radius * 1.5f);
                            
                            BezierSpline newSpline = _createSpline();
                            
                            Debug.Log("last anchor point "  + lastAnchorPoint);
                            Debug.Log("Last control point "  + lastControlPoint);
                            
                            Debug.Log("new anchor point " + newAnchorPoint);
                            Debug.Log("new control point " + newControlPoint);
                            
                            int startIndex = i * 3 + 2;
                            if (ctrlCnt == 1)
                            {
                                startIndex++;
                            } 
                            if (Vector2.Distance(newAnchorPoint, p3) < 0.05f)
                            {
                                startIndex += 2;
                               newControlPoint += next * (_radius * 0.5f);
                            }
                            
                            //Check if next segment is perpendicular
                            if (i + 1 < NumbSeg)
                            {
                                Vector2[] nextSegmentPoint = GetPointInSegment(i + 1);
                                if (IsPerpendicular((nextSegmentPoint[0] - nextSegmentPoint[1]).normalized,
                                        (nextSegmentPoint[3] - nextSegmentPoint[2]).normalized))
                                {
                                    _points[i * 3 + 5] = _points[i * 3 + 4];
                                }
                            }
                            
                            newSpline.AddPreCalculatedPoint(newAnchorPoint);
                            newSpline.AddPreCalculatedPoint(newControlPoint);
                            
                            for (int j = startIndex; j < _points.Count; j++)
                            {
                                newSpline.AddPreCalculatedPoint(_points[j]);
                            }
                            _points.RemoveRange(i * 3 + 2, _points.Count - (i*3+2));
                            
                            this.AddPreCalculatedPoint(lastControlPoint);
                            this.AddPreCalculatedPoint(lastAnchorPoint);

                            newSpline.UpdateMesh();
                            this.UpdateMesh();
                            
                            Vector3[] points = new Vector3[]
                            {
                                //Set back a bit to avoid perfect pixel mesh
                                (lastAnchorPoint + prev * 0.1f) + new Vector2(-prev.y, prev.x) * _halfWidth,
                                (lastAnchorPoint + prev * 0.1f)- new Vector2(-prev.y, prev.x) * _halfWidth,
                                 (lastPoint + lastDir * 0.1f) + new Vector2(-lastDir.y, lastDir.x) * _halfWidth,
                                 (lastPoint + lastDir * 0.1f)- new Vector2(-lastDir.y, lastDir.x) * _halfWidth,
                                 (newAnchorPoint + next *0.1f) + new Vector2(-next.y, next.x) * _halfWidth,
                                 (newAnchorPoint + next *0.1f)- new Vector2(-next.y, next.x) * _halfWidth,
                            };
                            
                            _createIntersection(ArrangeCornerPoints(points), intersecPos);
                        }
                        else
                        {
                            if (i*3+4 >= _points.Count)
                                return;
                            
                            Vector2 prev = (p2-p3).normalized; 
                            Vector2 next = (_points[i*3+4] - p3).normalized;
                            
                            Debug.Log("Prev =" + p3 + " - " + p2);
                            Debug.Log("Next " + _points[i*3+4] + " - " + p3);  
                            
                            Vector2 lastAnchorPoint = intersecPos + prev * _radius;
                            Vector2 lastControlPoint = intersecPos + prev * _radius * 1.5f;
                        
                            Vector2 newAnchorPoint = intersecPos + next * _radius;
                            Vector2 newControlPoint = intersecPos + next * _radius * 1.5f;
                            
                            
                            BezierSpline newSpline = _createSpline();
                            
                            newSpline.AddPreCalculatedPoint(newAnchorPoint);
                            newSpline.AddPreCalculatedPoint(newControlPoint);

                            for (int j = i * 3 + 5; j < _points.Count; j++)
                            {
                                newSpline.AddPreCalculatedPoint(_points[j]);
                            }
                            
                            Debug.Log("i*3 + 6 " + _points[i*3+6]);
                            
                            _points.RemoveRange(i*3+2, _points.Count - (i*3+2));
                            _points.Add(lastControlPoint);
                            _points.Add(lastAnchorPoint);
                            
                            newSpline.UpdateMesh();
                            this.UpdateMesh();
                            
                            Vector3[] points = new Vector3[]
                            {
                                lastAnchorPoint + new Vector2(-prev.y, prev.x) * _halfWidth,
                                lastAnchorPoint - new Vector2(-prev.y, prev.x) * _halfWidth,
                                lastPoint + new Vector2(-lastDir.y, lastDir.x) * _halfWidth,
                                lastPoint - new Vector2(-lastDir.y, lastDir.x) * _halfWidth,
                                newAnchorPoint + new Vector2(-next.y, next.x) * _halfWidth,
                                newAnchorPoint - new Vector2(-next.y, next.x) * _halfWidth,
                            };
                            
                            _createIntersection(ArrangeCornerPoints(points), intersecPos);

                        }

                        return;
                    }
                }
            }


            ///Require 2 points in pair index
            Vector3[] ArrangeCornerPoints(Vector3[] points)
            {
                if (points.Length < 4)
                {
                    return points;
                }
                
                Vector3[] arrangedPoints = new Vector3 [points.Length];
                
                float d1 = Vector2.SqrMagnitude(points[0] - points[2]);
                float d2 = Vector2.SqrMagnitude(points[0] - points[3]);
                
                float  d3 = Vector2.SqrMagnitude(points[1] - points[2]);
                float d4 = Vector2.SqrMagnitude(points[1] - points[3]);

                if (d1 + d2 > d3 + d4)
                {
                    arrangedPoints[0] = points[0];
                    arrangedPoints[1] = points[1];

                    if (d3 < d4)
                    {
                        arrangedPoints[2] = points[2];
                        arrangedPoints[3] = points[3];
                    }
                    else
                    {
                        arrangedPoints[2] = points[3];
                        arrangedPoints[3] = points[2];
                    }
                }
                else
                {
                    arrangedPoints[0] = points[1];
                    arrangedPoints[1] = points[0];

                    if (d1 < d2)
                    {
                        arrangedPoints[2] = points[2];
                        arrangedPoints[3] = points[3];
                    }
                    else
                    {
                        arrangedPoints[2] = points[3];
                        arrangedPoints[3] = points[2];
                    }
                }

                for (int i = 3; i < points.Length - 2; i += 2)
                {
                    if (Vector2.SqrMagnitude(arrangedPoints[i] - points[i + 1]) <
                        Vector2.SqrMagnitude(arrangedPoints[i] - points[i + 2]))
                    {
                        arrangedPoints[i+1] = points[i+1];
                        arrangedPoints[i+2] = points[i + 2];
                    }
                    else
                    {
                        arrangedPoints[i+1] = points[i+2];
                        arrangedPoints[i+2] = points[i +1];
                    }
                }
                return arrangedPoints;
            }
        }

        /// <summary>
        /// Return if a point inside a rectangle
        /// </summary>
        /// <param name="checkPos">check position</param>
        /// <param name="centerX">position x of center</param>
        /// <param name="centerY">position y of center</param>
        /// <param name="width">rectangle's width</param>
        /// <param name="height">rectangle's height</param>
        /// <returns></returns>
        private bool IsInSide(Vector2 checkPos, float centerX, float centerY, float width, float height)
        {
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            
            return checkPos.x >= centerX - halfWidth && checkPos.x <= centerX + halfWidth && checkPos.y >= centerY - halfHeight && checkPos.y <= centerY + halfHeight;
        }
        /// <summary>
        /// Check if a point is straight line, or curve
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        private bool IsCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 dir = p3 - p0;
    
            // Check if both control points lie on the line formed by p0->p3
            float cross1 = Cross(dir.normalized, p1 - p0);
            float cross2 = Cross(dir.normalized, p2 - p0);

            return !(Mathf.Approximately(cross1, 0f) && Mathf.Approximately(cross2, 0f));
        }

        
        /// <summary>
        /// Check if a point is straight
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        private bool IsCurve(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 dir = p3 - p1;
            float cross1 = Cross(dir.normalized, p2 - p1);
            float cross2 = Cross(dir.normalized, p3 - p1);
            
            return !(Mathf.Approximately(cross1, 0f) && Mathf.Approximately(cross2, 0f));
        }

        /// <summary>
        /// Check if 2 direction vector is perpendicular
        /// </summary>
        /// <param name="dir1"></param>
        /// <param name="dir2"></param>
        /// <returns></returns>
        private bool IsPerpendicular(Vector2 dir1, Vector2 dir2)
        {
            return Mathf.Approximately(Vector2.Dot(dir2, dir1), 0);
        }

        private bool IsPerpendicular(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 dir1 =  (p2 - p1).normalized;
            Vector2 dir2 =  (p3 - p2).normalized;
            return IsPerpendicular(dir1, dir2);
        }
        
        private float Cross(Vector2 dir1, Vector2 dir2)
        {
            return dir1.x * dir2.y - dir1.y * dir2.x;
        }
    }
}
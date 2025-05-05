using System;
using System.Collections.Generic;
using System.Linq;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Road;
using NUnit.Framework;
using UnityEngine;

namespace Game._00.Script._04.Timer.CurvePath
{
    public class BezierSpline
    {
        private Mesh _mesh;

        private Func<Vector2[], Mesh> _meshCreator;

        private float _spacing;

        private int _curveSmoothness;
        
        private List<Vector2> _points;

        public List<Vector2> Points
        {
            get
            {
                return _points; 
            }
        }

        private float _alpha;

        private float _radius;

        private float _diameter;
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
        
        public BezierSpline(float alpha, Func<Vector2[], Mesh> meshCreator, float spacing, int curveSmoothness)
        {
            _meshCreator = meshCreator; 
            
            _spacing = spacing;
            
            _curveSmoothness = curveSmoothness;
            
            _points = new List<Vector2>();
            
            _radius = GridManager.NodeRadius; 
            
            _diameter = GridManager.NodeDiameter;
        }

        public void AddPoint(Vector2 point)
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
                        Debug.Log("New straight point");
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
                        && NumbSeg >= 3)
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

                    }
                    else
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

        private void UpdateMesh()
        {
            if (NumbSeg == 0)
            {
                _mesh = new Mesh();
                return;
            } 
            
            _mesh =  _meshCreator.Invoke(GetEvenlySpacedPoints(_spacing, _curveSmoothness));
        }

      
        private Vector2[] GetEvenlySpacedPoints(float spacing, int curveSmooth)
        {
            spacing = Mathf.Max(spacing, 0.005f);
            spacing = Mathf.Min(spacing, 1f);
            List<Vector2> evenlySpacedPoints = new List<Vector2>();
            
            for(int i = 0 ; i < _points.Count -3 ; i += 3)
            {
                evenlySpacedPoints.Add(_points[i]);

                Vector2 previousPoint = _points[i];
                float distanceSinceLastPoint = 0f;
                
                // if (IsCurve(_points[i], _points[i + 1], _points[i + 2], _points[i+3]))
                // {
                    for (int j = 1; j <= curveSmooth; j++)
                    {
                        float t = j / (float)curveSmooth;
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
                // }
                // else
                // {
                //     //Only have to add end point for straight line
                //     evenlySpacedPoints.Add(BezierCurve.GetPoint(_points[i], _points[i+1], _points[i+2], _points[i+3],1));
                // }
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

        public Vector2[] GetPointInSegment(int i)
        {
            if (i< 0 || i > NumbSeg)
            {
                return new Vector2[]{};
            }
            return new[] {_points[i], _points[i+1], _points[i+2], _points[i+3]};
        }
        private bool IsCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 dir = p3 - p0;
    
            // Check if both control points lie on the line formed by p0->p3
            float cross1 = Cross(dir.normalized, p1 - p0);
            float cross2 = Cross(dir.normalized, p2 - p0);

            return !(Mathf.Approximately(cross1, 0f) && Mathf.Approximately(cross2, 0f));
        }

        private bool IsCurve(Vector2 anchor1, Vector2 anchor2, Vector2 anchor3)
        {
            Vector2 dir = anchor3 - anchor1;
            float cross1 = Cross(dir.normalized, anchor2 - anchor1);
            float cross2 = Cross(dir.normalized, anchor3 - anchor1);
            
            return !(Mathf.Approximately(cross1, 0f) && Mathf.Approximately(cross2, 0f));
        }
        
        private float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}
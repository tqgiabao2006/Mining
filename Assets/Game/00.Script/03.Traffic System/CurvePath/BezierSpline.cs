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
        
        private List<BezierCurve> _segments;

        private List<Vector2> _points;

        private float _alpha;

        private float _nodeDiamater;

        public int PointCount
        {
            get
            {
                return _points.Count; 
            }
        }
        
        public int NumbSeg
        {
            get
            {
                return _segments.Count;
            }
        }

        public BezierCurve this[int index]
        {
            get
            {
                return _segments[index];
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
            
            _segments = new List<BezierCurve>();
        
            _points = new List<Vector2>();
            
            _nodeDiamater = GridManager.NodeDiameter;
        }

        public void AddPoint(Vector2 point)
        {
            //Not enough point to form a segment
            if (_points.Count < 2)
            {
                _points.Add(point);
            }
            
            if (_points.Count == 2) //Single segment
            {
                Vector2 dir =  (point - _points[0]).normalized;
                Vector2 p1 = _points[0] + dir * _nodeDiamater;
                Vector2 p2 = point - dir * _nodeDiamater;
                _segments.Add(new BezierCurve(_points[0],p1,p2,point, false));
                
                _points.RemoveAt(_points.Count - 1);
                _points.Add(p1);
                _points.Add(p2);
                _points.Add(point);
            }
            else if(_points.Count > 2)
            {
                Vector2 dir =  (point - _points[_points.Count-1]).normalized;
                Vector2 prevDir = (_points[_points.Count - 1] - _points[Mathf.Max(0, _points.Count - 3)]).normalized;
                
                float cross = Cross(dir,prevDir);
                
                //If point same direction, not add, just move the last anchor point
                if (Mathf.Approximately(cross, 0f) || Mathf.Approximately(cross, 180f))
                {
                    Debug.Log("Straight");
                    Vector2 p2 = point - dir * _nodeDiamater;

                    if (!_segments[_segments.Count - 1].IsCurve)
                    {
                        _segments[_segments.Count - 1] = new BezierCurve(_segments[_segments.Count - 1].P0, _segments[_segments.Count - 1].P1, 
                            p2, point, false);
                    }
                    else
                    {
                        _segments.Add(new BezierCurve(_points[_points.Count - 1], _points[_points.Count - 1], point, point, false));
                    }
                    
                    _points[_points.Count-2] = p2;
                    _points[_points.Count-1] =  point;
                }
                else //If curved, delete the middle point, avoid sharp shape, create new bezier curve for the curve
                {
                    Debug.Log("Curve");
                    
                    //Delete middle point between 2 curve
                    Vector2 connected = _points[_points.Count - 1];
                    
                    //Update to previous
                    //Set back the last node to make room for a curve
                    _segments[_segments.Count - 1] = new BezierCurve(_segments[_segments.Count - 1].P0, _segments[_segments.Count - 1].P1, 
                        _points[_points.Count -1] - prevDir * _nodeDiamater, _points[_points.Count - 1] - prevDir * _nodeDiamater, false);
                    
                    //Create curve bezier
                    //Set back the last node to create smoother curve
                    _segments.Add(new BezierCurve(_points[_points.Count -1] - prevDir * _nodeDiamater, connected, connected, point, true));

                    _points.Add(connected);
                    _points.Add(connected);
                    _points.Add(point);
                    
                }
            }
            
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            if (_segments.Count == 0)
            {
                return;
            }
            Debug.Log(_segments.Count);
            _mesh =  _meshCreator.Invoke(GetEvenlySpacedPoints(_spacing, _curveSmoothness));
        }

      
        private Vector2[] GetEvenlySpacedPoints(float spacing, int curveSmooth)
        {
            spacing = Mathf.Max(spacing, 0.005f);
            spacing = Mathf.Min(spacing, 1f);
            List<Vector2> evenlySpacedPoints = new List<Vector2>();
            
            foreach (BezierCurve segment in _segments)
            {
                evenlySpacedPoints.Add(segment.GetPoint(0));

                Vector2 previousPoint = segment.GetPoint(0);
                float distanceSinceLastPoint = 0f;
                
                if (segment.IsCurve)
                {
                    Vector2 lastSample = segment.GetPoint(0);

                    for (int i = 1; i <= curveSmooth; i++)
                    {
                        float t = i / (float)curveSmooth;
                        Vector2 currentSample = segment.GetPoint(t);
                        float distance = Vector2.Distance(previousPoint, currentSample);

                        if (distanceSinceLastPoint + distance >= spacing)
                        {
                            float overshoot = spacing - distanceSinceLastPoint;
                            Vector2 newPoint = Vector2.Lerp(previousPoint, currentSample, overshoot / distance);
                            evenlySpacedPoints.Add(newPoint);

                            distanceSinceLastPoint = 0f;
                            previousPoint = newPoint;
                            i--;
                        }
                        else
                        {
                            distanceSinceLastPoint += distance;
                            previousPoint = currentSample;
                        }

                        lastSample = currentSample;
                    }
                }
                else
                {
                    //Only have to add end point for straight line
                    evenlySpacedPoints.Add(segment.GetPoint(1));
                }
            }

            return evenlySpacedPoints.ToArray();
        }


        public BezierCurve GetCurve(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _segments.Count)
            {
                return new BezierCurve();
            }
        
            return _segments[segmentIndex];
        }

        //Debug-only
        public List<Vector2> GetPoints()
        {
            return _points;
        }

        public void Pop()
        {
            _segments.RemoveAt(_segments.Count - 1);
            _points.RemoveAt(_points.Count - 1);
        }

        private float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}
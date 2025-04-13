using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Custom_Editor;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.CurvePath
{
    [System.Serializable]
    public class CurvePath
    {
       [SerializeField, HideInInspector] private List<Vector2> _points;
       [SerializeField, HideInInspector] private bool _isClosed;
       [SerializeField, HideInInspector] private bool _autoSet;
        public int NumbSegs
        {
            get
            {
                return _points.Count / 3;
            }
        }

        public int NumbPoints
        {
            get
            {
                return _points.Count;
            }
        }

        public bool AutoSet
        {
            get
            {
                return _autoSet;
            }
            set
            {
                if (value != _autoSet)
                {
                    _autoSet = value;
                    SetAllControlPoints();
                }
            }
        }

        public bool IsClosed
        {
            get
            {
                return _isClosed;
            }
            set
            {
                if (value != _isClosed)
                {
                    _isClosed = value;
                    if (_isClosed)
                    { 
                        _points.Add(_points[_points.Count - 1] * 2 - _points[_points.Count - 2]);
                        _points.Add(_points[0] * 2 - _points[1]);

                        if (_autoSet)
                        {
                            SetControlPoints(0);
                            SetAffectedControlPoints(_points.Count - 3);
                        }
                    }
                    else
                    {
                        _points.RemoveRange(_points.Count - 2, 2);

                        if (_autoSet)
                        {
                            SetStartEndControl();
                        }
                    }
                }
            }
        }

        public Vector2 this[int index]
        {
            get
            {
                return _points[index];
            }
        }
    
        public CurvePath(Vector2 center)
        {
            _points = new List<Vector2>()
            {
                center + Vector2.left,
                center + Vector2.left / 2f + Vector2.up,
                center + Vector2.right / 2f + Vector2.down,
                center + Vector2.right,
            };
        }
    
        public void AddSegment(Vector2 anchorPoint)
        {
            _points.Add(_points[_points.Count - 1] * 2 - _points[_points.Count - 2]);
            _points.Add((anchorPoint +  _points[_points.Count - 1])  * 0.5f);
            _points.Add(anchorPoint);

            if (_autoSet)
            {
                SetAffectedControlPoints(_points.Count - 1);
            }
        }

        public void SplitSegment(Vector2 anchorPos, int segmentIndex)
        {
            Debug.Log(segmentIndex);
            _points.InsertRange(segmentIndex * 3 + 2, new [] {Vector2.zero, anchorPos,  Vector2.zero});
            if (_autoSet)
            {
                SetAffectedControlPoints(segmentIndex * 3 + 3);
            }
            else
            {
                SetControlPoints(segmentIndex * 3 + 3);
            }
        }

        public Vector2[] GetPointOnSegment(int i)
        {
            return new Vector2 []
            {
                _points[i * 3], _points[i * 3 + 1], _points[i * 3 + 2], _points[WrapIndex(i * 3 + 3)]
            };
        }

        public Vector2[] GetEvenlyPoint(float spacing, float resolution = 1)
        {
            List<Vector2> evenPoints = new List<Vector2>();
            evenPoints.Add(_points[0]);

            float prevDst = 0;
            Vector2 prevPoint = _points[0];

            for (int i = 0; i < NumbSegs; i++)
            {
                Vector2[] points = GetPointOnSegment(i);
                
                float netControlLength = Vector2.Distance(points[0], points[1]) +  Vector2.Distance(points[1], points[2]) + Vector2.Distance(points[2], points[3]);
                float curveLength = Vector2.Distance(points[0], points[3]) + netControlLength * 0.5f; //Estimated only
                int divisions = Mathf.CeilToInt(curveLength * resolution * 10);
                
                float t = 0;
                while (t < 1)
                {
                    t += .1f/divisions;
                    Vector2 pointOnCurve = Bezier.Cubic(points[0], points[1], points[2], points[3], t);
                    prevDst += Vector2.Distance(prevPoint, pointOnCurve);

                    //Overshoot
                    while (prevDst >= spacing)
                    {
                        float overDst = prevDst - spacing;
                        Vector2 newEvenPoint = pointOnCurve + (prevPoint - pointOnCurve).normalized * overDst;
                        evenPoints.Add(newEvenPoint);

                        prevDst = overDst;
                    }

                    prevPoint = pointOnCurve;
                }
            }

            return evenPoints.ToArray();
        }


        public void MovePoint(int i, Vector2 pos)
        {
            Vector2 deltaMove = pos - _points[i];

            if (i % 3 == 0 || !_autoSet) {
                _points[i] = pos;

                if (_autoSet)
                {
                    SetAffectedControlPoints(i);
                }
                else
                {
                    if (i % 3 == 0)
                    {
                        if (i + 1 < _points.Count || _isClosed)
                        {
                            _points[WrapIndex(i + 1)] += deltaMove;
                        }
                        if (i - 1 >= 0 || _isClosed)
                        {
                            _points[WrapIndex(i - 1)] += deltaMove;
                        }
                    }
                    else
                    {
                        bool nextPointIsAnchor = (i + 1) % 3 == 0;
                        int correspondingControlIndex = (nextPointIsAnchor) ? i + 2 : i - 2;
                        int anchorIndex = (nextPointIsAnchor) ? i + 1 : i - 1;

                        if (correspondingControlIndex >= 0 && correspondingControlIndex < _points.Count || _isClosed)
                        {
                            float dst = (_points[WrapIndex(anchorIndex)] - _points[WrapIndex(correspondingControlIndex)]).magnitude;
                            Vector2 dir = (_points[WrapIndex(anchorIndex)] - pos).normalized;
                            _points[WrapIndex(correspondingControlIndex)] = _points[WrapIndex(anchorIndex)] + dir * dst;
                        }
                    }
                }
            }
        }

        private int WrapIndex(int i)
        {
            return (i + _points.Count) % _points.Count;
        }

        public void SetAffectedControlPoints(int anchorIndex)
        {
            for (int i = anchorIndex - 3; i < anchorIndex + 3; i += 3)
            {
                if (i >= 0 && i < _points.Count || _isClosed)
                {
                    SetControlPoints(WrapIndex(i));
                }
            }
        }

        public void SetAllControlPoints()
        {
            for (int i = 0; i < _points.Count; i += 3)
            {
                SetControlPoints(i);
            }
            SetStartEndControl();
        }

        /// <summary>
        /// Calculate the angle between 2 left, and right anchor point
        /// Bisect the angle, get the perpendicular line of it
        /// Move the control points to on that line, with half distance from other anchor point
        /// Here, find perpendicular line by subtract vector one of other
        /// </summary>
        /// <param name="anchorIndex"></param>
        public void SetControlPoints(int anchorIndex)
        {
            Vector2 anchorPos  = _points[anchorIndex];

            if (anchorIndex + 3 < _points.Count && anchorIndex - 3 >= 0 || _isClosed)
            {
                int anchorIndex1 = WrapIndex(anchorIndex + 3);
                int anchorIndex2 = WrapIndex(anchorIndex - 3);
                
                Vector2 control1 = _points[anchorIndex1];
                Vector2 control2 =  _points[anchorIndex2];

                Vector2 v1 = (control1 - anchorPos).normalized;
                Vector2 v2 = (control2 - anchorPos).normalized;

                float dst1 = v1.magnitude;
                float dst2 = v2.magnitude;

                _points[WrapIndex(anchorIndex + 1)] = anchorPos +  (v1 - v2) * (dst1 * 0.5f);
                _points[WrapIndex(anchorIndex - 1)] = anchorPos +  (v2 - v1) * (dst2 * 0.5f);
            }
        }

        /// <summary>
        /// If !isClosed => the control point of end/start anchor point (only 1 control point each)
        /// set to be midpoint of the closest control point to start/end anchor point
        /// </summary>
        public void SetStartEndControl()
        {
            if (!_isClosed)
            {
                _points[1] = (_points[0] + _points[2]) * 0.5f;
                _points[_points.Count - 2] = (_points[_points.Count - 1] + _points[_points.Count - 3]) * 0.5f;
            }
            
        }

        public void DeletePoint(int anchorIndex)
        {
            //Only alow when numb seg > 2
            if (NumbSegs > 2 || !_isClosed && NumbSegs > 1)
            {
                if (anchorIndex == 0)
                {
                    if (_isClosed)
                    {
                        _points[WrapIndex(anchorIndex - 1)] = _points[2];
                    }
                           
                    _points.RemoveRange(anchorIndex,3);
                }else if (anchorIndex == _points.Count - 1 && !_isClosed)
                {
                    _points.RemoveRange(anchorIndex-2, 3);
                }
                else
                {
                    _points.RemoveRange(anchorIndex-1, 3);
                }     
            }
        }
    }
}


using System;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Road;
using Game._00.Script._04.Timer.CurvePath;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.CurvePath
{
    public class Intersection
    {
        [Tooltip("The smaller, the closer the smooth to intersection")]
        [Range(0.25f ,1)]
        private float _checkRadius;

        private Func<Vector3[], Vector3, Mesh> _createCornerMeshFunc;
        
        public Intersection(BezierSpline[] splines, Func<Vector3[], Vector3, Mesh> createCornerMeshFunc)
        {
            _checkRadius = GridManager.NodeRadius;
            
            _createCornerMeshFunc = createCornerMeshFunc;
        }
    }
}
using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager;
using Game._00.Script._03.Traffic_System.Road;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game._00.Script._03.Traffic_System.PathFinding
{
    #if UNITY_EDITOR
    public struct PathDebugData
    {
        public List<Vector3> Waypoints;
        public List<Vector3> OriginalPaths;
    }
    #endif
    

    /// <summary>
    /// Working as a bridge from pathfinding, and unit base to try create new thread => optimize, decoupling
    /// </summary>
    public class PathRequestManager : Singleton<PathRequestManager>
    {
        private PathFinding _pathFinding;
        private bool _isProcessingPath;
        private PathRequest _currentRequest;

        //Debug-only
        #if UNITY_EDITOR
        [SerializeField] private bool isGizmos;
        [SerializeField] private bool displayWaypoints;
        [SerializeField] private bool originalLines;
        private List<PathDebugData> _debugData;
        #endif
        
        private void Start()
        {
            Initialize();
        }
        public void Initialize()
        {
            _pathFinding = GetComponent<PathFinding>();
            _debugData = new List<PathDebugData>();
        }

        public Vector3[] GetPathWaypoints(Vector3 startPos, Vector3 endPos)
        {
            PathRequest pathRequest = new PathRequest(startPos, endPos);
            Vector3[] waypoints = _pathFinding.GetFuncFindPath()?.Invoke(pathRequest);
            if (waypoints != null && waypoints.Length > 0)
            {
               Vector3[] path = ShilftWaypoint(waypoints, RoadManager.RoadWidth/ 4f);
               
               #if UNITY_EDITOR
               _debugData.Add(new PathDebugData()
               {
                   OriginalPaths = new List<Vector3>(waypoints),
                   Waypoints = new List<Vector3>(path),
               });
               #endif

                return path;
            }
            
            return new Vector3[]{};
        }

        /// <summary>
        /// Shifts the path waypoints to the right side of their movement direction.
        /// Cars always stay on the right side relative to their buildingDirection.
        /// Each segment's perpendicular is used to shift points by quarterRoadWidth.
        /// </summary>
        public Vector3[] ShilftWaypoint(Vector3[] pathWaypoints, float quarterRoadWidth)
        {
            List<Vector3> shiftedPoints = new List<Vector3>();
            int count = pathWaypoints.Length;

            for (int i = 0; i < count; i++) {
                Vector2 prev = i > 0 ? pathWaypoints[i - 1] : pathWaypoints[i];
                Vector2 next = i < count - 1 ? pathWaypoints[i + 1] : pathWaypoints[i];
    
                Vector2 forward = (next - prev).normalized;
                Vector2 right = new Vector2(forward.y, -forward.x); 
                Vector2 shiftedPoint = (Vector2)pathWaypoints[i] + right * (quarterRoadWidth);
                shiftedPoints.Add(shiftedPoint);
            }
            
            return shiftedPoints.ToArray();
        }

        
        #if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (!isGizmos || _debugData == null || _debugData.Count == 0)
            {
                return;
            }
            
            foreach (PathDebugData debugData in _debugData)
            {
                if (displayWaypoints)
                {
                    Gizmos.color = Color.red;
                    for (int i = 0; i < debugData.Waypoints.Count; i++)
                    {
                        Gizmos.DrawSphere(debugData.Waypoints[i], 0.05f);
                        if (i<  debugData.Waypoints.Count - 1)
                        {
                            Gizmos.DrawLine(debugData.Waypoints[i], debugData.Waypoints[i+1]);
                        }
                    }
                }

                if (originalLines)
                {
                    Gizmos.color = Color.yellow;
                    for (int i = 0; i < debugData.OriginalPaths.Count; i++)
                    {
                        Gizmos.DrawSphere(debugData.OriginalPaths[i], 0.05f);
                        if (i < debugData.OriginalPaths.Count - 1)
                        {
                            Gizmos.DrawLine(debugData.OriginalPaths[i], debugData.OriginalPaths[i + 1]);
                        }
                    }

                }
            }
        }
        #endif


        public struct PathRequest
        {
            public Vector3 StartPos { get; }
            public Vector3 EndPos { get; }

            public PathRequest(Vector3 startPos, Vector3 endPos)
            {
                this.StartPos = startPos;
                this.EndPos = endPos;
            }
        }
    }
    
}
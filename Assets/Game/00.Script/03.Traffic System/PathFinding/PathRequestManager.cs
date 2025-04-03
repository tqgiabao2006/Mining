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
               Vector3[] path = Path(waypoints, RoadManager.RoadWidth/ 4f);
               
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
        /// Car always run on the right side *from their buildingDirection*
        /// Method: Calculate buildingDirection between 2 points, calculate perpendicular vector to it buildingDirection
        /// normalized it then multiple by 1/2 half roadWidth
        /// </summary>
        /// <param name="pathWaypoints"></param>
        /// <returns></returns>
        public Vector3[] Path(Vector3[] pathWaypoints, float quarterRoadWidth)
        {
            List<Vector3> waypoints = new List<Vector3>();

            //Half normal path
            for (int i = 0; i < pathWaypoints.Length - 2; i++)
            {
                Vector2 direction = (pathWaypoints[i + 1] - pathWaypoints[i]);
                Vector2 perDirection = (new Vector2(direction.y, -direction.x)).normalized;

                Vector3 shiftedPoint1 = new Vector3(quarterRoadWidth * perDirection.x + pathWaypoints[i].x
                    , quarterRoadWidth * perDirection.y + pathWaypoints[i].y, 0);
                if (!waypoints.Contains(shiftedPoint1))
                {
                    waypoints.Add(shiftedPoint1);
                }
                
                Vector3 shilftedPoint2 = new Vector3(quarterRoadWidth * perDirection.x + pathWaypoints[i + 1].x
                    , quarterRoadWidth * perDirection.y + pathWaypoints[i + 1].y, 0);

                if (!waypoints.Contains(shilftedPoint2))
                {
                    waypoints.Add(shilftedPoint2);
                }
            }
            
            waypoints.Add(pathWaypoints[pathWaypoints.Length - 1]);
            return waypoints.ToArray();
            
           
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
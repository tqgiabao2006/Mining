using System.Collections.Generic;
using Game._00.Script._00.Manager;
using Game._00.Script._03.Traffic_System.Road;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game._00.Script._03.Traffic_System.PathFinding
{
    /// <summary>
    /// Working as a bridge from pathfinding, and unit base to try create new thread => optimize, decoupling
    /// </summary>
    public class PathRequestManager : Singleton<PathRequestManager>
    {
        private PathFinding _pathFinding;
        private bool _isProcessingPath;
        private PathRequest _currentRequest;

        [SerializeField] public GameObject debugPrefab; 
        [SerializeField] public bool displayWaypoints;

        private void Start()
        {
            Initialize();
        }
        public void Initialize()
        {
            _pathFinding = GetComponent<PathFinding>();
        }

        public Vector3[] GetPathWaypoints(Vector3 startPos, Vector3 endPos)
        {
            PathRequest pathRequest = new PathRequest(startPos, endPos);
            Vector3[] waypoints = _pathFinding.GetFuncFindPath()?.Invoke(pathRequest);
            if (waypoints != null && waypoints.Length > 0)
            {
               Vector3[] ellipseWaypoints = EllipsePath(waypoints, RoadManager.RoadWidth/ 4f);

               if (displayWaypoints)
               {
                   DisplayPathWaypoints(ellipseWaypoints);
               }
               
               return ellipseWaypoints;
            }
            
            return new Vector3[]{};
        }

        /// <summary>
        /// Turn a straight line into an ellipse rounded path for car to follow
        /// Car always run on the right side *from their buildingDirection*
        /// Method: Calculate buildingDirection between 2 points, calculate perpendicular vector to it buildingDirection
        /// normalized it then multiple by 1/2 half roadWidth
        /// </summary>
        /// <param name="pathWaypoints"></param>
        /// <returns></returns>
        public Vector3[] EllipsePath(Vector3[] pathWaypoints, float quarterRoadWidth)
        {
            //Double waypoints
            List<Vector3> ellipsePathWaypoints = new List<Vector3>();

            //Half normal path
            for (int i = 0; i < pathWaypoints.Length - 1; i++)
            {
                Vector2 direction = (pathWaypoints[i + 1] - pathWaypoints[i]);
                Vector2 perDirection = (new Vector2(direction.y, -direction.x)).normalized;

                Vector3 shiftedPoint1 = new Vector3(quarterRoadWidth * perDirection.x + pathWaypoints[i].x
                    , quarterRoadWidth * perDirection.y + pathWaypoints[i].y, 0);
                if (!ellipsePathWaypoints.Contains(shiftedPoint1))
                {
                    ellipsePathWaypoints.Add(shiftedPoint1);
                }

                Vector3 shilftedPoint2 = new Vector3(quarterRoadWidth * perDirection.x + pathWaypoints[i + 1].x
                    , quarterRoadWidth * perDirection.y + pathWaypoints[i + 1].y, 0);

                if (!ellipsePathWaypoints.Contains(shilftedPoint2))
                {
                    ellipsePathWaypoints.Add(shilftedPoint2);
                }
            }

            //Half reverse path
            for (int i = pathWaypoints.Length - 1; i > 0; i--)
            {
                Vector2 direction = (pathWaypoints[i - 1] - pathWaypoints[i]);
                if (direction == Vector2.zero)
                {
                    continue;
                }

                Vector2 perDirection = new Vector2(direction.y, -direction.x).normalized;

                Vector3 shiftedPoint1 = new Vector3(quarterRoadWidth * perDirection.x + pathWaypoints[i].x
                    , quarterRoadWidth * perDirection.y + pathWaypoints[i].y, 0);
                if (!ellipsePathWaypoints.Contains(shiftedPoint1))
                {
                    ellipsePathWaypoints.Add(shiftedPoint1);
                }

                Vector3 shilftedPoint2 = new Vector3(quarterRoadWidth * perDirection.x + pathWaypoints[i - 1].x
                    , quarterRoadWidth * perDirection.y + pathWaypoints[i - 1].y, 0);

                if (!ellipsePathWaypoints.Contains(shilftedPoint2))
                {
                    ellipsePathWaypoints.Add(shilftedPoint2);
                }

            }

            return ellipsePathWaypoints.ToArray();
            
           
        }
        public void DisplayPathWaypoints(Vector3[] waypoints)
        {
            foreach (Vector3 p in waypoints) 
            {
                Instantiate(debugPrefab,p, Quaternion.identity);
                   
            }
        }

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
using System.Collections.Generic;
using UnityEngine;

namespace Game._00.Script.Demos
{
    public class Test_Path : MonoBehaviour
    {
        [SerializeField] public GameObject visualization;
        LineRenderer lineRenderer;
        private Vector3[] waypoints;
        void Start()
        {
            lineRenderer = GetComponent<LineRenderer>(); 
            waypoints = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(5, 0, 0),
                new Vector3(7,5,0)
            };
            Debug.Log("Before: " + waypoints.Length);
        
            for (int  i = 0;  i < waypoints.Length;  i++)
            {
                lineRenderer.SetPosition(i, waypoints[i]);
            }
        
            waypoints = EllipsePath(waypoints, 1f);
            Debug.Log("After: " + waypoints.Length);

            for (int  i = 0;  i < waypoints.Length;  i++)
            {
                Instantiate(visualization, waypoints[i], Quaternion.identity);
            }
        
        
        }
    
        public Vector3[] EllipsePath(Vector3[] pathWaypoints, float quarterRoadWidth)
        {
            //Double waypoints
            List<Vector3> ellipsePathWaypoints = new List<Vector3>();
            
            //Half normal path
            for (int i = 0; i < pathWaypoints.Length - 1; i ++)
            {
                Vector2 direction = (pathWaypoints[i+1] - pathWaypoints[i]);
                Vector2 perDirection = (new Vector2(direction.y, -direction.x)).normalized;
                Debug.Log("BitwiseDirection: " + direction);

                Vector3 shiftedPoint1 = new Vector3(quarterRoadWidth * perDirection.x + pathWaypoints[i].x
                    , quarterRoadWidth * perDirection.y + pathWaypoints[i].y, 0);
                if (!ellipsePathWaypoints.Contains(shiftedPoint1))
                {
                    ellipsePathWaypoints.Add(shiftedPoint1);
                }
                
                Debug.Log(shiftedPoint1);
                
                
                Vector3 shilftedPoint2 = new Vector3(quarterRoadWidth * perDirection.x + pathWaypoints[i + 1].x
                    , quarterRoadWidth * perDirection.y + pathWaypoints[i + 1].y, 0);

                if (!ellipsePathWaypoints.Contains(shilftedPoint2))
                {
                    ellipsePathWaypoints.Add(shilftedPoint2);
                }
                Debug.Log(shilftedPoint2);
            }
            
            //Half reverse path
            for (int i = pathWaypoints.Length - 1; i > 0; i--)
            {
                Vector2 direction = (pathWaypoints[i-1] - pathWaypoints[i]);
                Debug.Log("Reverse buildingDirection: " + direction);
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

                Vector3 shilftedPoint2 = new Vector3(quarterRoadWidth * perDirection.x +pathWaypoints[i -1].x
                    , quarterRoadWidth * perDirection.y + pathWaypoints[i - 1].y, 0);

                if (!ellipsePathWaypoints.Contains(shilftedPoint2))
                {
                    ellipsePathWaypoints.Add(shilftedPoint2);
                }
            
            }
            
            return ellipsePathWaypoints.ToArray();
        }
    

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}

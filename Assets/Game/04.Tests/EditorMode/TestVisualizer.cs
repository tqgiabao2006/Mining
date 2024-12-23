using System.Collections.Generic;
using UnityEngine;

namespace Game._04.Tests.EditorMode
{
    public class TestVisualizer
    {
        private List<Vector3> waypoints;
        
        //use for test wayspoint in 
        public TestVisualizer(List<Vector3> waypoints)
        {
            this.waypoints = waypoints;
        }

        private void OnGizmos()
        {
            if (this.waypoints != null && this.waypoints.Count > 0)
            {
                Gizmos.color = Color.green;
                foreach (Vector3 waypoint in this.waypoints)
                {
                    Gizmos.DrawSphere(waypoint, 0.1f);
                }
            }
        }
    }
}
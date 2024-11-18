    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Path 
    {
        public readonly Vector2[] lookPoints;
        public readonly Line[] turnBoundaries;
        public readonly int finishLineIndex;

        public readonly int slowDownIndex;

        

        public Path(Vector2[] wayPoints, Vector2 startPos, float turnDistance, float stoppingDistance)
        {
            lookPoints = wayPoints;
            turnBoundaries = new Line[lookPoints.Length];
            finishLineIndex = turnBoundaries.Length - 1;


            Vector2 previousPoint = startPos;
            for(int i = 0; i< lookPoints.Length; i++)
            {
                Vector2 currentPoint = lookPoints[i];
                Vector2 dirToCurrentPoint = (currentPoint - previousPoint).normalized;

                //if i == finishLineIndex => don't substract
                Vector2 turnBoundaryPoint = (i == finishLineIndex) ? currentPoint : currentPoint - dirToCurrentPoint * turnDistance;
                
                //Substract turnDistance > distance between previous and current point => wrong side
                turnBoundaries[i] = new Line(turnBoundaryPoint, previousPoint - dirToCurrentPoint * turnDistance);
                previousPoint = turnBoundaryPoint;
            }

            //Calculate slow down index:
            float distanceFromEndPoint =0;
            for(int i = lookPoints.Length - 1; i > 0; i--)
            {
                distanceFromEndPoint += Vector2.Distance(lookPoints[i], lookPoints[i-1]);
                if(distanceFromEndPoint > stoppingDistance)
                {
                    slowDownIndex = i;
                    break;

                }

            }

        }

        public void DrawWithGizmos()
        {
            Gizmos.color = Color.black;
            foreach(Vector2 p in lookPoints)
            {
                    Gizmos.DrawCube((Vector3)p + Vector3.up, new Vector3(0.5f, 0.5f, 0.5f));
            }
            Gizmos.color = Color.white; 
            foreach(Line l in turnBoundaries)
            {
                l.DrawWithGizmos(2);
            }
        }
        
    }

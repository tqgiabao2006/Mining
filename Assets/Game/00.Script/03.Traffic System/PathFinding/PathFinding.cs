using System.Collections.Generic;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Road;
using Game._00.Script._04.Timer.CurvePath;
using UnityEngine;
using System;
using Debug = UnityEngine.Debug;

namespace Game._00.Script._03.Traffic_System.PathFinding
{
    public class PathFinding : MonoBehaviour
    {
        [SerializeField] private bool isGizmos;

        private BezierSpline _spline;
        
        private RoadManager _roadManager;
        
        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            _roadManager = FindObjectOfType<RoadManager>();
        }

        public Func<PathRequestManager.PathRequest, Vector3[]> GetFuncFindPath()
        {
            return FindPath;
        }
        
        private Vector3[] FindPath(PathRequestManager.PathRequest pathRequest)
        {
            Vector3[] waypoints;
            Node startNode = GridManager.NodeFromWorldPosition(pathRequest.StartPos);
            Node endNode = GridManager.NodeFromWorldPosition(pathRequest.EndPos);
            
            bool pathSuccess = false;
            if (startNode.GraphIndex != endNode.GraphIndex || !startNode.Walkable || !endNode.Walkable)
            {
                return null;
            }

            List<Node> graphList = _roadManager.GetGraphList(startNode);
            int graphListCount = graphList.Count;
            
            Heap<Node> openSet = new Heap<Node>(graphListCount) ; //the set of nodes to be evaluated
            HashSet<Node> closedSet = new HashSet<Node>(); //the set of nodes already evaluated
            
            openSet.Add(startNode);
            startNode.Parent = startNode;

            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                if (currentNode == endNode)
                {
                    pathSuccess = true;
                    break;
                }
                
                List<Node> neighbours = GetNeighboursInAdjList(currentNode);
                foreach (Node neighbour in neighbours) 
                {
                    if (!neighbour.Walkable || closedSet.Contains(neighbour)) {
                        continue;
                    }
					
                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.MovementPenalty;
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, endNode);
                        neighbour.Parent = currentNode;
						
                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else 
                            openSet.UpdateItem(neighbour);
                    }
                }
            }

            if (pathSuccess)
            {
                waypoints = RetracePath(startNode, endNode);
                return waypoints;
            }
            else
            {
                DebugUtility.LogError("Can't find path", this.ToString());
                return null;
            }
            
        }
        
        private Vector3[] RetracePath(Node startNode, Node endNode) {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;
		
            while (currentNode != startNode) {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }

            BezierSpline spline = new BezierSpline(null,null, null,null,CurveRoadMesh.spacing, CurveRoadMesh.curveSmooth);

            spline.AddRawPoint(startNode.WorldPosition);
            for (int i = path.Count - 1; i >= 0; i--)
            {
                spline.AddRawPoint(path[i].WorldPosition);
            }
            
            _spline = spline;

            Vector3[] waypoints = spline.GetEvenlySpacedPoints(0.2f, 10);
            Vector3[] simplifyPath = SimplifyPath(waypoints, startNode, endNode);
            return simplifyPath;
        }
	
        /// <summary>
        /// Cut out repetitive buildingDirection BECAUSE to optimize calculation
        /// Add start, end node BECAUSE track the angle of road when it changes buildingDirection
        /// </summary>
        /// <param name="bezierPath"></param>
        /// <param name="startNode"></param>
        /// <param name="endNode"></param>
        /// <returns></returns>
        private Vector3[] SimplifyPath(Vector3[] bezierPath, Node startNode, Node endNode) 
        {
            List<Vector3> waypoints = new List<Vector3>();
            waypoints.Add(startNode.WorldPosition);

            Vector2 directionOld = Vector2.zero;
            float angleThresholdCos = Mathf.Cos(5 * Mathf.Deg2Rad);

            for (int i = 0; i < bezierPath.Length - 1; i++)
            {
                Vector2 directionNew = (bezierPath[i + 1] - bezierPath[i]).normalized;

                if (Vector2.Dot(directionNew, directionOld) < angleThresholdCos)
                {
                    waypoints.Add(bezierPath[i]);
                }

                directionOld = directionNew;
            }

            waypoints.Add(endNode.WorldPosition);
            return waypoints.ToArray();
        }

        
        private int GetDistance(Node nodeA, Node nodeB) {
            int dstX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
            int dstY = Mathf.Abs(nodeA.GridY - nodeB.GridY);
		
            if (dstX > dstY)
                return 14*dstY + 10* (dstX-dstY);
            return 14*dstX + 10 * (dstY-dstX);
        }
        
        /// <summary>
        /// Get node in adj list BECAUSE some road is nearby but not connected, focus on connection 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<Node> GetNeighboursInAdjList(Node node)
        {
            return _roadManager.GetNodeInAdjList(node);
        }

        private void OnDrawGizmos()
        {
            if (!isGizmos || _spline == null)
            {
                return;
            }
            
            for (int k = 0; k < _spline.NumbSeg; k++)
            {
                Vector2[] points = _spline.GetPointInSegment(k);

                for (int i = 0; i < points.Length; i++)
                {
                    if (i % 3 == 0)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(points[i], 0.05f);
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(points[i], 0.02f);
                    }
                }
            }
        }
    }
    
}
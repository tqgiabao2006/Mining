using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using UnityEngine;

namespace Game._00.Script.NewPathFinding
{
    public class PathFinding : MonoBehaviour
    {
        private RoadManager _roadManager;
        private GridManager _gridManager;

        public void Initialize()
        {
            _gridManager = GameManager.Instance.GridManager;
            _roadManager = GameManager.Instance.RoadManager;
        }

        public Func<NewPathRequest, Vector3[]> GetFuncFindPath()
        {
            return FindPath;
        }
        
        private Vector3[] FindPath(NewPathRequest pathRequest)
        {
            Vector3[] waypoints;
            Node startNode = _gridManager.NodeFromWorldPosition(pathRequest.StartPos);
            Node endNode = _gridManager.NodeFromWorldPosition(pathRequest.EndPos);
            
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
                Debug.Log("Can't find path");
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
            Vector3[] waypoints = SimplifyPath(path, startNode, endNode);
            return waypoints;
        }
	
        
        /// <summary>
        /// Cut out repetitive direction BECAUSE to optimize calculation
        /// Add start, end node BECAUSE track the angle of road when it changes direction
        /// </summary>
        /// <param name="path"></param>
        /// <param name="startNode"></param>
        /// <param name="endNode"></param>
        /// <returns></returns>
        private Vector3[] SimplifyPath(List<Node> path, Node startNode, Node endNode) 
        {
            List<Vector3> waypoints = new List<Vector3>();
            waypoints.Add(startNode.WorldPosition);

            Vector2 directionOld = Vector2.zero;
		
            //Only add the last index of a direction  For ex, up, up, top up, we will simply
            //up _ top up => run outside the road, so we have to simply as _ up, top up
            for (int i = path.Count - 1; i >=1 ; i --)  //Path start from end to startNode so we loop inversely
            {
                Vector2 directionNew = new Vector2(path[i-1].GridX - path[i].GridX,path[i-1].GridY - path[i].GridY);
                if (Mathf.Abs(Vector2.Dot(directionNew.normalized, directionOld.normalized)) < 0.99f)
                {
                    //Add last index of direction 
                    waypoints.Add(path[i].WorldPosition);
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
    }
    
}
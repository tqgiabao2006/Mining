using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using UnityEngine;

namespace Game._00.Script.NewPathFinding
{
    public class NewPathFinding 
    {
        private RoadManager _roadManager;
        private Grid _grid;

        public void Initialize()
        {
            _roadManager = GameManager.Instance.RoadManager;
            _grid = GameManager.Instance.Grid;
        }
        


        public IEnumerator FindPath(Node startNode, Node endNode)
        {
            bool pathSuccess = false;
            if (startNode.GraphIndex != endNode.GraphIndex || !startNode.Walkable || !endNode.Walkable)
            {
                yield return null;
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
                
                List<Node> neighbours = GetNeighboursInGraph(currentNode);
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
            
        }
        
        private Vector3[] RetracePath(Node startNode, Node endNode) {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;
		
            while (currentNode != startNode) {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
		
            }
            Vector3[] waypoints = SimplifyPath(path);
		
            Array.Reverse(waypoints);
            return waypoints;
		
        }
	
        private Vector3[] SimplifyPath(List<Node> path) {
            List<Vector3> waypoints = new List<Vector3>();
            Vector2 directionOld = Vector2.zero;
		
            for (int i = 1; i < path.Count -1; i ++) //Avoid simply start node and end node
            {
                Vector2 directionNew = new Vector2(path[i-1].GridX - path[i].GridX,path[i-1].GridY - path[i].GridY);
                if (directionNew != directionOld) 
                {
                    waypoints.Add(path[i].WorldPosition);
                    if (!waypoints.Contains(path[i - 1].WorldPosition))
                    { 
                        waypoints.Add(path[i - 1].WorldPosition);

                    }
                }
                directionOld = directionNew;
            }
            return waypoints.ToArray();
        }
        
        private int GetDistance(Node nodeA, Node nodeB) {
            int dstX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
            int dstY = Mathf.Abs(nodeA.GridY - nodeB.GridY);
		
            if (dstX > dstY)
                return 14*dstY + 10* (dstX-dstY);
            return 14*dstX + 10 * (dstY-dstX);
        }
        
        private List<Node> GetNeighboursInGraph(Node node)
        {
            List<Node> neighbours = new List<Node>();
            foreach (Node n in node.GetNeighbours())
            {
                if(!n.IsRoad) continue;
                if (n.GraphIndex == node.GraphIndex)
                {
                    neighbours.Add(n);
                }
            }
            return neighbours;
        
        }
    }
    
  

    struct RequestInfo
    {
        
    }
}
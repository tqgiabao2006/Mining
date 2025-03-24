// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System;
// using Game._00.Script._05._Manager;
// using Debug = UnityEngine.Debug;
//
// public class PathFinding : MonoBehaviour {
// 	
// 	// private Test_RequestManager _requestManager;
// 	// private GridManager _gridManager;
// 	//
// 	// void Awake()
// 	// {
// 	// 	_gridManager = GameManager.Instance.GridManager;
// 	// }
// 	//
// 	// public void StartFindPath(Vector3 startPos, Vector3 targetPos) {
// 	// 	StartCoroutine(FindPath(startPos,targetPos));
// 	// }
// 	//
// 	// IEnumerator FindPath(Vector3 startPos, Vector3 targetPos)
// 	// {
// 	// 	Vector3[] waypoints = new Vector3[0];
// 	// 	bool pathSuccess = false;
// 	// 	
// 	// 	OriginBuildingNode startNode = _gridManager.NodeFromWorldPosition(startPos);
// 	// 	OriginBuildingNode targetNode = _gridManager.NodeFromWorldPosition(targetPos);
// 	// 	startNode.Parent = startNode;
// 	// 	
// 	// 	
// 	// 	if (startNode.Walkable && targetNode.Walkable) {
// 	// 		Heap<OriginBuildingNode> openSet = new Heap<OriginBuildingNode>(_gridManager.MaxSize);
// 	// 		HashSet<OriginBuildingNode> closedSet = new HashSet<OriginBuildingNode>();
// 	// 		openSet.Add(startNode);
// 	// 		
// 	// 		while (openSet.Count > 0) {
// 	// 			OriginBuildingNode currentNode = openSet.RemoveFirst();
// 	// 			closedSet.Add(currentNode);
// 	// 			
// 	// 			if (currentNode == targetNode) {
// 	// 				pathSuccess = true;
// 	// 				break;
// 	// 			}
// 	// 			
// 	// 			foreach (OriginBuildingNode neighbour in currentNode.GetNeighbours()) {
// 	// 				if (!neighbour.Walkable || closedSet.Contains(neighbour)) {
// 	// 					continue;
// 	// 				}
// 	// 				
// 	// 				int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.MovementPenalty;
// 	// 				if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
// 	// 					neighbour.gCost = newMovementCostToNeighbour;
// 	// 					neighbour.hCost = GetDistance(neighbour, targetNode);
// 	// 					neighbour.Parent = currentNode;
// 	// 					
// 	// 					if (!openSet.Contains(neighbour))
// 	// 						openSet.Add(neighbour);
// 	// 					else 
// 	// 						openSet.UpdateItem(neighbour);
// 	// 				}
// 	// 			}
// 	// 		}
// 		}
// 		if (pathSuccess) {
// 			waypoints = RetracePath(startNode,targetNode);
// 			Debug.Log("Sucess");
// 			foreach (Vector3 n in waypoints)
// 			{
// 				Debug.Log(n);
// 			}
// 		}
// 		_requestManager.FinishedProcessingPath(waypoints,pathSuccess);
// 		yield return null;
// 	}
// 		
// 	
// 	Vector3[] RetracePath(OriginBuildingNode startNode, OriginBuildingNode endNode) {
// 		List<OriginBuildingNode> path = new List<OriginBuildingNode>();
// 		OriginBuildingNode currentNode = endNode;
// 		
// 		while (currentNode != startNode) {
// 			path.Add(currentNode);
// 			currentNode = currentNode.Parent;
// 		
// 		}
// 		Vector3[] waypoints = SimplifyPath(path);
// 		
// 		Array.Reverse(waypoints);
// 		return waypoints;
// 		
// 	}
// 	
// 	Vector3[] SimplifyPath(List<OriginBuildingNode> path) {
// 		List<Vector3> waypoints = new List<Vector3>();
// 		Vector2 directionOld = Vector2.zero;
// 		
// 		for (int i = 1; i < path.Count -1; i ++) //Avoid simply start node and end node
// 		{
// 			Vector2 directionNew = new Vector2(path[i-1].GridX - path[i].GridX,path[i-1].GridY - path[i].GridY);
// 			if (directionNew != directionOld) 
// 			{
// 				waypoints.Add(path[i].WorldPosition);
// 				if (!waypoints.Contains(path[i - 1].WorldPosition))
// 				{ 
// 					waypoints.Add(path[i - 1].WorldPosition);
//
// 				}
// 			}
// 			directionOld = directionNew;
// 		}
// 		return waypoints.ToArray();
// 	}
// 	
// 	int GetDistance(OriginBuildingNode nodeA, OriginBuildingNode nodeB) {
// 		int dstX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
// 		int dstY = Mathf.Abs(nodeA.GridY - nodeB.GridY);
// 		
// 		if (dstX > dstY)
// 			return 14*dstY + 10* (dstX-dstY);
// 		return 14*dstX + 10 * (dstY-dstX);
// 	}
// 	
// 	
// }
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;



public class Node : IHeapItem<Node>
{
	
	public bool Walkable;
	public Vector3 WorldPosition {private set; get;}
	public int GridX {private set; get;}
	public int GridY {private set; get;}

	public int MovementPenalty;

	public int gCost;
	public int hCost;
	public Node Parent;
	
	int heapIndex;

	public bool IsRoad;
	public bool IsBuilding;
	
	public int NodeIndex = -1; //its index of ALL MAP
	
	private int _graphIndex; //Use for check connection

	public int GraphIndex
	{
		get{return _graphIndex;}
		set
		{
			_graphIndex = value;
		} 
	}


	public Grid grid;
	public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty, Grid grid) {
		this.Walkable = _walkable;
		this.WorldPosition = _worldPos;
		this.GridX = _gridX;
		this.GridY = _gridY;
		this.MovementPenalty = _penalty;
		this._graphIndex = -1;
		this.grid = grid;
	}

	public void SetRoad(bool isRoad)
	{ 
		IsRoad = isRoad;
		Walkable = true;
	}

	public void SetBuilding(bool isBuilding)
	{
		IsBuilding = isBuilding;
	}
	
	public List<Node> GetNeighbours()
	{
		List<Node> neighbours = new List<Node>();
    
		// Search the 3x3 grid with the current node as the center
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				if (x == 0 && y == 0) continue; // Ignore the center node

				int checkX = this.GridX + x; // Calculate the neighboring node's x position
				int checkY = this.GridY + y; // Calculate the neighboring node's y position

				// Ensure the neighbor's position is within bounds
				if (checkX >= 0 && checkY >= 0 && checkX < grid.GridSizeX && checkY < grid.GridSizeY)
				{
					neighbours.Add(grid.grid[checkX, checkY]); // Add the neighbor node to the list
				}
			}
		}
		return neighbours; // Return the list of neighbor nodes
	}
	
	public int fCost {
		get {
			return gCost + hCost;
		}
	}
	public int HeapIndex {
		get {
			return heapIndex;
		}
		set {
			heapIndex = value;
		}
	}

	public int CompareTo(Node nodeToCompare) {
		int compare = fCost.CompareTo(nodeToCompare.fCost);
		if (compare == 0) {
			compare = hCost.CompareTo(nodeToCompare.hCost);
		}
		return -compare;
	}
}
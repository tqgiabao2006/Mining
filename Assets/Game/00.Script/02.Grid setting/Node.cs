using System.Collections.Generic;
using UnityEngine;

namespace Game._00.Script._02.Grid_setting
{
	public class Node : IHeapItem<Node>
	{
		private GameObject _belongedBuilding;
	
		private bool _walkable;

		private bool _isBuilding;
		
		private bool _isRoad;
		
		private bool _canDraw; //Used to tag a node can be drawn a road on, there is 1 case: parking lot that the road connect but not a road

		public Vector3 WorldPosition {private set; get;}
		
		public int GridX { get;}
		
		public int GridY { get;}

		public int MovementPenalty;

		public int gCost;
		
		public int hCost;
		
		public Node Parent;
		
		public int NodeIndex = -1; //its index of ALL MAP
    	
		private int _graphIndex; //Use for check connection
    
		public GridManager GridManager;
		
		int heapIndex;
		
		public bool Walkable
		{
			get { return _walkable; }
		}
		
		public bool IsEmpty
		{
			get { return !_isBuilding && !_isRoad; }
		}

		public bool IsRoad
		{
			get { return _isRoad; }
		}

		public bool CanDraw
		{
			get { return _canDraw; }
		}
		
		public bool IsBuilding
		{
			get { return _isBuilding; }
		}


		public GameObject BelongedBuilding
		{
			get { return _belongedBuilding; }
		}
		
		public int GraphIndex
		{
			get{return _graphIndex;}
			set
			{
				_graphIndex = value;
			} 
		}
		public Node(bool walkable, Vector3 worldPos, int gridX, int gridY, int penalty, GridManager gridManager) 
		{
			this._walkable = walkable;
			this.WorldPosition = worldPos;
			this.GridX = gridX;
			this.GridY = gridY;
			this.MovementPenalty = penalty;
			this._graphIndex = -1;
			this.GridManager = gridManager;
			_canDraw = true;
		}

		public void SetRoad(bool isRoad)
		{
			_isRoad = isRoad;
			_walkable = isRoad;
		}


		public void SetBuilding(bool isBuilding)
		{
			_isBuilding = isBuilding;
		}

		public void SetDrawable(bool isDrawable)
		{
			_canDraw = isDrawable;
		}

		public void SetWalkable(bool walkable)
		{
			_walkable = walkable;
		}

		public void SetBelongedBuilding(GameObject building)
		{
			_belongedBuilding = building;
		}
		public List<Node> GetNeighbours()
		{
			List<Node> neighbours = new List<Node>();
    
			// Search the 3x3 _gridManager with the current node as the center
			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					if (x == 0 && y == 0) continue; // Ignore the center node

					int checkX = this.GridX + x; // Calculate the neighboring node's x position
					int checkY = this.GridY + y; // Calculate the neighboring node's y position

					// Ensure the neighbor's position is within bounds
					if (checkX >= 0 && checkY >= 0 && checkX < GridManager.GridSizeX && checkY < GridManager.GridSizeY)
					{
						neighbours.Add(GridManager.Grid[checkX, checkY]); // Add the neighbor node to the list
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
}
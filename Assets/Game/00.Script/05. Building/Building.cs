using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class Building : MonoBehaviour
{
    private RoadMesh _roadMesh;
    private BuildingManager _buildingManager;
    private Grid _grid;
    private GameManager _gameManager; 
    private RoadManager _roadManager;
    
    private Vector2 _worldPosition;
    
    private BoxCollider2D _boxCollider;

    public Vector2 WorldPosition
    {
        get { return _worldPosition; }
        set { _worldPosition = value; }
    }

    public BuildingType BuildingType { get; private set; }  // Make it a property
    [SerializeField] public float LifeTime = 2f;
    [SerializeField] public float buildingSize = 0.25f;
    
    public void Initialize(Node node, Grid grid, BuildingType buildingType ,BuildingManager buildingManager, Vector2 worldPosition, RoadMesh roadMesh)
    {
        this._gameManager = GameManager.Instance;
        this._roadManager = _gameManager.RoadManager;
        
        this.BuildingType = buildingType;
        this._buildingManager = buildingManager;
        this._worldPosition = worldPosition;
        
        this._boxCollider = GetComponent<BoxCollider2D>();
        this._boxCollider.isTrigger = true;
        
        this._roadMesh = roadMesh;
        this._grid = grid;
        
        node.SetBuilding(true);
        _roadManager.PlaceNode(node);
            // Invoke("DeactivateBuilding", LifeTime);
        SpawnRoad(node);
    }

    private void DeactivateBuilding()
    {
        this.gameObject.SetActive(false);
    }

    private void SpawnRoad(Node buildingNode)
    {
        Node roadNode = GetRoadNode();
        _roadManager.PlaceNode(roadNode);
        _roadManager.SetAdjList(roadNode, buildingNode);
        _roadManager.CreateMesh(roadNode);
    }
    
    /// <summary>
    /// Iten 1 =  Road node, Item 2 = DirectionType of road
    /// Get random node, around the building to make road mesh
    /// Get DirectionType for that road
    /// </summary>
    /// <returns></returns>
    private Node GetRoadNode()
    {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.right, Vector2.left};
        int directionIndex = Random.Range(0, 4);
        Vector2 direction = directions[directionIndex];
        
        //It will be +building size in futured
        Vector2 nodePos = direction + new Vector2(this._worldPosition.x, this._worldPosition.y);
        Node node = _grid.NodeFromWorldPosition(nodePos);
        return node;
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(this.transform.position, buildingSize);
    }
}

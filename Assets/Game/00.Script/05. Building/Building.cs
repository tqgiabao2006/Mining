using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class Building : MonoBehaviour
{
    private RoadManager _roadManager;
    private GameManager _gameManager;
    private Grid _grid;
    private Vector2 _worldPosition;
    public Vector2 WorldPosition
    {
        get { return _worldPosition; }
        set { _worldPosition = value; }
    }

    public BuildingType BuildingType { get; private set; }  // Make it a property
    [SerializeField] public float LifeTime = 2f;
    [SerializeField] public float buildingSize = 0.25f;
    
    public void Initialize (Node node, Grid grid, BuildingType buildingType, Vector2 worldPosition)
    {
        this._gameManager = GameManager.Instance;
        this._roadManager = _gameManager.RoadManager;
        this._grid = grid;  
        
        this.BuildingType = buildingType;
        this._worldPosition = worldPosition;
        
        SpawnBuildingNode(node, buildingType);
        
        // Invoke("DeactivateBuilding", LifeTime);
        SpawnRoad(node);
    }

    private void DeactivateBuilding()
    {
        this.gameObject.SetActive(false);
    }

    private void SpawnBuildingNode(Node node, BuildingType buildingType)
    {
        //Place building node
        node.SetBuilding(true);
        _roadManager.PlaceNode(node, buildingType);
    }

    private void SpawnRoad(Node buildingNode)
    {
        Node roadNode = GetRoadNode();
        _roadManager.PlaceNode(roadNode, BuildingType.None);
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

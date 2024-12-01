using System;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public 
    class PlacingSystem : SubjectBase
{
    [Header("Gizmos Setting")] [SerializeField]
    public bool showGizmos = true;

    [Header("Handles Setting")] [SerializeField]
    public bool showHandles = true;

    [SerializeField] public float handlesSize = .4f;
    [SerializeField] public Color handleColor = Color.red;
    [SerializeField] public Color lineColor = Color.yellow;
    [SerializeField] public float lineThickness = 50.0f;

    //Mesh Creator:
    private RoadMesh _roadMesh;

    //Input handle:
    [Header("Input Setting")] private GridManager _gridManager;
    private Vector2 _mousePos;
    private bool _isPlacing = false;
    private Node _curNode = null; //After applying threshold
    private Vector2 _lastMousePos;
    private List<Node> _selectedNodes = new List<Node>();
    private float _baseThreshold;
    private float _diagonalThreshold = 0.05f;
    private float _fastThreshold = 0f;
    
    //Manager:
    private RoadManager _roadManager;
    private GameStateManager _gameStateManager;
    
    //Observer:
    /// <summary>
    /// Include: GameStateManager => catch isPlacing system
    /// </summary>'
    
    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        _gridManager = GetComponentInParent<GridManager>();
        _roadMesh = FindObjectOfType<RoadMesh>();
        
        //Manager set up
        _gameStateManager = GameManager.Instance.GameStateManager;
        _roadManager =  GameManager.Instance.RoadManager;
        
        //Obsever set up
        ObserversSetup();
        
        //Threshold set up:
        _baseThreshold = _gridManager.NodeRadius / 1.5f;

    }
    
    private void Update()
    {
        InputProcess();
    }
    

    private void InputProcess()
    {
        _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0) && isInGrid() && isInRoadOrBuilding())
        {
            _isPlacing = true;
            _selectedNodes.Clear();
            
            //Notify obsevers:
            Notify(true, NotificationFlags.PlacingState);

            // Start with the initial node
            _curNode = _gridManager.NodeFromWorldPosition(_mousePos);
            _selectedNodes.Add(_curNode);
        }

        if (Input.GetMouseButtonUp(0)) // Stop placing when mouse button is released
        {
            _isPlacing = false;
            Notify(false, NotificationFlags.PlacingState);
            Notify(true, NotificationFlags.CheckingConnection);
        }

        if (_isPlacing)
        {
            float distance = Vector2.Distance(_mousePos, _lastMousePos);
            float mouseSpeed = distance / Time.deltaTime;

            float threshold = _baseThreshold;
            
            //If mouse moving fast => no threshold
            if(mouseSpeed >= 40)
            {
                threshold = _fastThreshold;
            }
            Node newNode = NodeFromWorldPositionWithSnapping(_mousePos, _curNode, _diagonalThreshold, threshold);

            if (newNode != _curNode) 
            {
                _roadManager.PlaceNode(newNode, null);
                _roadManager.SetAdjList(_curNode, newNode);
                _selectedNodes.Add(newNode);
                _roadManager.CreateMesh(newNode);
                _curNode = newNode;
            }
        }

        _lastMousePos = _mousePos;
    }

    #region Obsever
    
 
    public override void ObserversSetup()
    {
        _observers.Add(_gameStateManager);
        _observers.Add(_roadManager);

        foreach (var observer in _observers)
        {
            Attach(observer);
        }
        
    }
    #endregion
    
    
    #region Input Helpers
    
    /// <summary>
    /// Correct user mouesPosition
    /// </summary>
    /// <param name="mousePos"></param>
    /// <param name="curNode"></param>
    /// <param name="diagonalThreshold"></param>
    /// <param name="dynamicThreshold"></param>
    /// <returns></returns>
    private Node NodeFromWorldPositionWithSnapping(Vector2 mousePos, Node curNode, float diagonalThreshold, float dynamicThreshold)
    {
        //Diagonal corner
        Vector2 botLeft = new Vector2(curNode.WorldPosition.x - _gridManager.NodeRadius + diagonalThreshold, curNode.WorldPosition.y - _gridManager.NodeRadius + diagonalThreshold);
        Vector2 botRight= new Vector2(curNode.WorldPosition.x + _gridManager.NodeRadius - diagonalThreshold, curNode.WorldPosition.y - _gridManager.NodeRadius + diagonalThreshold);
        Vector2 topLeft = new Vector2(curNode.WorldPosition.x - _gridManager.NodeRadius + diagonalThreshold, curNode.WorldPosition.y + _gridManager.NodeRadius - diagonalThreshold);
        Vector2 topRight = new Vector2(curNode.WorldPosition.x + _gridManager.NodeRadius - diagonalThreshold, curNode.WorldPosition.y + _gridManager.NodeRadius - diagonalThreshold);
        
        float nodeDiamater = _gridManager.NodeDiameter;
        if (isInThreshold(botLeft, diagonalThreshold, mousePos))
        {
            return _gridManager.NodeFromWorldPosition(new Vector2(curNode.WorldPosition.x - nodeDiamater, curNode.WorldPosition.y - nodeDiamater));
        }
        
        if (isInThreshold(botRight, diagonalThreshold, mousePos))
        {
            return _gridManager.NodeFromWorldPosition(new Vector2(curNode.WorldPosition.x + nodeDiamater, curNode.WorldPosition.y - nodeDiamater));
        }
        
        if (isInThreshold(topLeft, diagonalThreshold, mousePos))
        {
            return _gridManager.NodeFromWorldPosition(new Vector2(curNode.WorldPosition.x - nodeDiamater, curNode.WorldPosition.y + nodeDiamater));
        }
        
        if (isInThreshold(topRight, diagonalThreshold, mousePos))
        {
            return _gridManager.NodeFromWorldPosition(new Vector2(curNode.WorldPosition.x + nodeDiamater, curNode.WorldPosition.y + nodeDiamater));
        }
        
        float distance = Vector2.Distance(curNode.WorldPosition, mousePos);
        Node nextNode = _gridManager.NodeFromWorldPosition(mousePos);
        
        //Vertical and horizontal threshold
        if (distance >= dynamicThreshold + _gridManager.NodeRadius && nextNode != curNode)
        {
            return nextNode;
        }
        
        return curNode;
    }

    public bool isInThreshold(Vector2 center, float radius, Vector2 checkPos)
    {
        return Vector2.Distance(center, checkPos) <= radius;
    }

    private bool isInRoadOrBuilding()
    {
        Node node = _gridManager.NodeFromWorldPosition(_mousePos);
        return node.IsRoad || node.IsBuilding;
    }

    private bool isInGrid()
    {
        return _mousePos.x >= -_gridManager.GridSizeX / 2 && _mousePos.x <= _gridManager.GridSizeX / 2 &&
               _mousePos.y >= -_gridManager.GridSizeY / 2 && _mousePos.y <= _gridManager.GridSizeY / 2;
    }
    #endregion

        
    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        if(!showGizmos || _curNode == null || _gridManager != null) return;
    }

    #endregion
}



using System.Collections.Generic;
using Game._00.Script._00.Manager;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Road;
using UnityEngine;

namespace Game._00.Script._01.PlacingSystem
{
    public class PlacingSystem : MonoBehaviour
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

        //Input handle:
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
            //Manager set up
            _gameStateManager = GameManager.Instance.GameStateManager;
            _roadManager = FindObjectOfType<RoadManager>();
        
            //Obsever set up
        
            //Threshold set up:
            _baseThreshold = GridManager.NodeRadius / 1.5f;
        }
    
        private void Update()
        {
            InputProcess();
        }
    

        private void InputProcess()
        {
            _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButtonDown(0) && isInGrid() && IsInWalkableNode())
            {
                _isPlacing = true;
                _selectedNodes.Clear();
            
                // Start with the initial node
                _curNode = GridManager.NodeFromWorldPosition(_mousePos);
                _selectedNodes.Add(_curNode);
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
                    _roadManager.PlaceNode(newNode);
                    _roadManager.SetAdjList(_curNode, newNode);
                    _selectedNodes.Add(newNode);
                    _roadManager.CreateMesh(newNode);
                    _curNode = newNode;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isPlacing = false;
            }
            _lastMousePos = _mousePos;
        }

    
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
            Vector2 botLeft = new Vector2(curNode.WorldPosition.x -GridManager.NodeRadius + diagonalThreshold, curNode.WorldPosition.y -GridManager.NodeRadius + diagonalThreshold);
            Vector2 botRight= new Vector2(curNode.WorldPosition.x +GridManager.NodeRadius - diagonalThreshold, curNode.WorldPosition.y - GridManager.NodeRadius+ diagonalThreshold);
            Vector2 topLeft = new Vector2(curNode.WorldPosition.x - GridManager.NodeRadius + diagonalThreshold, curNode.WorldPosition.y + GridManager.NodeRadius - diagonalThreshold);
            Vector2 topRight = new Vector2(curNode.WorldPosition.x + GridManager.NodeRadius - diagonalThreshold, curNode.WorldPosition.y +GridManager.NodeRadius - diagonalThreshold);
        
            float nodeDiamater =GridManager.NodeDiameter;
            if (IsInThreshold(botLeft, diagonalThreshold, mousePos))
            {
                return GridManager.NodeFromWorldPosition(new Vector2(curNode.WorldPosition.x - nodeDiamater, curNode.WorldPosition.y - nodeDiamater));
            }
        
            if (IsInThreshold(botRight, diagonalThreshold, mousePos))
            {
                return GridManager.NodeFromWorldPosition(new Vector2(curNode.WorldPosition.x + nodeDiamater, curNode.WorldPosition.y - nodeDiamater));
            }
        
            if (IsInThreshold(topLeft, diagonalThreshold, mousePos))
            {
                return GridManager.NodeFromWorldPosition(new Vector2(curNode.WorldPosition.x - nodeDiamater, curNode.WorldPosition.y + nodeDiamater));
            }
        
            if (IsInThreshold(topRight, diagonalThreshold, mousePos))
            {
                return GridManager.NodeFromWorldPosition(new Vector2(curNode.WorldPosition.x + nodeDiamater, curNode.WorldPosition.y + nodeDiamater));
            }
        
            float distance = Vector2.Distance(curNode.WorldPosition, mousePos);
            Node nextNode = GridManager.NodeFromWorldPosition(mousePos);
        
            //Vertical and horizontal threshold
            if (distance >= dynamicThreshold + GridManager.NodeRadius && nextNode != curNode)
            {
                return nextNode;
            }
        
            return curNode;
        }

        public bool IsInThreshold(Vector2 center, float radius, Vector2 checkPos)
        {
            return Vector2.Distance(center, checkPos) <= radius;
        }

        private bool IsInWalkableNode()
        {
            Node node = GridManager.NodeFromWorldPosition(_mousePos);
            return node.Walkable;
        }

        private bool isInGrid()
        {
            return _mousePos.x >= -GridManager.GridSizeX / 2 && _mousePos.x <= GridManager.GridSizeX / 2 &&
                   _mousePos.y >= -GridManager.GridSizeY / 2 && _mousePos.y <= GridManager.GridSizeY / 2;
        }
        #endregion

        
        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            if(!showGizmos || _curNode == null ) return;
        }

        #endregion
    }
}



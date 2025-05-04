using System.Collections.Generic;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using Game._00.Script._03.Traffic_System.Road;
using Game._00.Script._04.Timer.CurvePath;
using Game._00.Script.Camera;
using UnityEngine;
namespace Game._00.Script._01.PlacingSystem
{
    public class PlacingSystem : SubjectBase
    { 
        //==================BEZIER CURVE============================
        // [Tooltip("The smaller, the smooth of the curve is")]
        // [SerializeField] [Range(0.05f, 1.5f)] private float spacing;
        //
        // [Tooltip("The smaller, the lest impact of the control point to the curve, usually smaller than curve length")]
        // [SerializeField] [Range(0, 0.5f)] private float straightControlLength;
        //
        //
        // [Tooltip("Usually larger than straight Control Length")]
        // [SerializeField] [Range(0.05f, 0.5f)] private float curveControlLength;
        //
        // private CurveRoadMesh _roadMesh;
        //
        // private CurvePath _path;
        
        
        //====================CATMULL-ROM CURVE============================
        private BezierSpline _curSpline;
        
        private CurveRoadMesh _roadMesh;

        private Stack<Vector2> _prevStack; //Used to track the prev node, when move into, then trigger undo the road

        //Input handle:
        private Vector2 _mousePos;
 
        private bool _isPlacing;
        
        private Node _curNode; //After applying threshold
        
        private Vector2 _lastMousePos;
        
        private float _baseThreshold;
        
        private float _diagonalThreshold = 0.05f;
        
        private float _fastThreshold = 0f;
    
        //Manager:
        private RoadManager _roadManager;
        
        private BuildingManager _buildingManager;
        
        private CameraZoom _cameraZoom; 
        
        private UI_Grid _uiGrid;
        
        private GridManager _gridManager;

        
        //Observer:
        /// <summary>
        /// Include: GameStateManager => catch isPlacing system
        /// </summary>
    
        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            //Manager set up
            _roadManager = FindObjectOfType<RoadManager>();
            
            _buildingManager = FindObjectOfType<BuildingManager>();
            
            _gridManager = FindObjectOfType<GridManager>();
            
            _cameraZoom = CameraZoom.Instance;
            
            _uiGrid = FindObjectOfType<UI_Grid>();
            
            _roadMesh = FindObjectOfType<CurveRoadMesh>();

            _prevStack = new Stack<Vector2>();
            
            //Observer set up
           ObserversSetup(); 
        
            //Threshold set up:
            _baseThreshold = GridManager.NodeRadius / 1.5f;
            
        }
    
        private void Update()
        {
            InputProcess();
        }
    

        private void InputProcess()
        {
            _mousePos = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButtonDown(0) && IsInGrid() && IsInWalkableNode() && IsInDrawableNode())
            {
                _isPlacing = true;
            
                // Start with the initial node
                _curNode = GridManager.NodeFromWorldPosition(_mousePos);
            }

            if (_isPlacing)
            {
                Notify(null, NotificationFlags.PLACING);
                _cameraZoom.EnabledZoom = false;
                
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
                    if (_prevStack.Count == 0 || Vector2.Distance(_prevStack.Peek(),newNode.WorldPosition) > 0.1f)
                    {
                        _roadManager.PlaceNode(newNode);
                        _roadManager.SetAdjList(_curNode, newNode);

                        if (_curSpline == null)
                        {
                            _curSpline = _roadMesh.CreateSpline();
                            _curSpline.AddPoint(_curNode.WorldPosition);
                        }
                        
                        _curSpline.AddPoint(newNode.WorldPosition);
                        
                        _roadMesh.UpdateRoadMesh(_curSpline);
                        
                        _gridManager.UpdateWalkable(newNode.WorldPosition);
                        // _roadManager.CreateMesh(newNode);

                        _prevStack.Push(_curNode.WorldPosition);

                        //NOTICE: Notify after the road manager update graph because use graph index to determine if 2 road is connected
                        //CHECK: after place a new road => possibility that there are some homes connecteed
                        Notify(null, NotificationFlags.CHECK_CONNECTION);
                    }
                    else
                    {
                        _prevStack.Pop();
                        _curSpline.Pop();
                        _roadMesh.UpdateRoadMesh(_curSpline);
                    }
                    
                    _curNode = newNode;
                }
            }
            else
            {
                Notify(null, NotificationFlags.NOT_PLACING);
                _cameraZoom.EnabledZoom = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _curSpline = null;
                _isPlacing = false;
                _prevStack.Clear();
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
           return GridManager.NodeFromWorldPosition(_mousePos).Walkable;
        }

        private bool IsInDrawableNode()
        {
            return GridManager.NodeFromWorldPosition(_mousePos).CanDraw;
        }

        private bool IsInGrid()
        {
                   //Inside the grid && Inside the bound of the map
            return _mousePos.x >= -GridManager.GridSizeX / 2 && _mousePos.x <= GridManager.GridSizeX / 2 &&
                   _mousePos.y >= -GridManager.GridSizeY / 2 && _mousePos.y <= GridManager.GridSizeY / 2 &&
                   _mousePos.x >= -_cameraZoom.Zone.Size.x/2  && _mousePos.x <= _cameraZoom.Zone.Size.x/2 &&
                   _mousePos.y >= -_cameraZoom.Zone.Size.y/2 && _mousePos.y <= _cameraZoom.Zone.Size.y/2;
        }
        
        #endregion
        public override void ObserversSetup()
        {
           _observers.Add(_buildingManager); 
           _observers.Add(_uiGrid);
        }
      }
}



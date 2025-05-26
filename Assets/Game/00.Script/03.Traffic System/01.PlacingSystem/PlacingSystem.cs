using System.Collections.Generic;
using System.Numerics;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using Game._00.Script._03.Traffic_System.Road;
using Game._00.Script._04.Timer.CurvePath;
using Game._00.Script.Camera;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace Game._00.Script._01.PlacingSystem
{
    public class PlacingSystem : SubjectBase
    { 
        //Spline
        private BezierSpline _curSpline;
        
        private CurveRoadMesh _roadMesh;

        private Stack<Vector2> _prevStack; //Used to track the prev node, when move into, then trigger undo the road

        //Input handle:
        private Vector2 _mousePos;
 
        private bool _isPlacing;

        private Node _curNode; //After applying threshold

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

            if (_isPlacing && IsInGrid())
            {
                Notify(null, NotificationFlags.PLACING);
                _cameraZoom.EnabledZoom = false;
                
                Node newNode = null;
                
                //Avoid too responsive, and never place diagonal node
                if (Vector2.Distance(_mousePos, _curNode.WorldPosition) >= GridManager.NodeDiameter)
                {
                    newNode = GridManager.NodeFromWorldPosition(_mousePos);
                }
                
                if (newNode != _curNode && newNode != null && IsInGrid()) 
                {
                    if (_prevStack.Count == 0 || Vector2.Distance(_prevStack.Peek(),newNode.WorldPosition) > 0.1f)
                    {
                        _roadManager.PlaceNode(newNode);
                        _roadManager.SetAdjList(_curNode, newNode);

                        if (_curSpline == null || _curSpline.NumbSeg == 0)
                        {
                            _curSpline = _roadMesh.CreateSpline();
                            _curSpline.AddPoint(_curNode.WorldPosition);
                            _curNode.SetRoad(true);
                        }
                        
                        _curSpline.AddPoint(newNode.WorldPosition);
                        _curNode.SetRoad(true);
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
                        GridManager.NodeFromWorldPosition(_prevStack.Peek()).SetRoad(false);
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

            if (Input.GetMouseButtonUp(0) || !IsInGrid())
            {
                _curSpline = null;
                _isPlacing = false;
                _prevStack.Clear();
            }
        }

    
        #region Input Helpers
        private bool IsInWalkableNode()
        {
           return GridManager.NodeFromWorldPosition(_mousePos).Walkable;
        }

        private bool IsInDrawableNode()
        {
            return GridManager.NodeFromWorldPosition(_mousePos).CanDraw;
        }

        private bool IsInSide(Vector2 checkPos, float centerX, float centerY, float width, float height)
        {
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            
            return checkPos.x >= centerX - halfWidth && checkPos.x <= centerX + halfWidth && checkPos.y >= centerY - halfHeight && checkPos.y <= centerY + halfHeight;
        }
        
        private bool IsInGrid()
        {
            return IsInSide(_mousePos, 0.5f, 0.5f, GridManager.GridSizeX, GridManager.GridSizeY) 
                && IsInSide(_mousePos, 0.5f, 0.5f, _cameraZoom.InteractZone.Size.x, _cameraZoom.InteractZone.Size.y);
        }
        
        #endregion
        public override void ObserversSetup()
        {
           _observers.Add(_buildingManager); 
           _observers.Add(_uiGrid);
        }
      }
}



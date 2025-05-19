using System.Collections.Generic;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS;
using Game._00.Script._03.Traffic_System.Mesh_Generator;
using Game._00.Script._03.Traffic_System.Road;
using Game._00.Script._04.Timer.CurvePath;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;


namespace Game._00.Script._03.Traffic_System.Building
{
    public abstract class BuildingBase : MonoBehaviour,IDebugable
    {
        [Header("Debug settings")] 
        [SerializeField] protected bool isGizmos;

        [Tooltip("Draw parking waypoints")]
        [SerializeField] protected bool drawWaypoints;

        [Tooltip("Draw road node, and layout, car numbers, or demands")]
        [SerializeField] protected bool drawInfo;
        
        [Header("Basic stats")] 
        [SerializeField] private BuildingSpriteCollection spriteCollections;

        [SerializeField] private ParkingLotSize size;

        [SerializeField] private BuildingType buildingType;
        
        [SerializeField] private BuildingColor buildingColor;

        [Header("Curve smoothness")] 
        [Tooltip("The smaller, the smoother the curve")]
        [Range(0.05f, 0.2f)] 
        [SerializeField] private float spacing = 0.3f;
    
        [Tooltip("The larger, the smoother the curve")] 
        [Range(1, 20)] 
        [SerializeField]private  int curveSmooth = 10;
        
        protected Dictionary<DebugMenu.DebugFlag, bool> debugButtonMap;
        //Manager
        protected BuildingManager BuildingManager;
        
        protected EntityManager EntityManager;

        //Default
        protected Vector2 _worldPosition;

        private bool _isConnected;

        private List<Node> _parkingNodes;

        protected List<float3> TestParkingWaypoints;

        public List<ParkingLot> ParkingPos; //Parking lots positions
        
        protected float3 _centerPos;

        protected Node _originBuildingNode;
        
        protected Node _roadNode;
        
        protected Queue<Entity> ParkingResquest;
        
        
        public BuildingDirection BuildingDirection { get; private set; }

        public BuildingType BuildingType
        {
            get
            {
                return buildingType;
            }
        }

        public BuildingColor BuildingColor
        {
            get
            {
                return buildingColor;
            }
        }

        public ParkingLotSize Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
            }
        }
       
        public BuildingSpriteCollection SpriteCollections
        {
            get { return spriteCollections; }
        }
        public Vector2 WorldPosition
        {
            get { return _worldPosition; }
            set { _worldPosition = value; }
        }
        
        public Node RoadNode
        {
          get { return _roadNode; }
          set
          {
              _roadNode = value;
          }
        }

        public Node OriginBuildingNode
        {
            get { return _originBuildingNode; }
        }
        public bool IsConnected
        {
            get{ return _isConnected; }
            set {_isConnected = value;}
        }
        
        public float3 CenterPos
        {
            set { _centerPos = value; }
        }
        
        
        public List<Node> ParkingNodes
        {
            get {return _parkingNodes;}  
            set {_parkingNodes = value;}
        }
        public virtual void Initialize(BuildingManager buildingManager, Node node, BuildingType buildingType, BuildingDirection direction,Vector2 worldPosition)
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
           
            this.BuildingManager = buildingManager; 
            
            this.BuildingDirection = direction;
            
            this.buildingType = buildingType;

            this._worldPosition = worldPosition;
            
            this._originBuildingNode = GridManager.NodeFromWorldPosition(worldPosition);
            
            _parkingNodes = new List<Node>();
            
            ParkingPos = new List<ParkingLot>();
            
            ParkingResquest = new Queue<Entity>();
            
            SetUpDebugFlag();
            
            //Test-only
            #if    UNITY_EDITOR
            TestParkingWaypoints = new List<float3>();
            #endif
        }

        private void DeactivateBuilding()
        {
            this.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Receive a parking request, if available, create waypoints in parking lot
        /// 2x1 parking node is divided by 4 |/ / / /|, number of parking lots = 3,
        /// position1 = node1.WorldPos, position2 = node2.WorldPos, position 3 = point between them, x (for up, down), y for (left, right) buildingDirection
        /// Flow: Go in to the top right then go left until reach the parking lot x, then go to it, then move to the bottom then go right to out
        /// Different flow with different buildingDirection: Left, Up => start bottom to top, Down, Right => Top to bottom
        /// </summary>
        /// <param name="car"></param>
        public void GetParkingRequest(Entity car)
        {
            ParkingResquest.Enqueue(car); 
            // Check if any slot is available
            float3 parkingPos = float3.zero;
            bool foundSlot = false;

            if (this is Business)
            {
                Business business = this as Business;
                business.CarEnter();
            }

            for (int i = 0; i < ParkingPos.Count; i++)
            {
                if (ParkingPos[i].IsEmpty)
                {
                    parkingPos = ParkingPos[i].Position;
                    foundSlot = true;
                    break;
                }
            }

            // Ensure the entity has ParkingData before modifying it
            if (EntityManager.HasComponent<ParkingData>(car) && foundSlot)
            {
                
                float3[] waypoints = GetParkingWaypoints(
                    _originBuildingNode.WorldPosition, 
                    parkingPos,
                    _roadNode.WorldPosition,
                    BuildingDirection,
                    Size
                );
                
                TestParkingWaypoints.AddRange(waypoints);

                BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
                ref BlobArray<float3> parkingWaypointBlob = ref blobBuilder.ConstructRoot<BlobArray<float3>>();

                // Add waypoints to the Blob
                BlobBuilderArray<float3> blobBuilderArray = blobBuilder.Allocate(ref parkingWaypointBlob, waypoints.Length);
                for (int i = 0; i < waypoints.Length; i++)
                {
                    blobBuilderArray[i] = waypoints[i]; 
                }

                BlobAssetReference<BlobArray<float3>> waypointsBlob = blobBuilder.CreateBlobAssetReference<BlobArray<float3>>(Allocator.Persistent);

                ParkingData parkingData = new ParkingData
                {
                    WaypointsBlob = waypointsBlob,
                    CurrentIndex = 0,  
                    ParkingPos =  parkingPos,
                    HasPath = true     
                };

                EntityManager.SetComponentData(car, parkingData);
                blobBuilder.Dispose();
            }
            else
            {
                DebugUtility.LogWarning($"Parking request failed for entity {car}, no available slots or missing ParkingData.", this.name);
            }

            ParkingResquest.Dequeue();
        }

        #region Generate Parking Waypoints

        /// <summary>
        /// Use bezier curve to create waypoints to enter waypoint
        /// </summary>
        /// <param name="parkingPos"></param>
        /// <param name="roadPos"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public float3[] GetParkingWaypoints(Vector2 buildingPos, float3 parkingPos, Vector2 roadPos, BuildingDirection dir, ParkingLotSize size)
        {
            float oneHalfRadius = GridManager.NodeRadius * 3 / 2f;
            float halfRadius = GridManager.NodeRadius * 1 / 2f;
            
            Vector2 parkingPos2 = new Vector2(parkingPos.x, parkingPos.y);
         
            Vector2 buildingEntryDir = -(GetRoadNodeDirection(roadPos, buildingPos, dir, size)).normalized;
            
            Debug.Log(buildingEntryDir);

            Vector2 right = new Vector2(buildingEntryDir.y, -buildingEntryDir.x);
            
            BezierSpline spline = new BezierSpline(null, spacing, curveSmooth);

            Vector2 parkingEntryDir = Vector2.zero;

            if (dir == BuildingDirection.Down || dir == BuildingDirection.Up)
            {
                float dirMul = dir == BuildingDirection.Up ? -1 : 1;
 
                //Edge case, one road direction that don't need to invert
                if ((buildingEntryDir == Vector2.left && dir == BuildingDirection.Up) ||
                    (buildingEntryDir == Vector2.right && dir == BuildingDirection.Down))
                {
                    dirMul *= -1;
                }
                
                parkingEntryDir = Vector2.up * dirMul;
            }
            else
            {
                float dirMul = dir == BuildingDirection.Right  ? -1 : 1;
                
                //Edge case, one road direction that don't need to invert
                if ((buildingEntryDir == Vector2.up && dir == BuildingDirection.Right) ||
                    (buildingEntryDir == Vector2.down && dir == BuildingDirection.Left))
                {
                    //Reverse back
                    dirMul *= -1;
                }
                parkingEntryDir = Vector2.right  * dirMul;
            }
            
            Vector2 rightRoadPos = roadPos + right * RoadManager.QuarterWidth;
            Vector2 leftRoadPos = roadPos - right * RoadManager.QuarterWidth;
            
            //Avoid moving glicthly becase the end of path is the same as the start of new path
            spline.AddPoint(rightRoadPos + buildingEntryDir * halfRadius);
            spline.AddPoint( rightRoadPos + buildingEntryDir * oneHalfRadius);

            spline.AddPoint(parkingPos2 + parkingEntryDir * halfRadius);
            spline.AddPoint(parkingPos2);
            spline.AddPoint(parkingPos2 - parkingEntryDir * halfRadius);
            
            spline.AddPoint(leftRoadPos + buildingEntryDir * oneHalfRadius);
            //Avoid moving glicthly becase the end of path is the same as the start of new path
            spline.AddPoint(leftRoadPos +  buildingEntryDir * halfRadius);

            
            Vector3[] points = spline.GetEvenlySpacedPoints(spacing, curveSmooth);
            float3[]  waypoints= new  float3[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                waypoints[i] = points[i];
            }

            return waypoints;
        }
        
        /// <summary>
        ///Instead of using get neighbours list of road node, we compare Y-axis or X-axis of roadPos to the origin node
        ///to decouple from GridManager (for testing majorly)
        ///Return vector2.left if the road is on the left of parking node
        /// NOTICE: The direction is WORLD DIRECTION (not perspective direction)
        /// </summary>
        /// <param name="roadPos"></param>
        /// <param name="buildingPos"></param>
        /// <param name="direction"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private Vector2 GetRoadNodeDirection(Vector2 roadPos, Vector2 buildingPos, BuildingDirection direction, ParkingLotSize size)
        {
            float nodeRadius = GridManager.NodeRadius;
            if (size == ParkingLotSize._1x1)
            {
                return (roadPos - buildingPos).normalized;
            }
            else
            {
                if (direction == BuildingDirection.Up || direction == BuildingDirection.Down)
                {
                    if (roadPos.x > buildingPos.x)
                    {
                        return Vector2.right;
                    }
                    if (roadPos.x < buildingPos.x && buildingPos.x  - roadPos.x > 2 * nodeRadius)
                    {
                        return Vector2.left;
                    }
                    return direction == BuildingDirection.Up? Vector2.up: Vector2.down;
                }
                if (direction == BuildingDirection.Right || direction == BuildingDirection.Left)
                {
                    if (roadPos.y > buildingPos.y && roadPos.y - buildingPos.y > 2f * nodeRadius)
                    {
                        return Vector2.up;
                    }
                
                    if (roadPos.y < buildingPos.y)
                    {
                        return Vector2.down;
                    }
                    
                    return direction == BuildingDirection.Right? Vector2.right : Vector2.left;
                }
            }
               
            return Vector2.zero;
        }
        
        #endregion
        
        
       #if UNITY_EDITOR  
        private void PrintWaypoints(float3[] waypoints)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                  DebugUtility.Log($"{i+1}. {waypoints[i]}", this.ToString());
            }
        }
        #endif

        #region Debug
        protected virtual void OnDrawGizmos()
        {
            if (ParkingNodes == null || _originBuildingNode == null || TestParkingWaypoints == null || !isGizmos)
            {
                return;
            }

            Gizmos.color = Color.red;

            if (drawWaypoints)
            {
                foreach (var waypoint in ParkingNodes)
                {
                    Gizmos.DrawWireSphere(waypoint.WorldPosition, 0.5f);
                }
            }

            Gizmos.color = Color.red;
            foreach (var node in TestParkingWaypoints)
            {
                Gizmos.DrawSphere(node, 0.05f);
            }

            if (drawInfo)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_roadNode .WorldPosition, 0.5f);
            
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_originBuildingNode.WorldPosition, 0.5f);

                if (this._isConnected)
                {
                    Handles.Label(new Vector3(transform.position.x, transform.position.y, transform.position.z), "Connected", new GUIStyle { fontSize = 16, normal = { textColor = Color.green } });
                }
                else
                {
                    Handles.Label(new Vector3(transform.position.x, transform.position.y, transform.position.z), "Unconnected", new GUIStyle { fontSize = 16, normal = { textColor = Color.red } });

                }
            }
        }

        public string Name
        {
            get
            {
                return  "Building";
            }
        }

        private void SetUpDebugFlag()
        {
            debugButtonMap = new Dictionary<DebugMenu.DebugFlag, bool>();
            debugButtonMap.Add(DebugMenu.DebugFlag.BuildingNode, drawInfo);
            debugButtonMap.Add(DebugMenu.DebugFlag.BuildingWaypoint, drawWaypoints);
        }

        public void ToggleDebug(DebugMenu.DebugFlag flag , bool enabled)
        {
            if (!debugButtonMap.ContainsKey(flag) || debugButtonMap == null)
            {
                return;
            }
            debugButtonMap[flag] = enabled;
        }

        public void TurnOffAll(bool enabled)
        {
            isGizmos = enabled;
        }

        public Dictionary<DebugMenu.DebugFlag, bool> GetDebugFlags()
        {
            return debugButtonMap;
        }
        
        #endregion
    }
    
    
}
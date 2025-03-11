using System.Collections.Generic;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS;
using Game._00.Script._03.Traffic_System.Mesh_Generator;
using Game._00.Script._03.Traffic_System.Road;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEditor;
using UnityEngine;


namespace Game._00.Script._03.Traffic_System.Building
{
    public enum BuildingType
    {
        BusinessRed,
        BusinessBlue,
        BusinessYellow,
        HomeRed,
        HomeYellow,
        HomeBlue,
        
    }

    /// <summary>
    /// Note: keep the order exactly this because JSON databased on this to convert
    /// </summary>
    public enum BuildingDirection
    {
        Left,
        Right,
        Up,
        Down,
    }

    public enum ParkingLotSize
    {
        _1x1,
        _2x2,
        _2x3
    }
  
    public abstract class BuildingBase : MonoBehaviour
    {
        [Header("Default building settings")] 
        [SerializeField] private bool isGizmos;
        [SerializeField] private BuildingSpriteCollection spriteCollections;
        public BuildingSpriteCollection SpriteCollections
        {
            get { return spriteCollections; }
        }
        protected RoadManager RoadManager;
        protected BuildingManager BuildingManager;
        protected EntityManager EntityManager;

        
        protected Vector2 _worldPosition;

        private bool _isConnected;

        public bool IsConnected
        {
            get{ return _isConnected; }
            set {_isConnected = value;}
        }
        private List<Node> _parkingNodes;

        public List<Node> ParkingNodes
        {
            get {return _parkingNodes;}  
            set {_parkingNodes = value;}
        }

        protected List<float3> TestParkingWaypoints;

        public List<ParkingLot> ParkingPos; //Parking lots positions
        
        protected float3 _centerPos;

        public float3 CenterPos
        {
            set { _centerPos = value; }
        }
        
        protected Node _originBuildingNode;
        public Node OriginBuildingNode
        {
            get { return _originBuildingNode; }
        }
        
        protected Node _roadNode;
        public Node RoadNode
        {
          get { return _roadNode; }
          set { _roadNode = value; }
        }

        public Vector2 WorldPosition
        {
            get { return _worldPosition; }
            set { _worldPosition = value; }
        }

        protected Queue<Entity> ParkingResquest;
        private ParkingMesh _parkingMesh;
    
        public BuildingType BuildingType { get; private set; }  // Make it a property
        [SerializeField] protected float lifeTime = 2f;
        [SerializeField] public ParkingLotSize size = ParkingLotSize._1x1;
        public BuildingDirection BuildingDirection { get; private set; }

        public virtual void Initialize(Node node, BuildingType buildingType, BuildingDirection direction,Vector2 worldPosition)
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _parkingMesh = FindObjectOfType<ParkingMesh>();
            this.RoadManager = FindObjectOfType<RoadManager>();
            this.BuildingManager = FindObjectOfType<BuildingManager>();
            
            this.BuildingDirection = direction;
            this.BuildingType = buildingType;
            this._worldPosition = worldPosition;
            
            this._originBuildingNode = GridManager.NodeFromWorldPosition(worldPosition);

            _parkingNodes = new List<Node>();
            ParkingPos = new List<ParkingLot>();
            
            //After finish initialize parking lots, initlize bool[] to track if the parking lot is available
            ParkingResquest = new Queue<Entity>();
            
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
        /// Recieve a parking request, if available, create waypoints in parking lot
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
                    BuildingDirection,
                    size,
                    parkingPos,
                    new float3(_centerPos),
                    _roadNode.WorldPosition
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
        
        
         /// <summary>
         /// Get way points to direct the car to park following these rules:
         /// 1/ Always go from the right of a lane. If it is an outward lane, it = 1/4f Node radius (divided) Radius 2 lane, road. If it inward lane (lane close to building), it = 1/2 Node radius, 1 lane road
         /// 2/ Bot corner = corner close to the street, top corner = corner close to the building. Each = 1/2 node radius
         /// 3/ Normally, go from bot corner to parking lot to top corner, there is 1 special situation on each buildingDirection that it reverses the route, from top to bot
         /// 4/ Step in, step out corner is to make sure to set car in the right lane of road outside, transStep is transition step between each corner and step in, step out
         /// 5/ Parking Pos 
         /// </summary>
         /// <param name="buildingDirection>
         /// <param name="sizeingLotSize"></param>
         /// <param name="parkingPos"></param>
         /// <param name="roadNode"></param>
         /// <param name = "center point"></param> in the center of vertical (or horizontal if Right,Left) of parking (2 nodes)
         /// <returns></returns>
         public float3[] GetParkingWaypoints(Vector2 originPos, BuildingDirection buildingDirection, ParkingLotSize size, float3 parkingPos, float3 centerPoint, Vector2 roadPos)
         {
             float nodeRadius = GridManager.NodeRadius;
             float roadWidth = RoadManager.RoadWidth;
            Vector2 roadDirection = GetRoadNodeDirection(roadPos, originPos, buildingDirection, size);

            var inOutSteps = GetInOutCorner(roadPos, roadDirection);
            float3 inCorner = inOutSteps.Item1;
            float3 outCorner = inOutSteps.Item2;
            
            var botTopCorners = GetBotTopCorner(originPos, size, buildingDirection);
            float3 botCorner = botTopCorners.Item1;
            float3 topCorner = botTopCorners.Item2;

            if (size == ParkingLotSize._1x1)
            {
                float3 center = new float3(originPos.x, originPos.y, 0);

                if (buildingDirection == BuildingDirection.Up || buildingDirection == BuildingDirection.Down)
                {
                    float directionMultipler = buildingDirection == BuildingDirection.Up ? 1 : -1;
                    float3 right = new float3(originPos.x - directionMultipler * roadWidth/4f, originPos.y, 0);
                    float3 left = new float3(originPos.x + directionMultipler * roadWidth/4f, originPos.y, 0);
                    return new []{right, center, left};
                }
                
                if (buildingDirection == BuildingDirection.Right || buildingDirection == BuildingDirection.Left)
                {
                    float directionMultipler = buildingDirection == BuildingDirection.Right ? 1 : -1;
                    float3 top = new float3(originPos.x, originPos.y + directionMultipler * roadWidth/4f, 0);
                    float3 bot = new float3(originPos.x, originPos.y - directionMultipler * roadWidth/4f, 0);
                    return new []{top, center, bot};
                }
            }
            else //More complicated building complex
            {
                if (buildingDirection == BuildingDirection.Down || buildingDirection == BuildingDirection.Up)
                {
                    float directionMultipler = buildingDirection == BuildingDirection.Up ? 1 : -1;

                    float3 botParking = new float3(parkingPos.x,
                        centerPoint.y + directionMultipler * nodeRadius * 1 / 4f, 0);
                    float3 topParking = new float3(parkingPos.x, topCorner.y, 0);

                    float3 inTransStep = new float3(inCorner.x, botCorner.y, 0);
                    float3 outTransStep = new float3(outCorner.x, botParking.y, 0);

                    //Skip bot corner
                    if ((buildingDirection == BuildingDirection.Up && roadDirection == Vector2.right) ||
                        (buildingDirection == BuildingDirection.Down && roadDirection == Vector2.left))
                    {
                        inTransStep = new float3(inCorner.x, topCorner.y, 0);
                        botParking.y = centerPoint.y + directionMultipler * nodeRadius * 1 / 2f;
                        outTransStep = new float3(outCorner.x, botParking.y, 0);

                        return new[]
                        {
                            inCorner, inTransStep, topCorner, topParking, parkingPos, botParking, outTransStep,
                            outCorner
                        };
                    }

                    //Reverse root
                    if ((buildingDirection == BuildingDirection.Up && roadDirection == Vector2.left) ||
                        (buildingDirection == BuildingDirection.Down && roadDirection == Vector2.right))
                    {
                        //Move bot corner x to the opposite side
                        botCorner.x += directionMultipler * nodeRadius * 3;

                        //Update bot, top parking && in,out trans step after changing bot corner
                        botParking.y = botCorner.y;
                        inTransStep = new float3(inCorner.x, botCorner.y, 0);
                        outTransStep = new float3(outCorner.x, topParking.y, 0);

                        return new[]
                        {
                            inCorner, inTransStep, botCorner, botParking, parkingPos, topParking, outTransStep,
                            outCorner
                        };

                    }

                    //Normally
                    return new[]
                    {
                        inCorner, inTransStep, botCorner, topCorner, topParking, parkingPos, botParking, outTransStep,
                        outCorner
                    };

                }
                if (buildingDirection == BuildingDirection.Right || buildingDirection == BuildingDirection.Left)
                {
                    float directionMultipler = buildingDirection == BuildingDirection.Right ? 1 : -1;
                    float3 topParking = new float3(topCorner.x, parkingPos.y, 0);
                    float3 botParking = new float3(centerPoint.x + directionMultipler * nodeRadius * 1 / 4f,
                        parkingPos.y, 0);

                    float3 inTransStep = new float3(botCorner.x, inCorner.y, 0);
                    float3 outTransStep = new float3(botParking.x, outCorner.y, 0);

                    //Skip bot corner because it has inCorner.x > botCorner.x
                    if ((roadDirection == Vector2.up && buildingDirection == BuildingDirection.Left) ||
                        (roadDirection == Vector2.down && buildingDirection == BuildingDirection.Right))
                    {
                        //Set bot parking && bot corner to the left side of lane
                        botCorner.x = centerPoint.x + directionMultipler * nodeRadius * 1 / 2f;
                        botParking.x = botCorner.x;

                        //Update out and in trans step: inTranStep, base on topCorner
                        inTransStep = new float3(topCorner.x, inCorner.y, 0);
                        outTransStep = new float3(botParking.x, outCorner.y, 0);

                        return new[]
                        {
                            inCorner, inTransStep, topCorner, topParking, parkingPos, botParking, outTransStep,
                            outCorner
                        };
                    }

                    if ((roadDirection == Vector2.down && buildingDirection == BuildingDirection.Left) ||
                        (roadDirection == Vector2.up && buildingDirection == BuildingDirection.Right))
                    {
                        //Move y-axis of bot corner
                        botCorner = new float3(centerPoint.x + directionMultipler * nodeRadius * 3 / 4f,
                            parkingPos.y - directionMultipler * nodeRadius * 3 / 2f, 0);
                        botParking.x = botCorner.x;

                        //Re-calculate in/out trans
                        inTransStep = new float3(botCorner.x, inCorner.y, 0);
                        outTransStep = new float3(topParking.x, outCorner.y, 0);

                        return new[]
                        {
                            inCorner, inTransStep, botCorner, botParking, parkingPos, topParking, outTransStep,
                            outCorner
                        };
                    }

                    //Normal
                    return new[]
                    {
                        inCorner, inTransStep, botCorner, topCorner, topParking, parkingPos, botParking, outTransStep,
                        outCorner
                    };
                }
            }

            return new[] { float3.zero };
            
            //Instead of using get neighbours list of road node, we compare Y-axis or X-axis of roadPos to the origin node
            //to decouple from GridManager (for testing majorly)
            //Return vector2.left if the road is on the left of parking node 
            Vector2 GetRoadNodeDirection(Vector2 roadPos, Vector2 buildingPos, BuildingDirection direction, ParkingLotSize size)
            {
                float nodeRadius = GridManager.NodeRadius;
                if (size == ParkingLotSize._1x1)
                {
                    Vector2 dir = roadPos - buildingPos;
                    if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                    {
                        return dir.x > 0 ? Vector2.left : Vector2.right;
                    }
                    else
                    {
                        return dir.y > 0 ? Vector2.down : Vector2.up;
                    }

                }
                else
                {
                    if (direction == BuildingDirection.Up || direction == BuildingDirection.Down)
                    {
                        if (roadPos.x > buildingPos.x)
                        {
                            return Vector2.left;
                        }
                        if (roadPos.x < buildingPos.x && buildingPos.x  - roadPos.x > 2 * nodeRadius)
                        {
                            return Vector2.right;
                        }
                        return direction == BuildingDirection.Up? Vector2.down : Vector2.up;
                    }
                    if (direction == BuildingDirection.Right || direction == BuildingDirection.Left)
                    {
                        if (roadPos.y > buildingPos.y && roadPos.y - buildingPos.y > 2f * nodeRadius)
                        {
                            return Vector2.down;
                        }
                
                        if (roadPos.y < buildingPos.y)
                        {
                            return Vector2.up;
                        }
                        return direction == BuildingDirection.Right? Vector2.left : Vector2.right;
                    }
                }
               
                return Vector2.zero;
            }
            
            //Get stepInCorner and stepOutCorner
            
            (float3, float3)GetBotTopCorner(Vector3 originPos, ParkingLotSize size, BuildingDirection buildingDirection)
            {
                float sizeMultipler = size == ParkingLotSize._2x2 ? 1 : 2;
                float rightOffset = GridManager.NodeRadius * 3 / 4f; //To set the bot corner on the right of lanes when car enter
                float normalOffset = GridManager.NodeRadius * 1 / 2f;
                float nodeDiameter = GridManager.NodeDiameter;


                return buildingDirection switch
                {   
                    BuildingDirection.Up => (
                        new float3(originPos.x - nodeDiameter - rightOffset, originPos.y + sizeMultipler * nodeDiameter + rightOffset, 0),
                        new float3(originPos.x - nodeDiameter - rightOffset, originPos.y + sizeMultipler * nodeDiameter - normalOffset, 0)
                    ),
                    BuildingDirection.Down => (
                        new float3(originPos.x + rightOffset, originPos.y - sizeMultipler * nodeDiameter - rightOffset, 0),
                        new float3(originPos.x + rightOffset, originPos.y - sizeMultipler * nodeDiameter + normalOffset, 0)
                    ),
                    BuildingDirection.Right => ( //this have Y higher than originPos.y
                            new float3(originPos.x + sizeMultipler * nodeDiameter + rightOffset, originPos.y + nodeDiameter + normalOffset, 0),
                            new float3(originPos.x + sizeMultipler * nodeDiameter - normalOffset, originPos.y + nodeDiameter + normalOffset, 0)
                        ),
                    BuildingDirection.Left => ( //this have Y lower than originPos.y
                            new float3(originPos.x - sizeMultipler * nodeDiameter - rightOffset, originPos.y - normalOffset, 0),
                            new float3(originPos.x - sizeMultipler * nodeDiameter + normalOffset, originPos.y - normalOffset, 0)
                        ),
                    _ => (float3.zero, float3.zero)
                };

            }
            
            (float3, float3) GetInOutCorner(Vector2 roadPos, Vector2 roadDirection)
            {
                float roadWidth =  RoadManager.RoadWidth;
                float nodeRadius = GridManager.NodeRadius;

                float offsetX = roadWidth / 4f;
                float offsetY = nodeRadius * 5 / 4f;

                return roadDirection switch
                {
                    Vector2 dir when dir == Vector2.down => (
                        new float3(roadPos.x - offsetX, roadPos.y - offsetY, 0),
                        new float3(roadPos.x + offsetX, roadPos.y - offsetY, 0)
                    ),

                    Vector2 dir when dir == Vector2.up => (
                        new float3(roadPos.x + offsetX, roadPos.y + offsetY, 0),
                        new float3(roadPos.x - offsetX, roadPos.y + offsetY, 0)
                    ),

                    Vector2 dir when dir == Vector2.left => (
                        new float3(roadPos.x - offsetY, roadPos.y + offsetX, 0),
                        new float3(roadPos.x - offsetY, roadPos.y - offsetX, 0)
                    ),

                    Vector2 dir when dir == Vector2.right => (
                        new float3(roadPos.x + offsetY, roadPos.y - offsetX, 0),
                        new float3(roadPos.x + offsetY, roadPos.y + offsetX, 0)
                    ),

                    _ => (float3.zero, float3.zero),
                };
            }
         }
       #if UNITY_EDITOR  
        private void PrintWaypoints(float3[] waypoints)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                  DebugUtility.Log($"{i+1}. {waypoints[i]}", this.ToString());
            }
        }
        #endif
        private void OnDrawGizmos()
        {
            if (ParkingNodes == null || _originBuildingNode == null || TestParkingWaypoints == null || !isGizmos)
            {
                return;
            }
            Gizmos.color = Color.yellow;
            foreach (var waypoint in ParkingNodes)
            {
                Gizmos.DrawWireSphere(waypoint.WorldPosition, 0.5f);
            }

            Gizmos.color = Color.yellow;
            foreach (var node in TestParkingWaypoints)
            {
                Gizmos.DrawSphere(node, 0.05f);
            }
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_roadNode.WorldPosition, 0.5f);
            
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
    
    
}
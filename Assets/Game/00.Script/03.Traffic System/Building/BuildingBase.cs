using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS;
using Game._00.Script._03.Traffic_System.Mesh_Generator;
using Game._00.Script._03.Traffic_System.Road;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game._00.Script._03.Traffic_System.Building
{
    public enum BuildingType
    {
        Heart,
        Lung,
        NormalCell
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
        
        //Test variables:
        private List<Vector3> _test_Waypoints;
        private float3 _parkingPoint;
        
        private RoadManager _roadManager;
        private EntityManager _entityManager;
        
        protected Vector2 _worldPosition;
        
        private Node _originBuildingNode;
       
        private List<Node> _parkingNodes;

        public List<Node> ParkingNodes
        {
            get {return _parkingNodes;}  
        }
        
        
        public Node OriginBuildingNode
        {
            get { return _originBuildingNode; }
        }

        private Node _roadNode;

        public Node RoadNode
        {
            get{return _roadNode;}
        }

        public Vector2 WorldPosition
        {
            get { return _worldPosition; }
            set { _worldPosition = value; }
        }
        
        private Queue<Entity> _parkingResquest = new Queue<Entity>();
        private bool[] _availableParking;
        
        private ParkingMesh _parkingMesh;
        private TestSaver _testSaver;
    
        public BuildingType BuildingType { get; private set; }  // Make it a property
        [SerializeField] protected float lifeTime = 2f;
        [SerializeField] public ParkingLotSize parkingLotSize = ParkingLotSize._1x1;
        public BuildingDirection BuildingDirection { get; private set; }
        public void Initialize (Node node, BuildingType buildingType, Vector2 worldPosition)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _parkingMesh = FindObjectOfType<ParkingMesh>();
            _testSaver = GameManager.Instance.TestSaver;
            
            //Random between horizontal and vertical building
            float random = Random.Range(0f, 1f);
            bool isHorizontal = random <= 0.5f;
            
            this._roadManager = FindObjectOfType<RoadManager>();
            this.BuildingType = buildingType;
            this._worldPosition = worldPosition;
            this._originBuildingNode = GridManager.NodeFromWorldPosition(worldPosition);

            _parkingNodes = new List<Node>();
            BuildingDirection[] directionTypes = new[] { BuildingDirection.Down, BuildingDirection.Up, BuildingDirection.Left,BuildingDirection.Right };
            int randomIndex = Random.Range(1, directionTypes.Length);
            BuildingDirection = BuildingDirection.Right;
            
            //This has to be called first to set up for the next function, save parking nodes to set adj list to road nodes later
            SetBuildingAndInsideRoads(node, parkingLotSize,BuildingDirection);
            // Invoke("DeactivateBuilding", LifeTime);
            int testIndex =0;
            SpawnRoad(node, parkingLotSize, BuildingDirection, testIndex);
            
            //After finish initialize parking lots, initlize bool[] to track if the parking lot is available
            _parkingResquest = new Queue<Entity>();
            _availableParking = new bool[_parkingNodes.Count];
            
            float3 centerPoint= new float3(_originBuildingNode.WorldPosition.x + GridManager.NodeDiameter, _originBuildingNode.WorldPosition.y + GridManager.NodeRadius, 0f);
            _parkingPoint = centerPoint;
            float3[] waypoints = GetParkingWaypoints(OriginBuildingNode.WorldPosition,BuildingDirection, parkingLotSize, _parkingPoint, centerPoint,_roadNode.WorldPosition);   
            _test_Waypoints = new List<Vector3>();
            foreach (float3 waypoint in waypoints)
            {
                _test_Waypoints.Add(new Vector3(waypoint.x, waypoint.y, waypoint.z));
            }
            Debug.Log(waypoints.Length);
            
        }

        private void DeactivateBuilding()
        {
            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// Set building (unWalkable, not empty node), based on parkingLotSize and buildingDirection.
        /// BitwiseDirection = right => building on left, road on right.
        /// Directions are limited to [Up, Down, Left, Right].
        /// </summary>
        /// <param name="originalBuildingNode"></param>
        /// <param name="parkingLotSize"></param>
        /// <param name="buildingDirection"></param>
        private void SetBuildingAndInsideRoads(Node originalBuildingNode, ParkingLotSize parkingLotSize, BuildingDirection buildingDirection)
        {
            Vector2 position = originalBuildingNode.WorldPosition;
            float nodeDiameter = GridManager.NodeDiameter;

            (List<Vector2>, List<Vector2>) GetBuildingWalkableOffsets(ParkingLotSize size, BuildingDirection dir)
            {
                List<Vector2> buildingOffsets = new();
                List<Vector2> walkableOffsets = new();
                
                if (size == ParkingLotSize._1x1)
                {
                    buildingOffsets.Add(Vector2.zero); // Single node for 1x1
                   walkableOffsets.Add(Vector2.zero);
                }
                else if (size == ParkingLotSize._2x2)
                {
                    if (dir == BuildingDirection.Up || dir == BuildingDirection.Down) //Basically, the second building node spawned on the left of original node
                    {
                        float directionMultiplier = dir == BuildingDirection.Up ? 1 : -1;
                        buildingOffsets.AddRange(new[] { Vector2.zero, new Vector2(-nodeDiameter, 0) });
                        walkableOffsets.AddRange( new[] {new Vector2(-nodeDiameter, nodeDiameter * directionMultiplier), new Vector2(0, nodeDiameter * directionMultiplier)});
                    }
                    else // Left or Right
                    {
                        float directionMultiplier = dir == BuildingDirection.Right ? 1 : -1; //Basically, the second building node spawned on the top of original node
                        buildingOffsets.AddRange(new[] { Vector2.zero, new Vector2(0, nodeDiameter) });
                        walkableOffsets.AddRange( new[] {new Vector2(nodeDiameter * directionMultiplier, nodeDiameter), new Vector2(nodeDiameter * directionMultiplier, 0)});
                    }
                }
                else if (size == ParkingLotSize._2x3)
                {
                    if (dir == BuildingDirection.Up || dir == BuildingDirection.Down)
                    {
                        float directionMultiplier = dir == BuildingDirection.Up ? 1 : -1;
                        buildingOffsets.AddRange(new[]
                        {
                            Vector2.zero,
                            new Vector2(-nodeDiameter, 0),
                            new Vector2(0, nodeDiameter * directionMultiplier),
                            new Vector2(-nodeDiameter, nodeDiameter * directionMultiplier)
                        });
                        
                        walkableOffsets.AddRange(new[]
                        {
                            new Vector2(0, nodeDiameter * directionMultiplier * 2),
                            new Vector2(-nodeDiameter, nodeDiameter * directionMultiplier*2)
                        });
                    }
                    else // Left or Right
                    {
                        float directionMultiplier = dir == BuildingDirection.Right ? 1 : -1;
                        buildingOffsets.AddRange(new[]
                        {
                            Vector2.zero,
                            new Vector2(nodeDiameter * directionMultiplier, 0),
                            new Vector2(0, nodeDiameter),
                            new Vector2(nodeDiameter * directionMultiplier, nodeDiameter)
                        });
                        walkableOffsets.AddRange(new[]
                        {
                            new Vector2(nodeDiameter * directionMultiplier * 2 , 0),
                            new Vector2(nodeDiameter * directionMultiplier * 2, nodeDiameter)
                        });
                    }
                }
                return (buildingOffsets, walkableOffsets);
            }

            // Iterate through calculated offsets and apply building settings
            List<Vector2> buildingOffsets = GetBuildingWalkableOffsets(parkingLotSize, buildingDirection).Item1;
            foreach (Vector2 offset in buildingOffsets)
            {
                Node buildingNode = GridManager.NodeFromWorldPosition(position + offset);
                buildingNode.SetBuilding(true);
                buildingNode.SetWalkable(false);
                _parkingMesh.PlaceBuildingMesh(originalBuildingNode, parkingLotSize, buildingDirection);
            }
            
            List<Vector2> walkableOffsets = GetBuildingWalkableOffsets(parkingLotSize, buildingDirection).Item2;
            foreach (Vector2 offset in walkableOffsets)
            {
                //Set this like a road with RoadManager, set but not create mesh
                Node insideRoadNode = GridManager.NodeFromWorldPosition(position + offset);
               
                _parkingNodes.Add(insideRoadNode);
                insideRoadNode.SetBelongedBuilding(this.gameObject);
                
                insideRoadNode.SetEmpty(false);
                insideRoadNode.SetWalkable(true);

                //Because the 1x1 single house, the car can go inside the house
                if (parkingLotSize != ParkingLotSize._1x1)
                {
                    insideRoadNode.SetDrawable(false);
                }
                _roadManager.PlaceNode(insideRoadNode);
            }

            _roadManager.PlaceNode(originalBuildingNode);
            _parkingMesh.PlaceBuildingMesh(originalBuildingNode, parkingLotSize, buildingDirection );
        }
        
        /// <summary>
        /// Spawns a road node based on the parking lot size and buildingDirection.
        /// </summary>
        /// <param name="buildingNode"></param>
        /// <param name="parkingLotSize"></param>
        /// <param name="buildingDirection"></param>
        /// <param name="randomIndex"> [0,3] </param>
        private void SpawnRoad(Node buildingNode, ParkingLotSize parkingLotSize, BuildingDirection buildingDirection, int randomIndex)
        {
            Vector2 position = buildingNode.WorldPosition;
            float nodeDiameter = GridManager.NodeDiameter;
            
            // xMult, yMult must be positive, only useful when only change 1 offset, X or Y
            Vector2 GetOffset(BuildingDirection direction, float xMult, float yMult)
            {
                return direction switch
                {
                    BuildingDirection.Up => new Vector2(0, yMult * nodeDiameter),
                    BuildingDirection.Down => new Vector2(0, -yMult * nodeDiameter),
                    BuildingDirection.Right => new Vector2(xMult * nodeDiameter, 0),
                    BuildingDirection.Left => new Vector2(-xMult * nodeDiameter, 0),
                    _ => new Vector2(0, 0)
                };
            }
            
            if (parkingLotSize == ParkingLotSize._1x1)
            {
                Vector2 offset = GetOffset(buildingDirection, 1, 1); // Same multiplier for _1x1
                Node roadNode = GridManager.NodeFromWorldPosition(position + offset);

                _roadNode = roadNode;
                _roadNode.SetBelongedBuilding(this.gameObject);
                
                _roadManager.PlaceNode(roadNode);
                _roadManager.SetAdjList(roadNode, buildingNode);
                _roadManager.CreateMesh(roadNode);
            }
            else if (parkingLotSize == ParkingLotSize._2x2 || parkingLotSize == ParkingLotSize._2x3)
            {
                float maxMultipler = parkingLotSize == ParkingLotSize._2x2 ? 2 : 3; //Multiply with node Diameter

                Vector2[] offsets;
                // Define possible offsets for _2x2 based on random ranges
                if( buildingDirection == BuildingDirection.Up || buildingDirection == BuildingDirection.Down)
                {
                    float directionMultipler = buildingDirection ==BuildingDirection.Up ? 1 : -1;
                    offsets = new[]
                    {
                        new Vector2(nodeDiameter, nodeDiameter * (maxMultipler -1) * directionMultipler),
                        new Vector2(0, nodeDiameter * directionMultipler * maxMultipler),
                        new Vector2(- nodeDiameter, nodeDiameter * directionMultipler * maxMultipler),
                        new Vector2(-2 * nodeDiameter, nodeDiameter * (maxMultipler -1) * directionMultipler)
                    };
                }else if (buildingDirection ==  BuildingDirection.Left || buildingDirection ==  BuildingDirection.Right)
                {
                    float directionMultipler = buildingDirection == BuildingDirection.Right ? 1 : -1;
                   offsets = new[]
                    {
                        new Vector2(directionMultipler * nodeDiameter * (maxMultipler - 1), 2 * nodeDiameter),
                        new Vector2(directionMultipler * nodeDiameter * maxMultipler, nodeDiameter),
                        new Vector2(directionMultipler * nodeDiameter * maxMultipler, 0),
                        new Vector2(directionMultipler * nodeDiameter * (maxMultipler - 1), -nodeDiameter),
                    };

                }
                else
                {
                    offsets = new Vector2[] { };
                }
                
                //Wraparound make sure that randomIndex in [0,3], non-negative number
                randomIndex = (randomIndex % 4 + 4) % 4;
                Vector2 chosenOffset = offsets[randomIndex];
                Node roadNode = GridManager.NodeFromWorldPosition(position + chosenOffset);
                
                
                //Get buildingDirection of a road;
                BitwiseDirection GetRoadDirection(Node roadNode, List<Node> parkingNodes, BuildingDirection direction)
                {
                    float roadX = roadNode.WorldPosition.x;
                    float roadY = roadNode.WorldPosition.y;

                    float parking1X = _parkingNodes[0].WorldPosition.x;
                    float parking2X = _parkingNodes[1].WorldPosition.x;
                
                    float parking1Y = _parkingNodes[0].WorldPosition.y;
                    float parking2Y = _parkingNodes[1].WorldPosition.y;

                    //Check perpendicular case
                    if (Mathf.Approximately(roadX, parking1X)) //Left and right
                    { 
                        return (roadY > parking1Y && roadY > parking2Y) ? BitwiseDirection.Bottom : BitwiseDirection.Up;
                    
                    }
                    if (Mathf.Approximately(roadY, parking2Y))
                    {
                        return (roadX > parking1X && roadX > parking2X) ? BitwiseDirection.Left : BitwiseDirection.Right;
                    }
                    
                    //Check same buildingDirection case => return = buildingDirection
                        
                    return direction switch
                    {
                        BuildingDirection.Up => BitwiseDirection.Bottom,
                        BuildingDirection.Down => BitwiseDirection.Up,
                        BuildingDirection.Left => BitwiseDirection.Right,
                        BuildingDirection.Right => BitwiseDirection.Left,
                    };
                }
                _roadNode = roadNode;
                _roadNode.SetBelongedBuilding(this.gameObject);
                _roadManager.PlaceNode(roadNode);

                //Get the closest node to the road node, set it to drawable to make it connect to the road node
                float minDst = float.MaxValue;
                Node closestNode = null;
                foreach (Node parkingNode in _parkingNodes)
                {
                    _roadManager.SetAdjList(roadNode, parkingNode);
                    float dst = Vector2.Distance(roadNode.WorldPosition, parkingNode.WorldPosition);
                    if (dst < minDst)
                    {
                        minDst = dst;
                        closestNode = parkingNode;
                    }
                }
                
                //Set closest walkable node to drawable
                if (closestNode != null)
                {
                    closestNode.SetDrawable(true);
                }
                
                _roadManager.CreateMesh(roadNode, GetRoadDirection(roadNode, _parkingNodes, buildingDirection)); 
             }
           
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

            int wayPointsCount = 6; 
            // Top corner line ->same x(y) with parking node -> parking node -> same x(y) with parking node -> left Bot corner (near road)
            // Every car have to step to their begin step first, then find the first corner later
            _parkingResquest.Enqueue(car);
            
            //Check if is any slot available
            bool isAvailable = false;
            int slotIndex = -1;
            for (int i = 0; i < _availableParking.Length; i++)
            {
                if (_availableParking[i])
                {
                    isAvailable = true;
                    slotIndex = i;
                    break;
                }
            }

            if (isAvailable && slotIndex != -1 && _entityManager.HasBuffer<ParkingWaypoints>(car))
            {

                DynamicBuffer<ParkingWaypoints> buffer = _entityManager.GetBuffer<ParkingWaypoints>(car);
                int parkingIndex = slotIndex + 1; //Convert base 0 to base 1
                float3[] waypoints = new float3[wayPointsCount];

            }
        }

        #region Test
         /// <summary>
         /// Get way points to direct the car to park following these rules:
         /// 1/ Always go from the right of a lane. If it is an outward lane, it = 1/4f Node radius (divided) Radius 2 lane, road. If it inward lane (lane close to building), it = 1/2 Node radius, 1 lane road
         /// 2/ Bot corner = corner close to the street, top corner = corner close to the building. Each = 1/2 node radius
         /// 3/ Normally, go from bot corner to parking lot to top corner, there is 1 special situation on each buildingDirection that it reverses the route, from top to bot
         /// 4/ Step in, step out corner is to make sure to set car in the right lane of road outside, transStep is transition step between each corner and step in, step out
         /// 5/ Parking Pos 
         /// </summary>
         /// <param name="buildingDirection>
         /// <param name="parkingLotSize"></param>
         /// <param name="parkingPos"></param>
         /// <param name="roadNode"></param>
         /// <param name = "center point"></param> in the center of vertical (or horizontal if Right,Left) of parking (2 nodes)
         /// <returns></returns>
         public float3[] GetParkingWaypoints(Vector2 originPos, BuildingDirection buildingDirection, ParkingLotSize parkingLotSize, float3 parkingPos, float3 centerPoint, Vector2 roadPos)
         {
             float nodeRadius = GridManager.NodeRadius;
            Vector2 roadDirection = GetRoadNodeDirection(roadPos, originPos, buildingDirection, parkingLotSize);

            var inOutSteps = GetInOutCorner(roadPos, roadDirection);
            float3 inCorner = inOutSteps.Item1;
            float3 outCorner = inOutSteps.Item2;
            
            var botTopCorners = GetBotTopCorner(originPos, parkingLotSize, buildingDirection);
            float3 botCorner = botTopCorners.Item1;
            float3 topCorner = botTopCorners.Item2;

            if (buildingDirection == BuildingDirection.Down || buildingDirection == BuildingDirection.Up)
            {
                float directionMultipler = buildingDirection == BuildingDirection.Up ? 1 : -1;
                float3 botParking = new float3(parkingPos.x, centerPoint.y + directionMultipler * nodeRadius* 1/4f, 0);
                float3 topParking = new float3(parkingPos.x, topCorner.y, 0);
                
                float3 inTransStep = new float3(inCorner.x, botCorner.y, 0);
                float3 outTransStep = new float3(outCorner.x,botParking.y, 0);

                //Skip bot corner
                if ((buildingDirection == BuildingDirection.Up && roadDirection == Vector2.right) ||
                    (buildingDirection ==BuildingDirection.Down && roadDirection == Vector2.left))
                {
                    inTransStep = new float3(inCorner.x, topCorner.y, 0);
                    botParking.y = centerPoint.y + directionMultipler * nodeRadius * 1/2f ;
                    outTransStep = new float3(outCorner.x, botParking.y, 0);

                    return new[]
                    {
                        inCorner, inTransStep, topCorner, topParking, parkingPos, botParking, outTransStep, outCorner
                    };
                }

                //Reverse root
                if ((buildingDirection == BuildingDirection.Up && roadDirection == Vector2.left) ||
                    (buildingDirection ==BuildingDirection.Down && roadDirection == Vector2.right))
                {
                    //Move bot corner x to the opposite side
                    botCorner.x += directionMultipler * nodeRadius * 3;
                    
                    //Update bot, top parking && in,out trans step after changing bot corner
                    botParking.y = botCorner.y;
                    inTransStep = new float3( inCorner.x,botCorner.y, 0);
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
                float3 botParking = new float3(centerPoint.x + directionMultipler * nodeRadius * 1/4f , parkingPos.y, 0);
                
                float3 inTransStep = new float3(botCorner.x, inCorner.y, 0);
                float3 outTransStep = new float3(botParking.x, outCorner.y, 0);

                //Skip bot corner because it has inCorner.x > botCorner.x
                if ((roadDirection == Vector2.up && buildingDirection == BuildingDirection.Left) || (roadDirection == Vector2.down && buildingDirection ==BuildingDirection.Right))
                {
                    //Set bot parking && bot corner to the left side of lane
                    botCorner.x = centerPoint.x + directionMultipler * nodeRadius * 1 / 2f;
                    botParking.x = botCorner.x;
                    
                    //Update out and in trans step: inTranStep, base on topCorner
                    inTransStep = new float3(topCorner.x, inCorner.y, 0);
                    outTransStep = new float3(botParking.x, outCorner.y, 0);

                    return new[]
                    {
                        inCorner, inTransStep, topCorner, topParking, parkingPos, botParking, outTransStep, outCorner
                    };
                }
                
                if ((roadDirection == Vector2.down && buildingDirection == BuildingDirection.Left) || (roadDirection == Vector2.up && buildingDirection == BuildingDirection.Right))
                {
                    //Move y-axis of bot corner
                    botCorner = new float3(centerPoint.x + directionMultipler * nodeRadius * 3/4f, parkingPos.y - directionMultipler * nodeRadius * 3/2f, 0);
                    botParking.x = botCorner.x;
                    
                    //Re-calculate in/out trans
                    inTransStep = new float3(botCorner.x, inCorner.y, 0);
                    outTransStep = new float3(topParking.x, outCorner.y, 0);

                    return new[]
                    {
                        inCorner, inTransStep, botCorner, botParking, parkingPos, topParking, outTransStep, outCorner
                    };
                }
                
                //Normal
                return new[]
                {
                    inCorner, inTransStep, botCorner, topCorner, topParking, parkingPos, botParking, outTransStep,
                    outCorner
                };
            }
            
            return new[] { float3.zero };
            
            //Instead of using get neighbours list of road node, we compare Y-axis or X-axis of roadPos to the origin node
            //to decouple from GridManager (for testing majorly)
            //Return vector2.left if the road is on the left of parking node 
            Vector2 GetRoadNodeDirection(Vector2 roadPos, Vector2 buildingPos, BuildingDirection direction, ParkingLotSize size)
            {
                float nodeRadius = GridManager.NodeRadius;
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

                
                //
                // Node roadNode = GridManager.NodeFromWorldPosition(roadPos);
                // List<Node> neighborus = roadNode.GetNeighbours();
                // foreach (Node n in neighborus)
                // {
                //     //Avoid diagonal roadNode into calculation
                //     if (n.BelongedBuilding == roadNode.BelongedBuilding && (Mathf.Approximately(n.WorldPosition.x, roadNode.WorldPosition.x) || Mathf.Approximately(n.WorldPosition.y, roadNode.WorldPosition.y)))
                //     {
                //         Vector2 dir = n.WorldPosition - roadNode.WorldPosition;
                //         if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                //         {
                //             return dir.x > 0 ? Vector2.right : Vector2.left;
                //         }
                //
                //         return dir.y > 0 ? Vector2.up : Vector2.down;
                //
                //     }
                // }
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
         
        private void PrintWaypoints(float3[] waypoints)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                  DebugUtility.Log($"{i+1}. {waypoints[i]}", this.ToString());
            }
        }
        private void OnDrawGizmos()
        {
            if (_test_Waypoints != null && _test_Waypoints.Count > 0)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < _test_Waypoints.Count-1; i++)
                {
                    Gizmos.DrawLine(_test_Waypoints[i], _test_Waypoints[i+1]);
                }
                
                Gizmos.color = Color.yellow;
                foreach (var waypoint in _test_Waypoints)
                {
                    Gizmos.DrawSphere(waypoint, 0.05f);
                }
                
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_parkingPoint, 0.05f);

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_roadNode.WorldPosition, 0.05f);
            }
        }

        #endregion
        
    }
}
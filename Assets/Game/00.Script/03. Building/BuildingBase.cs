using System;
using System.Collections.Generic;
using Game._00.Script._02._System_Manager;
using Game._00.Script._05._Manager;
using Game._00.Script._07._Mesh_Generator;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Game._00.Script._03._Building
{
    public enum ParkingLotSize //Used for create mesh around the building that is walkable
    {
        _1x1,
        _2x2,
        _2x3,
    }
    
    public abstract class BuildingBase : MonoBehaviour
    {
        
        private RoadManager _roadManager;
        private GridManager _gridManager;
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
        
        //Test only
        private ParkingMesh _parkingMesh;
    
        public BuildingType BuildingType { get; private set; }  // Make it a property
        [SerializeField] protected float lifeTime = 2f;
        [SerializeField] public ParkingLotSize parkingLotSize = ParkingLotSize._1x1;
        public DirectionType Direction { get; private set; }
        public void Initialize (Node node, BuildingType buildingType, Vector2 worldPosition)
        {
            _parkingMesh = FindObjectOfType<ParkingMesh> ();
            //Random between horizontal and vertical bulding
            float random = Random.Range(0f, 1f);
            bool isHorizontal = random <= 0.5f;
            
            this._roadManager = GameManager.Instance.RoadManager;
            this.BuildingType = buildingType;
            this._worldPosition = worldPosition;
            this._originBuildingNode = GridManager.NodeFromWorldPosition(worldPosition);

            _parkingNodes = new List<Node>();
            DirectionType[] directionTypes = new[]
                { DirectionType.Down, DirectionType.Up, DirectionType.Left, DirectionType.Right };
            
            int randomIndex = Random.Range(0, directionTypes.Length);
            Direction = directionTypes[3];
            //This has to be called first to set up for the next function, save parking nodes to set adj list to road nodes later
            SetBuildingAndInsideRoads(node, parkingLotSize,Direction);
            // Invoke("DeactivateBuilding", LifeTime);
            SpawnRoad(node, parkingLotSize, Direction);
            Debug.Log(directionTypes[3]);
            Debug.Log(OriginBuildingNode.WorldPosition);
            _roadManager.FinishSpawningRoad(this);
        }

        private void DeactivateBuilding()
        {
            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// Set building (unWalkable, not empty node), based on parkingLotSize and direction.
        /// BitwiseDirection = right => building on left, road on right.
        /// Directions are limited to [Up, Down, Left, Right].
        /// </summary>
        /// <param name="originalBuildingNode"></param>
        /// <param name="parkingLotSize"></param>
        /// <param name="direction"></param>
        private void SetBuildingAndInsideRoads(Node originalBuildingNode, ParkingLotSize parkingLotSize, DirectionType direction)
        {
            Vector2 position = originalBuildingNode.WorldPosition;
            float nodeDiameter = GridManager.NodeDiameter;

            (List<Vector2>, List<Vector2>) GetBuildingWalkableOffsets(ParkingLotSize size, DirectionType dir)
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
                    if (dir == DirectionType.Up || dir == DirectionType.Down) //Basically, the second building node spawned on the left of original node
                    {
                        float directionMultiplier = dir == DirectionType.Up ? 1 : -1;
                        buildingOffsets.AddRange(new[] { Vector2.zero, new Vector2(-nodeDiameter, 0) });
                        walkableOffsets.AddRange( new[] {new Vector2(-nodeDiameter, nodeDiameter * directionMultiplier), new Vector2(0, nodeDiameter * directionMultiplier)});
                    }
                    else // Left or Right
                    {
                        float directionMultiplier = dir == DirectionType.Right ? 1 : -1; //Basically, the second building node spawned on the top of original node
                        buildingOffsets.AddRange(new[] { Vector2.zero, new Vector2(0, nodeDiameter) });
                        walkableOffsets.AddRange( new[] {new Vector2(nodeDiameter * directionMultiplier, nodeDiameter), new Vector2(nodeDiameter * directionMultiplier, 0)});
                    }
                }
                else if (size == ParkingLotSize._2x3)
                {
                    if (dir == DirectionType.Up || dir == DirectionType.Down)
                    {
                        float directionMultiplier = dir == DirectionType.Up ? 1 : -1;
                        buildingOffsets.AddRange(new[]
                        {
                            Vector2.zero,
                            new Vector2(nodeDiameter, 0),
                            new Vector2(0, nodeDiameter * directionMultiplier),
                            new Vector2(nodeDiameter, nodeDiameter * directionMultiplier)
                        });
                        
                        walkableOffsets.AddRange(new[]
                        {
                            new Vector2(0, nodeDiameter * directionMultiplier),
                            new Vector2(nodeDiameter, nodeDiameter * directionMultiplier)
                        });
                    }
                    else // Left or Right
                    {
                        float directionMultiplier = dir == DirectionType.Right ? 1 : -1;
                        buildingOffsets.AddRange(new[]
                        {
                            Vector2.zero,
                            new Vector2(nodeDiameter * directionMultiplier, 0),
                            new Vector2(0, nodeDiameter),
                            new Vector2(nodeDiameter * directionMultiplier, nodeDiameter)
                        });
                        
                        walkableOffsets.AddRange(new[]
                        {
                            new Vector2(nodeDiameter * directionMultiplier, 0),
                            new Vector2(nodeDiameter * directionMultiplier, nodeDiameter)
                        });
                    }
                }
                return (buildingOffsets, walkableOffsets);
            }

            // Iterate through calculated offsets and apply building settings
            List<Vector2> buildingOffsets = GetBuildingWalkableOffsets(parkingLotSize, direction).Item1;
            foreach (Vector2 offset in buildingOffsets)
            {
                Node buildingNode = GridManager.NodeFromWorldPosition(position + offset);
                buildingNode.SetBuilding(true);
                buildingNode.SetWalkable(false);
                _parkingMesh.PlaceBuildingMesh(originalBuildingNode, parkingLotSize, direction);
            }
            
            List<Vector2> walkableOffsets = GetBuildingWalkableOffsets(parkingLotSize, direction).Item2;
            foreach (Vector2 offset in walkableOffsets)
            {
                //Set this like a road with RoadManager, set but not create mesh
                Node insideRoadNode = GridManager.NodeFromWorldPosition(position + offset);
               
                _parkingNodes.Add(insideRoadNode);
                insideRoadNode.SetBelongedBuilding(this);
                
                insideRoadNode.SetEmpty(false);
                insideRoadNode.SetWalkable(true);

                //Because the 1x1 single house, the car can go inside the house
                if (parkingLotSize != ParkingLotSize._1x1)
                {
                    insideRoadNode.SetDrawable(false);
                }
                _roadManager.PlaceNode(insideRoadNode, null);
            }

            _roadManager.PlaceNode(originalBuildingNode, this);
            _parkingMesh.PlaceBuildingMesh(originalBuildingNode, parkingLotSize, direction );
        }

        /// <summary>
        /// Spawns a road node based on the parking lot size and direction.
        /// </summary>
        /// <param name="buildingNode"></param>
        /// <param name="parkingLotSize"></param>
        /// <param name="direction"></param>
        private void SpawnRoad(Node buildingNode, ParkingLotSize parkingLotSize, DirectionType direction)
        {
            Vector2 position = buildingNode.WorldPosition;
            float nodeDiameter = GridManager.NodeDiameter;
            
            // xMult, yMult must be positive, only useful when only change 1 offset, X or Y
            Vector2 GetOffset(DirectionType direction, float xMult, float yMult)
            {
                return direction switch
                {
                    DirectionType.Up => new Vector2(0, yMult * nodeDiameter),
                    DirectionType.Down => new Vector2(0, -yMult * nodeDiameter),
                    DirectionType.Right => new Vector2(xMult * nodeDiameter, 0),
                    DirectionType.Left => new Vector2(-xMult * nodeDiameter, 0),
                    _ => new Vector2(0, 0)
                };
            }
            
            if (parkingLotSize == ParkingLotSize._1x1)
            {
                Vector2 offset = GetOffset(direction, 1, 1); // Same multiplier for _1x1
                Node roadNode = GridManager.NodeFromWorldPosition(position + offset);
                
                _roadManager.PlaceNode(roadNode, null);
                _roadManager.SetAdjList(roadNode, buildingNode);
                _roadManager.CreateMesh(roadNode);
            }
            else if (parkingLotSize == ParkingLotSize._2x2 || parkingLotSize == ParkingLotSize._2x3)
            {
                float maxMultipler = parkingLotSize == ParkingLotSize._2x2 ? 2 : 3; //Multiply with node Diameter

                Vector2[] offsets;
                // Define possible offsets for _2x2 based on random ranges
                if( direction == DirectionType.Up || direction ==  DirectionType.Down)
                {
                    float directionMultipler = direction == DirectionType.Up ? 1 : -1;
                    offsets = new[]
                    {
                        new Vector2(nodeDiameter, nodeDiameter * (maxMultipler -1) * directionMultipler),
                        new Vector2(0, nodeDiameter * directionMultipler * maxMultipler),
                        new Vector2(- nodeDiameter, nodeDiameter * directionMultipler * maxMultipler),
                        new Vector2(-2 * nodeDiameter, nodeDiameter * (maxMultipler -1) * directionMultipler)
                    };
                }else if (direction == DirectionType.Left || direction == DirectionType.Right)
                {
                    float directionMultipler = direction == DirectionType.Right ? 1 : -1;
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
                
                int randomIndex = Random.Range(1, offsets.Length);
                Vector2 chosenOffset = offsets[randomIndex];
                Node roadNode = GridManager.NodeFromWorldPosition(position + chosenOffset);
                
                //Get direction of a road;
                BitwiseDirection GetRoadDirection(Node roadNode, List<Node> parkingNodes,DirectionType direction)
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
                    
                    //Check same direction case => return = direction
                        
                    return direction switch
                    {
                        DirectionType.Up => BitwiseDirection.Bottom,
                        DirectionType.Down => BitwiseDirection.Up,
                        DirectionType.Left => BitwiseDirection.Right,
                        DirectionType.Right => BitwiseDirection.Left,
                    };
                }
                _roadNode = roadNode;
                _roadManager.PlaceNode(roadNode, null);

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
                
                _roadManager.CreateMesh(roadNode, GetRoadDirection(roadNode, _parkingNodes, direction)); 
             }
           
        }
    }
}
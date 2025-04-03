using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Mesh_Generator;
using Game._00.Script._03.Traffic_System.Road;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
namespace Game._00.Script._03.Traffic_System.Building
{
    [System.Serializable]
    public struct BuildingPrefab
    {
        public GameObject Prefab;
        public BuildingColor Color;
        public BuildingType Type;
    }
    public struct Zone
    {
        public Vector2 Pivot;
        public Vector2 Size;
    }
    
    public struct SpawnInfo
    {
        public BuildingType Type;
        public BuildingColor Color;
        public ParkingLotSize Size;
        public BuildingDirection Direction;
    }

    public class BuildingSpawner : MonoBehaviour, IObserver
    {
        [Header("Gizmos")] 
        
        [SerializeField] private bool isGizmos = false;

        [SerializeField] private bool show1x1 = true;
        
        [SerializeField] private bool show2x2 = true;
        
        [SerializeField] private bool show2x3 = true;
        
        [Header("BuildingBase Prefabs")]
        
        [SerializeField] private List<BuildingPrefab> buildingPrefabs; 
        
        [Header("Delay setting")]
        
        [Tooltip("Time delay between each spawn")]
        [SerializeField] private float spawnDelayTime = 1;
        
        [FormerlySerializedAs("_carDemandRatio")]
        [Header("Spawn Stats")]
        
        [Tooltip("Time delay between each spawn")]
        [SerializeField] private float carDemandRatio = 3; //Ration between demands/cars
        
        private Dictionary<(BuildingType, BuildingColor), BuildingPrefab> _buildingPrefabsDict;
        
        private BuildingColor[] _buildingColors;
        
        private BuildingDirection[] _buildingDirections;
        
        private Coroutine _spawnWaveCoroutine;
        
        private ParkingMesh _parkingMesh;

        //Top level class
        private BuildingManager _buildingManager;
        
        private RoadManager _roadManager;

        private ObjectPooling _objectPooling;
        
        private PossionDisc _possionDisc;
        
        //Zone
        private Zone _currentZone;
        
        private int _currentWeek;

        private float _spawnTimeCounter;
        
        private Queue<SpawnInfo>  _spawnQueue;
        
        
        private void Start()
        {
            IntialSetUp();
        }
        

        #region Initialize
        private void IntialSetUp()
        {
            //Top-level classes
            _buildingManager = FindObjectOfType<BuildingManager>();
            
            _roadManager = FindObjectOfType<RoadManager>();
            
            _objectPooling = GameManager.Instance.ObjectPooling;
            
            _parkingMesh = FindObjectOfType<ParkingMesh>();

            _buildingPrefabsDict = new Dictionary<(BuildingType, BuildingColor), BuildingPrefab>();
            
            _spawnQueue = new Queue<SpawnInfo>();

            for (int i = 0; i < buildingPrefabs.Count; i++)
            {
                _buildingPrefabsDict.Add((buildingPrefabs[i].Type, buildingPrefabs[i].Color), buildingPrefabs[i]);
            }
            
            _buildingColors = Enum.GetValues(typeof(BuildingColor)) as BuildingColor[];
            
            _buildingDirections = Enum.GetValues(typeof(BuildingDirection)) as BuildingDirection[];
            
            _currentWeek = 1;

            _currentZone = new Zone()
            {
                Pivot = new Vector2(-7, -4),
                Size = new Vector2(14 * GridManager.NodeDiameter, 8 * GridManager.NodeDiameter),
            };
                
            _possionDisc = new PossionDisc(_currentZone.Pivot, _currentZone.Size);
            
            _spawnTimeCounter = spawnDelayTime;
        }
        #endregion

        private void Update()
        {
            ProcessWave();
        }
        /// <summary>
        /// ISubject: GameStateManager
        /// manage the current level
        /// </summary>
        /// <param name="data"> int currentLevel</param>
        /// <param name="flag"> NotificationsFlag: Update Level</param>
        public void OnNotified(object data, string flag)
        {
            // if (flag == NotificationFlags.UPDATE_LEVEL && data is int && !_isProcessingWave)
            // {
            //     ProcessWave((int)data);
            // //    }
            if (flag == NotificationFlags.WEEK_PASS)
            {
                _currentWeek++;
            }
            
            if(flag == NotificationFlags.DEMAND_BUILDING)
            {
              GenerateWaves();   
            }
        }


        /// <summary>
        /// Process the wave from queue list
        /// </summary>
        private void ProcessWave()
        {
            if (_spawnQueue.Count <= 0)
            {
                return;
            }

            _spawnTimeCounter -= Time.deltaTime;

            if (_spawnTimeCounter <= 0 && _spawnQueue.Count > 0)
            {
                SpawnInfo spawnInfo = _spawnQueue.Peek();
                
                List<Vector2> points = _possionDisc[spawnInfo.Size];
                
                bool spawnSuccess = false;
                int currentAttempt = 0;
                
                foreach (Vector2 point in points)
                {
                    if (IsValid(point, spawnInfo.Size))
                    {
                        //Spawned object
                        GameObject buildingObj = _objectPooling.GetObj(_buildingPrefabsDict[(spawnInfo.Type, spawnInfo.Color)].Prefab);

                        BuildingBase buildingComp = buildingObj.GetComponent<BuildingBase>();

                        //Get and initialize class
                        Vector2 buildingPos = point;

                        BuildingDirection buildingDirection = spawnInfo.Direction;
                        
                        BuildingType buildingType = spawnInfo.Type;
                        
                        ParkingLotSize parkingLotSize = spawnInfo.Size;

                        Node buildingNode = GridManager.NodeFromWorldPosition(buildingPos);
                        buildingComp.Initialize(_buildingManager, buildingNode, buildingType, buildingDirection, buildingPos);
                        
                        //Set Sprite
                        Sprite sprite = buildingComp.SpriteCollections.GetBuildingSprite(buildingDirection, buildingComp.Size);
                        buildingObj.GetComponent<SpriteRenderer>().sprite = sprite;

                        //Set Transform
                        buildingObj.transform.position = SetTransformOnSize(buildingComp.Size, buildingDirection, buildingPos);
                        buildingObj.SetActive(true);
                        
                        //This has to be called first to set up for the next function, save parking nodes to set adj list to road nodes later
                        SetBuildingAndInsideRoads(buildingComp, buildingNode, buildingComp.Size, buildingDirection);
                        buildingComp.CenterPos = GetCenterPos(buildingPos, buildingDirection, buildingComp.Size);

                        //Set road to building
                        Node roadNode = SpawnRoadRandomDirection(buildingNode, buildingComp.Size, buildingDirection);
                        buildingComp.RoadNode = roadNode; 
                        buildingComp.RoadNode.SetBelongedBuilding(buildingComp.gameObject);
                        _roadManager.PlaceNode(roadNode);

                        if (buildingComp.Size == ParkingLotSize._1x1)
                        {
                            _roadManager.SetAdjList(roadNode, buildingNode);
                            _roadManager.CreateMesh(roadNode);
                        }
                        else
                        {
                            //Set adj to all parking nodes
                            // SetClosestDrawable(roadNode, buildingComp.ParkingNodes);
                            _roadManager.CreateMesh(roadNode,
                                GetRoadDirection(roadNode, buildingComp.ParkingNodes, buildingDirection));
                        }
                        
                        buildingComp.ParkingPos = GetParkingPos(buildingNode.WorldPosition, buildingDirection, parkingLotSize);

                        Vector3 SetTransformOnSize(ParkingLotSize parkingLotSize, BuildingDirection direction,
                            Vector2 spawnPos)
                        {
                            Vector2 offset;
                            switch (parkingLotSize)
                            {
                                case ParkingLotSize._2x2:
                                    if (direction == BuildingDirection.Left || direction == BuildingDirection.Right)
                                    {
                                        offset = new Vector2(0, 1);
                                    }
                                    else
                                    {
                                        offset = new Vector2(-1, 0);
                                    }

                                    break;
                                case ParkingLotSize._2x3:
                                    if (direction == BuildingDirection.Left || direction == BuildingDirection.Right)
                                    {
                                        offset = new Vector2((direction == BuildingDirection.Right ? 1 : -1), 1);
                                    }
                                    else
                                    {
                                        offset = new Vector2(-1, (direction == BuildingDirection.Up ? 1 : -1));
                                    }

                                    break;
                                default:
                                    offset = Vector2.zero;
                                    break;
                            }

                            return spawnPos + offset * GridManager.NodeRadius;
                        }

                        spawnSuccess = true;
                        _spawnQueue.Dequeue();
                        break;
                    }
                }

                if (!spawnSuccess)
                {
                    IncreaseZone();
                }

                _spawnTimeCounter = spawnDelayTime;
            }
        }

        /// <summary>
        /// Only calculate current demands vs current cars to enqueue spawn info request
        /// </summary>
        private void GenerateWaves()
        {
            //One Color
            if (_currentWeek <= 3)
            {
                  //Pick random color
                  BuildingColor color =  _buildingColors[Random.Range(0, _buildingColors.Length)];
                
                //If there is no count, prefer to 
                if (_buildingManager.TotalCount <= 0 )
                {
                    _spawnQueue.Enqueue(new SpawnInfo()
                    {
                        Type = BuildingType.Home,
                        Color = color,
                        Size = ParkingLotSize._1x1,
                        Direction = _buildingDirections[Random.Range(0, _buildingDirections.Length)]
                    });
                    
                    _spawnQueue.Enqueue(new SpawnInfo()
                    {
                        Type = BuildingType.Home,
                        Color = color,
                        Size = ParkingLotSize._1x1,
                        Direction = _buildingDirections[Random.Range(0, _buildingDirections.Length)]
                    });

                    _spawnQueue.Enqueue(new SpawnInfo()
                    {

                        Type = BuildingType.Business,
                        Color = color,
                        Size = ParkingLotSize._2x2,
                        Direction = _buildingDirections[Random.Range(0, _buildingDirections.Length)]
                    });
                }
                else
                {
                    if (_buildingManager.GetCarNumb(color) != 0 && _buildingManager.GetDemand(color) != 0)
                    {
                        if (_buildingManager.GetCarNumb(color) > _buildingManager.GetDemand(color) * carDemandRatio)
                        {   
                            _spawnQueue.Enqueue(new SpawnInfo()
                            {

                                Type = BuildingType.Business,
                                Color = color,
                                Size = ParkingLotSize._2x2,
                                Direction = _buildingDirections[Random.Range(0, _buildingDirections.Length)]
                            });
                        }
                        else
                        {
                            _spawnQueue.Enqueue(new SpawnInfo()
                            {
                                Type = BuildingType.Home,
                                Color = color,
                                Size = ParkingLotSize._1x1,
                                Direction = _buildingDirections[Random.Range(0, _buildingDirections.Length)]
                            });
                        }
                    }
                    
                }
            }
        }

        private void IncreaseZone()
        {
            _currentZone.Size +=  Vector2.one * 2;
            _currentZone.Pivot -=Vector2.one;
        }
        
        #region Building Direction Spawn


        /// <summary>
        /// Set building (unWalkable, not empty node), based on size and buildingDirection.
        /// BitwiseDirection = right => building on left, road on right.
        /// Directions are limited to [Up, Down, Left, Right].
        /// </summary>
        /// <param name="originalBuildingNode"></param>
        /// <param name="parkingLotSize"></param>
        /// <param name="buildingDirection"></param>
        private void SetBuildingAndInsideRoads(BuildingBase buildingComp,Node originalBuildingNode, ParkingLotSize parkingLotSize, BuildingDirection buildingDirection)
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
                    if (dir == BuildingDirection.Up ||
                        dir == BuildingDirection
                            .Down) //Basically, the second building node spawned on the left of original node
                    {
                        float directionMultiplier = dir == BuildingDirection.Up ? 1 : -1;
                        buildingOffsets.AddRange(new[] { Vector2.zero, new Vector2(-nodeDiameter, 0) });
                        walkableOffsets.AddRange(new[]
                        {
                            new Vector2(-nodeDiameter, nodeDiameter * directionMultiplier),
                            new Vector2(0, nodeDiameter * directionMultiplier)
                        });
                    }
                    else // Left or Right
                    {
                        float directionMultiplier =
                            dir == BuildingDirection.Right
                                ? 1
                                : -1; //Basically, the second building node spawned on the top of original node
                        buildingOffsets.AddRange(new[] { Vector2.zero, new Vector2(0, nodeDiameter) });
                        walkableOffsets.AddRange(new[]
                        {
                            new Vector2(nodeDiameter * directionMultiplier, nodeDiameter),
                            new Vector2(nodeDiameter * directionMultiplier, 0)
                        });
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
                            new Vector2(-nodeDiameter, nodeDiameter * directionMultiplier * 2)
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
                            new Vector2(nodeDiameter * directionMultiplier * 2, 0),
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
                buildingNode.SetDrawable(false);
                _parkingMesh.PlaceBuildingMesh(originalBuildingNode, parkingLotSize, buildingDirection);
            }

            List<Vector2> walkableOffsets = GetBuildingWalkableOffsets(parkingLotSize, buildingDirection).Item2;
            
            foreach (Vector2 offset in walkableOffsets)
            {
                //Set this like a road with RoadManager, set but not create mesh
                Node insideRoadNode = GridManager.NodeFromWorldPosition(position + offset);
                
                buildingComp.ParkingNodes.Add(insideRoadNode);
                insideRoadNode.SetBelongedBuilding(buildingComp.gameObject);
                
                insideRoadNode.SetRoad(true);
                insideRoadNode.SetWalkable(true);
                insideRoadNode.SetDrawable(false);
                
                _roadManager.PlaceNode(insideRoadNode);
            }
            
            _roadManager.PlaceNode(originalBuildingNode);
            _parkingMesh.PlaceBuildingMesh(originalBuildingNode, parkingLotSize, buildingDirection);
        }


        
        /// <summary>
        /// This is an overloading function, for test only
        /// Used to check if one direction is avaible in spawn random
        /// </summary>
        /// <param name="originalBuildingNode"></param>
        /// <param name="parkingLotSize"></param>
        /// <param name="buildingDirection"></param>
        /// <returns></returns>
        private List<Node> SetBuildingAndInsideRoads(Node originalBuildingNode, ParkingLotSize parkingLotSize, BuildingDirection buildingDirection)
        {
              Vector2 position = originalBuildingNode.WorldPosition;
            float nodeDiameter = GridManager.NodeDiameter;
            List<Node> result = new List<Node>();
            
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
                    if (dir == BuildingDirection.Up ||
                        dir == BuildingDirection
                            .Down) //Basically, the second building node spawned on the left of original node
                    {
                        float directionMultiplier = dir == BuildingDirection.Up ? 1 : -1;
                        buildingOffsets.AddRange(new[] { Vector2.zero, new Vector2(-nodeDiameter, 0) });
                        walkableOffsets.AddRange(new[]
                        {
                            new Vector2(-nodeDiameter, nodeDiameter * directionMultiplier),
                            new Vector2(0, nodeDiameter * directionMultiplier)
                        });
                    }
                    else // Left or Right
                    {
                        float directionMultiplier =
                            dir == BuildingDirection.Right
                                ? 1
                                : -1; //Basically, the second building node spawned on the top of original node
                        buildingOffsets.AddRange(new[] { Vector2.zero, new Vector2(0, nodeDiameter) });
                        walkableOffsets.AddRange(new[]
                        {
                            new Vector2(nodeDiameter * directionMultiplier, nodeDiameter),
                            new Vector2(nodeDiameter * directionMultiplier, 0)
                        });
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
                            new Vector2(-nodeDiameter, nodeDiameter * directionMultiplier * 2)
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
                            new Vector2(nodeDiameter * directionMultiplier * 2, 0),
                            new Vector2(nodeDiameter * directionMultiplier * 2, nodeDiameter)
                        });
                    }
                }

                return (buildingOffsets, walkableOffsets);
            }
            
            List<Vector2> buildingOffsets = GetBuildingWalkableOffsets(parkingLotSize, buildingDirection).Item1;
            foreach (Vector2 offset in buildingOffsets)
            {
                Node buildingNode = GridManager.NodeFromWorldPosition(position + offset);
                result.Add(buildingNode);
            }

            List<Vector2> walkableOffsets = GetBuildingWalkableOffsets(parkingLotSize, buildingDirection).Item2;
            foreach (Vector2 offset in walkableOffsets)
            {
                Node insideRoadNode = GridManager.NodeFromWorldPosition(position + offset);
                result.Add(insideRoadNode);
            }
            return result;
        }
        
        /// <summary>
        /// Spawns a road node based on the parking lot size and buildingDirection.
        /// </summary>
        /// <param name="buildingNode"></param>
        /// <param name="parkingLotSize"></param>
        /// <param name="buildingDirection"></param>
        /// <param name="roadRngIndex"> [0,3] </param>
        private Node SpawnRoadRandomDirection(Node buildingNode, ParkingLotSize parkingLotSize, BuildingDirection buildingDirection)
        {
            int randomIndex = UnityEngine.Random.Range(0, 4);
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
                
                return roadNode;

              
            }
            else if (parkingLotSize == ParkingLotSize._2x2 || parkingLotSize == ParkingLotSize._2x3)
            {
                float maxMultipler = parkingLotSize == ParkingLotSize._2x2 ? 2 : 3; //Multiply with node Diameter

                Vector2[] offsets;
                // Define possible offsets for _2x2 based on random ranges
                if (buildingDirection == BuildingDirection.Up || buildingDirection == BuildingDirection.Down)
                {
                    float directionMultipler = buildingDirection == BuildingDirection.Up ? 1 : -1;
                    offsets = new[]
                    {
                        new Vector2(nodeDiameter, nodeDiameter * (maxMultipler - 1) * directionMultipler),
                        new Vector2(0, nodeDiameter * directionMultipler * maxMultipler),
                        new Vector2(-nodeDiameter, nodeDiameter * directionMultipler * maxMultipler),
                        new Vector2(-2 * nodeDiameter, nodeDiameter * (maxMultipler - 1) * directionMultipler)
                    };
                }
                else if (buildingDirection == BuildingDirection.Left || buildingDirection == BuildingDirection.Right)
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

                Vector2 chosenOffset = offsets[randomIndex];
                Node roadNode = GridManager.NodeFromWorldPosition(position + chosenOffset);
                return roadNode;
            }

            return null;
        }

        /// <summary>
        /// Set the closest node to the road node of building to drawable.
        /// BECAUSE it makes the create mesh function() detach the road connect to 1 road, so it will draw a continuous road between them
        /// </summary>
        /// <param name="roadNode"></param>
        private void SetClosestDrawable(Node roadNode, List<Node> parkingNodes)
        {
            //Get the closest node to the road node, set it to drawable to make it connect to the road node
            float minDst = float.MaxValue;
            Node closestNode = null;
            foreach (Node parkingNode in parkingNodes)
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
            
        }
       
        /// <summary>
        /// Get buildingDirection of a road by calculating angle, and compare x, y component
        /// </summary>
        /// <param name="roadNode"></param>
        /// <param name="parkingNodes"></param>
        /// <param name="direction"></param>
        /// <returns></returns>

        private BitwiseDirection GetRoadDirection(Node roadNode, List<Node> parkingNodes, BuildingDirection direction)
        {
            float roadX = roadNode.WorldPosition.x;
            float roadY = roadNode.WorldPosition.y;

            float parking1X = parkingNodes[0].WorldPosition.x;
            float parking2X = parkingNodes[1].WorldPosition.x;

            float parking1Y = parkingNodes[0].WorldPosition.y;
            float parking2Y = parkingNodes[1].WorldPosition.y;

            //Check perpendicular case
            if (Mathf.Approximately(roadX, parking1X)) //Left and right
            {
                return (roadY > parking1Y && roadY > parking2Y) ? BitwiseDirection.Bottom : BitwiseDirection.Up;

            }

            if (Mathf.Approximately(roadY, parking2Y))
            {
                return (roadX > parking1X && roadX > parking2X)
                    ? BitwiseDirection.Left
                    : BitwiseDirection.Right;
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


        private List<ParkingLot> GetParkingPos(Vector2 originPos, BuildingDirection direction, ParkingLotSize size)
        {

            if (size == ParkingLotSize._1x1)
            {
                float3 center = new float3(originPos.x, originPos.y, 0);
                ParkingLot centerLot = new ParkingLot(center, true);
                return new List<ParkingLot>()
                {
                    centerLot
                };
            }
            else if (size == ParkingLotSize._2x2 || size == ParkingLotSize._2x3)
            {
                float sizeMultipler = size == ParkingLotSize._2x2 ? 1 : 2;
                float nodeRadius = GridManager.NodeRadius;
                float nodeDiameter = GridManager.NodeDiameter;

                if (direction == BuildingDirection.Up || direction == BuildingDirection.Down)
                {
                    float directionMultipler = direction == BuildingDirection.Up ? 1 : -1;
                    float3 center = new float3(originPos.x - nodeRadius,
                        originPos.y + directionMultipler * sizeMultipler * nodeDiameter, 0);
                    float3 right = new float3(center.x + nodeRadius, center.y, 0);
                    float3 left = new float3(center.x - nodeRadius, center.y, 0);

                    ParkingLot centerLot = new ParkingLot(center, true);
                    ParkingLot rightLot = new ParkingLot(right, true);
                    ParkingLot leftLot = new ParkingLot(left, true);
                    
                    return new List<ParkingLot>() { leftLot, centerLot, rightLot };
                }
                else if (direction == BuildingDirection.Left || direction == BuildingDirection.Right)
                {
                    float directionMultipler = direction == BuildingDirection.Right ? 1 : -1;
                    float3 center = new float3(originPos.x + directionMultipler * sizeMultipler * nodeDiameter,
                        originPos.y + nodeRadius, 0);
                    float3 top = new float3(center.x, center.y + nodeRadius, 0);
                    float3 bot = new float3(center.x, center.y - nodeRadius, 0);

                    ParkingLot centerLot = new ParkingLot(center, true);
                    ParkingLot topLot = new ParkingLot(top, true);
                    ParkingLot botLot = new ParkingLot(bot, true);

                    return new List<ParkingLot> { topLot, centerLot, botLot };
                }
            }
            return new List<ParkingLot>();
        }

        private float3 GetCenterPos(Vector2 originPos, BuildingDirection direction, ParkingLotSize size)
        {
            if (size == ParkingLotSize._1x1)
            {
                return new float3(originPos.x, originPos.y, 0);  
            }

            if (size == ParkingLotSize._2x2 || size == ParkingLotSize._2x3)
            {
                float sizeMultipler = size == ParkingLotSize._2x2 ? 1 : 2;
                float nodeRadius = GridManager.NodeRadius;
                float nodeDiameter = GridManager.NodeDiameter;
                
                if (direction == BuildingDirection.Up || direction == BuildingDirection.Down)
                {
                    float directionMultipler = direction == BuildingDirection.Up ? 1 : -1;
                    float3 center = new float3(originPos.x - nodeRadius,
                        originPos.y + directionMultipler * sizeMultipler * nodeDiameter, 0);
                    return center;
                }
                if (direction == BuildingDirection.Left || direction == BuildingDirection.Right)
                {
                    float directionMultipler = direction == BuildingDirection.Right ? 1 : -1;
                    float3 center = new float3(originPos.x + directionMultipler * sizeMultipler * nodeDiameter,
                        originPos.y + nodeRadius, 0);
                
                   return center;
                } 
                
            }
            return float3.zero;
        }


        #endregion

        #region Helper
       
        /// <summary>
        /// Check node around road
        /// </summary>
        /// <returns></returns>
        private bool IsValid(Vector2 spawnNodePos, ParkingLotSize size)
        {
            int radius = size == ParkingLotSize._2x3 ? 3 : 1;
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    if (j == 0 && i == 0)
                    {
                        continue;
                    }

                    Node checkNode = GridManager.NodeFromWorldPosition(new Vector2(spawnNodePos.x + i * GridManager.NodeDiameter, spawnNodePos.y + j * GridManager.NodeDiameter));
                    if (!checkNode.IsEmpty)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        private List<BuildingSpawnInfo> GetBuildingSpawn(float zoneRadius, ParkingLotSize size)
        {
            Stack<(Vector2, BuildingDirection)> stack = new Stack<(Vector2, BuildingDirection)>();
            stack.Push((new Vector2(0, -1), BuildingDirection.Down));
            stack.Push((new Vector2(0, 1), BuildingDirection.Up));
            stack.Push((new Vector2(-1, 0), BuildingDirection.Left));
            stack.Push((new Vector2(1, 0), BuildingDirection.Right));


            int maxAttempt = 100;
            int attempt = 0;
            do
            {
                Vector2 worldRandomPos = Random.insideUnitCircle * zoneRadius;
                Node spawnNode = GridManager.NodeFromWorldPosition(worldRandomPos);
                
                if (!spawnNode.IsEmpty)
                {
                    attempt++;
                    continue;
                }else 
                {
                    List<BuildingSpawnInfo> result = new List<BuildingSpawnInfo>();

                    while (stack.Count > 0)
                    {
                        var (dir, buildingDirection) = stack.Peek();
                        stack.Pop();
                        
                       List<Node> nodes =  SetBuildingAndInsideRoads(spawnNode, size, buildingDirection);

                       bool isValid = true;
                       foreach (Node node in nodes)
                       {
                           if (!node.IsEmpty)
                           {
                               isValid = false;
                               break;
                           }
                       }

                       if (isValid)
                       {
                           result.Add(new BuildingSpawnInfo(){Position = spawnNode.WorldPosition, BuildingDirection = buildingDirection});
                       }


                    }
                    if (result.Count > 0)
                    {
                        return result;
                    }
                        attempt++;
                }
            } while (attempt < maxAttempt);

            return new List<BuildingSpawnInfo>();
        }
        #endregion
        
        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!isGizmos)
            {
                return;
            }
            
            Gizmos.color = Color.green;
            if (_possionDisc != null)
            {
                   Color[] colors = new Color[]
                   {
                       Color.yellow,
                       Color.magenta,
                   };
                   
                   if (show1x1)
                   {
                       Gizmos.color = Color.yellow;
                       ParkingLotSize size = ParkingLotSize._1x1;
                       for (int i = 0; i < _possionDisc[size].Count; i++)
                       {
                           Gizmos.DrawSphere(_possionDisc[size][i], 0.05f);
                       }
                   }

                   if (show2x2)
                   {
                       Gizmos.color = Color.cyan;
                       ParkingLotSize size = ParkingLotSize._2x2;
                       for (int i = 0; i < _possionDisc[size].Count; i++)
                       {
                           Gizmos.DrawSphere(_possionDisc[size][i], 0.1f);
                       }
                   }

                   if (show2x3)
                   {
                       Gizmos.color = Color.magenta;
                       ParkingLotSize size = ParkingLotSize._2x3;
                       for (int i = 0; i < _possionDisc[size].Count; i++)
                       {
                           Gizmos.DrawSphere(_possionDisc[size][i], 0.2f);
                       }
                   }
            }
            
            //Expand from pivot, not expand from the middle
            Handles.Label(_currentZone.Pivot, "Pivot",
                new GUIStyle()
                {
                    fontSize = 20,
                    normal = new GUIStyleState()
                    {
                        textColor = Color.red
                    }
                });
            //Bot edge
            Gizmos.DrawLine(_currentZone.Pivot,  
                new Vector2(_currentZone.Pivot.x + _currentZone.Size.x, _currentZone.Pivot.y));
                
            //Top edge
            Gizmos.DrawLine(new Vector2(_currentZone.Pivot.x, _currentZone.Pivot.y + _currentZone.Size.y),  
                new Vector2(_currentZone.Pivot.x + _currentZone.Size.x, _currentZone.Pivot.y + _currentZone.Size.y));
                
            //Left edge
            Gizmos.DrawLine(_currentZone.Pivot,
                new Vector2(_currentZone.Pivot.x, _currentZone.Pivot.y + _currentZone.Size.y));
                
            //Right edge
            Gizmos.DrawLine(new Vector2(_currentZone.Pivot.x + _currentZone.Size.x, _currentZone.Pivot.y), 
                new Vector2(_currentZone.Pivot.x + _currentZone.Size.x, _currentZone.Pivot.y + _currentZone.Size.y));
       
        }
        #endregion
    }

    public struct BuildingSpawnInfo
    {
        public Vector2 Position;
        public BuildingDirection BuildingDirection;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Game._00.Script._00.Manager;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.MapData;
using Game._00.Script._03.Traffic_System.Mesh_Generator;
using Game._00.Script._03.Traffic_System.Road;
using Game._00.Script.Camera;
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
    
    /// <summary>
    /// Spawn wave info, require keys stat
    /// </summary>
    public struct SpawnInfo
    {
        public bool DemandOnly;
        public BuildingType Type;
        public BuildingColor Color;
        public ParkingLotSize Size;
        public BuildingDirection Direction;
    }

    public class BuildingSpawner : MonoBehaviour, IObserver
    {
        [Header("Debug")] [Tooltip("Show spawn queue count")] [SerializeField]
        private bool showSpawnQueue;

        [Header("BuildingBase Prefabs")] [SerializeField]
        private List<BuildingPrefab> buildingPrefabs;

        [Header("Delay setting")] 
        [Tooltip("Time delay between each spawn")] [SerializeField]
        private float spawnDelayTime = 0.5f;
        
        [Space]
        [Header("Spawn Stats")]
        [Tooltip("Time delay between each spawn")]
        [SerializeField]
        private int carDemandRatio = 2; //Ration between demands/cars
        
        [Tooltip("Decrease car demand ratio speed")]
        [SerializeField]
        private float decreaseCarDemand = 0.05f; //Ration between demands/cars
        private float _curDecreaseCarDemand; //Track to build up, when > 1 then actually decrease
        
        [Space]
        [Tooltip("Max distance from home to business that go over it, the candiate does not get more weight")]
        [SerializeField]
        private int maxDst = 5;

        [Tooltip("The increase in max distance per week")] 
        [SerializeField] 
        private int increaseMaxDst = 1;
        
        [FormerlySerializedAs("newAddRatio")]
        [Space]
        [Tooltip("Ratio between adding news building, or increase, 0.8 => 80% new building")]
        [Range(0, 1)] 
        [SerializeField] private float addNewRatio = 0; 
        
        [FormerlySerializedAs("increaseNewAddRatio")]
        [Range(0, 1)]
        [SerializeField] private float increaseAddNewRatio = 0.15f;
        
        
        private Dictionary<(BuildingType, BuildingColor), BuildingPrefab> _buildingPrefabsDict;

        private BuildingColor[] _buildingColors;

        private BuildingDirection[] _buildingDirections;

        private Coroutine _spawnWaveCoroutine;

        private ParkingMesh _parkingMesh;

        //Top level class
        private BuildingManager _buildingManager;

        private RoadManager _roadManager;

        private ObjectPooling _objectPooling;
        
        private MapSupplyDemand _mapSupplyDemand;

        private Queue<SpawnInfo> _spawnQueue;

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

            _mapSupplyDemand = FindObjectOfType<MapSupplyDemand>();

            _mapSupplyDemand.SetUp();

            _buildingPrefabsDict = new Dictionary<(BuildingType, BuildingColor), BuildingPrefab>();

            _spawnQueue = new Queue<SpawnInfo>();

            for (int i = 0; i < buildingPrefabs.Count; i++)
            {
                _buildingPrefabsDict.Add((buildingPrefabs[i].Type, buildingPrefabs[i].Color), buildingPrefabs[i]);
            }

            _buildingColors = Enum.GetValues(typeof(BuildingColor)) as BuildingColor[];

            _buildingDirections = Enum.GetValues(typeof(BuildingDirection)) as BuildingDirection[];
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
            if (flag == NotificationFlags.WEEK_PASS)
            {
               UpdateSpawningStat(); 
            }

            if (flag == NotificationFlags.DEMAND_BUILDING)
            {
                GenerateWaves();
            }
        }
        
        /// <summary>
        /// Process the wave from queue list
        /// </summary>
        private void ProcessWave()
        {
            if (_spawnQueue.Count == 0)
            {
                return;
            }

            SpawnInfo spawnInfo = _spawnQueue.Peek();

            if (spawnInfo.DemandOnly)
            {
                _buildingManager.IncreaseDemand();
            }
            else
            {
                float[] weights = new float[] { 0.2f, 0.4f, 0.6f, 0.8f, 1 };
            
                //If spawning home, choose and select around neighborhood;
                Dictionary<Vector2, float> scores = new Dictionary<Vector2, float>();
                foreach (float weight in weights)
                {
                    foreach (Vector2 point in _mapSupplyDemand[spawnInfo.Size, weight])
                    {
                        scores.Add(point, spawnInfo.Size == ParkingLotSize._1x1 ? GetHomeScore(point, spawnInfo.Color, weight) : GetBusinessScore(point, spawnInfo.Color, weight));
                    }
                }
                
                List<KeyValuePair<Vector2, float>> sortedScores = scores.OrderByDescending(kv => kv.Value).ToList();

                foreach (KeyValuePair<Vector2, float> data in sortedScores)
                {
                    //10 Attempts to change road node
                    for (int j = 0; j < 5; j++)
                    {
                        Node road = null;
                        if (IsValid(data.Key, spawnInfo.Size, spawnInfo.Direction, out road))
                        {
                            //Spawned object
                            GameObject buildingObj =
                                _objectPooling.GetObj(_buildingPrefabsDict[(spawnInfo.Type, spawnInfo.Color)].Prefab);

                            BuildingBase buildingComp = buildingObj.GetComponent<BuildingBase>();

                            //Get and initialize class
                            Vector2 buildingPos = data.Key;

                            BuildingDirection buildingDirection = spawnInfo.Direction;

                            BuildingType buildingType = spawnInfo.Type;

                            ParkingLotSize parkingLotSize = spawnInfo.Size;

                            Node buildingNode = GridManager.NodeFromWorldPosition(buildingPos);
                            buildingComp.Initialize(_buildingManager, buildingNode, buildingType, buildingDirection,
                                buildingPos);

                            //Set Sprite
                            Sprite sprite =
                                buildingComp.SpriteCollections.GetBuildingSprite(buildingDirection, buildingComp.Size);
                            buildingObj.GetComponent<SpriteRenderer>().sprite = sprite;

                            //Set Transform
                            buildingObj.transform.position =
                                SetTransformOnSize(buildingComp.Size, buildingDirection, buildingPos);
                            buildingObj.SetActive(true);

                            //This has to be called first to set up for the next function, save parking nodes to set adj list to road nodes later
                            SetBuildingAndInsideRoads(buildingComp, buildingNode, buildingComp.Size, buildingDirection);
                            buildingComp.CenterPos = GetCenterPos(buildingPos, buildingDirection, buildingComp.Size);

                            //Set road to building
                            Node roadNode = road;
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

                            buildingComp.ParkingPos =
                                GetParkingPos(buildingNode.WorldPosition, buildingDirection, parkingLotSize);

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

                            _spawnQueue.Dequeue();

                            return;
                        }
                    }
                    
                }
            }
        }


        /// <summary>
        /// Update controlling task of spawning houses, includes max distance, add/new ratio spawn color or not
        /// </summary>
        private void UpdateSpawningStat()
        {
            maxDst += increaseMaxDst;
            _curDecreaseCarDemand -= decreaseCarDemand;

            if (_curDecreaseCarDemand >= 1)
            {
                carDemandRatio -= 1;
                _curDecreaseCarDemand = 0;
            }

            addNewRatio += increaseAddNewRatio;
        }

        /// <summary>
        /// Only calculate current demands vs current cars to enqueue spawn info request
        /// </summary>
        private void GenerateWaves()
        {
            bool increaseOnly = Random.value <= addNewRatio;

            if (increaseOnly && _buildingManager.BusinessCount > 0)
            {
                Debug.Log(addNewRatio);
                _spawnQueue.Enqueue(new SpawnInfo()
                {
                    DemandOnly =  true
                });
            }
            else
            {
                bool isFull = true;
                List<BuildingColor> notExistColor = new List<BuildingColor>();
                foreach (BuildingColor c in _buildingColors)
                {
                    int demand = _buildingManager.GetDemand(c);
                    int carNumb = _buildingManager.GetCarNumb(c);

                    if (demand == 0 && carNumb == 0)
                    {
                        notExistColor.Add(c);
                    } //Prioritize feeling demands first
                    else if (carNumb < demand * carDemandRatio && demand > 0)
                    {
                        _spawnQueue.Enqueue(new SpawnInfo()
                        {
                            Color = c,
                            Direction = _buildingDirections[Random.Range(0, _buildingDirections.Length)],
                            Size = ParkingLotSize._1x1,
                            Type = BuildingType.Home,
                        });

                        isFull = false;
                    }
                }

                if (isFull)
                {
                    //If all demands fill out, spawn the color that current does not exist
                    BuildingColor color = _buildingColors[Random.Range(0, _buildingColors.Length)];
                    if (notExistColor.Count > 0)
                    {
                        color = notExistColor[Random.Range(0, notExistColor.Count)];
                    }

                    _spawnQueue.Enqueue(new SpawnInfo()
                    {
                        Color = color,
                        Direction = _buildingDirections[Random.Range(0, _buildingDirections.Length)],
                        Size = ParkingLotSize._2x3,
                        Type = BuildingType.Business,
                    });

                    _spawnQueue.Enqueue(new SpawnInfo()
                    {
                        Color = color,
                        Direction = _buildingDirections[Random.Range(0, _buildingDirections.Length)],
                        Size = ParkingLotSize._1x1,
                        Type = BuildingType.Home,
                    });
                }
            }
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
        private void SetBuildingAndInsideRoads(BuildingBase buildingComp, Node originalBuildingNode,
            ParkingLotSize parkingLotSize, BuildingDirection buildingDirection)
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
        /// Used to check if one direction is available in spawn random
        /// </summary>
        /// <param name="originalBuildingNode"></param>
        /// <param name="parkingLotSize"></param>
        /// <param name="buildingDirection"></param>
        /// <returns></returns>
        private List<Node> SetBuildingAndInsideRoads(Node originalBuildingNode, ParkingLotSize parkingLotSize,
            BuildingDirection buildingDirection)
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
        private Node SpawnRoadRandomDirection(Node buildingNode, ParkingLotSize parkingLotSize,
            BuildingDirection buildingDirection)
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
        private bool IsValid(Vector2 spawnNodePos, ParkingLotSize size, BuildingDirection direction, out Node roadNode)
        {
            Node buildingNode = GridManager.NodeFromWorldPosition(spawnNodePos);
            List<Node> nodes = SetBuildingAndInsideRoads(buildingNode, size, direction);

            Node road = SpawnRoadRandomDirection(buildingNode, size, direction);
            
            nodes.Add(road);

            foreach (Node node in nodes)
            {
                if (!node.IsEmpty)
                {
                    roadNode = null;
                    return false;
                }
            }

            roadNode = road;
            return true;
        }

        /// <summary>
        /// get score to pick the best score of all candidate, pick it to spawn
        /// criteria:
        /// far from business
        /// good density, around 3-5 houses same color
        /// </summary>
        /// <param name="candidate">candidate of spawn pos (must be the center of node)</param>
        /// <param name="weight">weight of layout</param>
        /// <param name="color">color of candidate</param>
        /// <returns></returns>
        private float GetHomeScore(Vector2 candidate, BuildingColor color, float weight)
        {
            float minDst = Mathf.Min(_buildingManager.DstToClosestBuilding(candidate, color), maxDst);
            int neighbors = _buildingManager.GetHomeDensity(candidate, 5);
            int businesses = _buildingManager.GetBusinessDensity(candidate, 5);
            
            if (neighbors > 7)
                return float.MinValue;

            float idealNeighbors = 4f;
            float neighborScore = -Mathf.Pow(neighbors - idealNeighbors, 2) + (idealNeighbors * idealNeighbors);

            bool forceSpread = Random.value < 0.2f; // 20% chance spread spawn
            float finalNeighborScore = forceSpread ? 0 : neighborScore;

            float score = minDst * 20 + finalNeighborScore * 10 + weight * 2f - businesses * 10;

            return score;
        }

        
        
        /// <summary>
        /// Get score to pick the best score of all candidate, pick it to spawn
        /// criteria:
        /// far from home
        /// prefer empty space
        /// </summary>
        /// <param name="candidate">candidate of spawn pos (must be the center of node)</param>
        /// <param name="weight">weight of layout</param>
        /// <param name="color">color of candidate</param>
        /// <returns></returns>
        private float GetBusinessScore(Vector2 candidate, BuildingColor color, float weight)
        {
            float minDst = _buildingManager.DstToClosestBuilding(candidate, color);

            int nearHomes = _buildingManager.GetHomeDensity(candidate, 5);

            return minDst - nearHomes + weight * 2f;
        }

            /// <summary>
        /// Get random choice base on the demand, which larger number get to pick mor frequently
        /// </summary>
        /// <param name="weights">The number of float with weight</param>
        /// <returns></returns>
        private float GetRandomWeight(float[] weights)
        {
            float sum = 0f;
            foreach (float weight in weights)
            {
                sum += weight;
            }

            float random = Random.value * sum;

            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (random <= cumulative)
                {
                    return weights[i];
                }
            }
            return weights[weights.Length - 1];
        }

        #endregion


        private void OnGUI()
        {
            if (_spawnQueue == null || !showSpawnQueue)
            {
                return;
            }
            GUI.Label(new Rect(10,20,200,200), _spawnQueue.Count.ToString(), new GUIStyle()
            {
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
                fontSize = 20
            });
        }
    }

  
}
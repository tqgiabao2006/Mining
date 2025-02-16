using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._00.Manager;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Mesh_Generator;
using Game._00.Script._03.Traffic_System.Road;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Game._00.Script._03.Traffic_System.Building
{
    struct SpawningWaveInfo
    {
        public int waveIndex; //Current level (game phase) 
        public List<BuildingInfo> BuildingInfos;
        public float WaveDelay; //Delay after one wave
        public float ZoneRadius;


        public SpawningWaveInfo(int waveIndex, float waveDelay, float zoneRadius, List <BuildingInfo> buildingsInfos)
        {
            this.waveIndex = waveIndex;
            this.BuildingInfos = buildingsInfos;
            this.WaveDelay = waveDelay;
            this.ZoneRadius = zoneRadius; 
        }

    }

    struct BuildingInfo
    {
        public BuildingType BuildingType;
        public int Amount;
        public float SpawnTime;
        public BuildingInfo(BuildingType buildingType, int amount, float spawnTime)
        {
            this.BuildingType = buildingType;
            this.Amount = amount; 
            this.SpawnTime = spawnTime;
        }
    }
    [System.Serializable]
    public class BuildingPrefabPair
    {
        public BuildingType BuildingType;
        public GameObject Prefab;
        public BuildingPrefabPair(BuildingType buildingType, GameObject prefab)
        {
            this.BuildingType = buildingType;
            this.Prefab = prefab;
        }
    }

    public class BuildingSpawner : MonoBehaviour, IObserver
    {
        //Vector3 zone center, float radius
        private Dictionary<Vector3, float> _zoneDictionary = new Dictionary<Vector3, float>();
        [Header("Gizmos")] [SerializeField] public bool isGizmos = false;

        [Header("BuildingBase Prefabs")]
        public List<BuildingPrefabPair> BuildingPrefabs = new List<BuildingPrefabPair>();

        [Header("Wave settings")] [SerializeField]
        public int maxWaves = 2;

        private SpawningWaveInfo[] _waveInfos;

        [Header("BuildingBase Settings")] [SerializeField]
        public float buildingBoundary = 0.5f;

        public int currentWave = 0;
        private Coroutine _spawnWaveCoroutine;

        private bool _isProcessingWave = false; //Avoid being notified multiple times when processing

        //Mesh
        private RoadMesh _roadMesh;
        private ParkingMesh _parkingMesh;

        //Top level class
        private BuildingManager _buildingManager;
        private RoadManager _roadManager;

        private ObjectPooling _objectPooling;
        private Invertory _invertory;



        private void Start()
        {
            IntialSetUp();
            WaveSetUp();
            ProcessWave(0);
        }

        #region Initialize

        private void IntialSetUp()
        {
            //Top-level classes
            _buildingManager = FindObjectOfType<BuildingManager>();
            _roadManager = FindObjectOfType<RoadManager>();

            _objectPooling = GameManager.Instance.ObjectPooling;
            _invertory = FindObjectOfType<Invertory>();

            //Load mesh class
            _roadMesh = FindObjectOfType<RoadMesh>();
            _parkingMesh = FindObjectOfType<ParkingMesh>();

            //Initialize data structure
            _waveInfos = new SpawningWaveInfo[maxWaves];
        }

        private void WaveSetUp()
        {
            //Level 1:
            _waveInfos[0] = new SpawningWaveInfo(0, 3, 5, new List<BuildingInfo>()
            {
                new BuildingInfo(BuildingType.NormalCell, 1, 0f),
                new BuildingInfo(BuildingType.Heart, 1, 2f),
                new BuildingInfo(BuildingType.NormalCell, 1, 4f),
                new BuildingInfo(BuildingType.Heart, 1, 2f)
            });
        }

        #endregion

        /// <summary>
        /// ISubject: GameStateManager
        /// GameStateManager manage the current level
        /// </summary>
        /// <param name="data"> int currentLevel</param>
        /// <param name="flag"> NotificationsFlag: Update Level</param>
        public void OnNotified(object data, string flag)
        {
            if (flag != NotificationFlags.UpdateLevel || data is not int) return;
            if (!_isProcessingWave)
            {
                ProcessWave((int)data);
            }
        }

        private void ProcessWave(int currentLevel)
        {
            SpawningWaveInfo waveInfo = _waveInfos[currentLevel];

            if (_spawnWaveCoroutine != null)
            {
                StopCoroutine(_spawnWaveCoroutine);
            }

            // Start the coroutine for spawning
            _spawnWaveCoroutine = StartCoroutine(SpawnCoroutine(waveInfo));
        }

        private IEnumerator SpawnCoroutine(SpawningWaveInfo waveInfo)
        {
            _isProcessingWave = true;

            float startTime = Time.time + waveInfo.WaveDelay; // Set the start time with wave delay
            int turnCount = 0;
            int roadNumb = _invertory.GetPossitiveNumbRoad();
            

            // Wait for the wave delay to pass
            yield return new WaitForSeconds(waveInfo.WaveDelay);

            // Continue spawning until all buildings have been processed
            while (turnCount < waveInfo.BuildingInfos.Count && roadNumb > 0)
            {
                float currentSpawnTime = waveInfo.BuildingInfos[turnCount].SpawnTime;

                // Wait until it's time to spawn the next building
                yield return new WaitUntil(() => Time.time >= startTime + currentSpawnTime);

                BuildingType buildingType = waveInfo.BuildingInfos[turnCount].BuildingType;
                int count = waveInfo.BuildingInfos[turnCount].Amount;

                // Spawn the specified number of buildings
                for (int i = 0; i < count; i++)
                {
                    GameObject buildingPrefab;
                    if (!TryGetPrefab(buildingType, out buildingPrefab))
                    {
                        buildingPrefab = new GameObject(buildingType.ToString());
                        DebugUtility.Log("Can't find building prefab", "Building Spawner");
                    }

                    //Spawned object
                    GameObject buildingObj = _objectPooling.GetObj(buildingPrefab);
                    BuildingBase buildingComp = buildingObj.GetComponent<BuildingBase>();
                    
                    List<BuildingSpawnInfo> buildingSpawnInfos = GetBuildingSpawn(waveInfo.ZoneRadius, buildingComp.size);

                    int randomIndex = Random.Range(0, buildingSpawnInfos.Count);
                    
                    //Get and initialize class
                    Vector2 buildingPos = buildingSpawnInfos[randomIndex].Position;
                    BuildingDirection buildingDirection = buildingSpawnInfos[randomIndex].BuildingDirection;
 
                    Node buildingNode = GridManager.NodeFromWorldPosition(buildingPos);
                    buildingComp.Initialize(buildingNode, buildingType, buildingPos);
                    
                    _buildingManager.RegisterBuilding(buildingComp);

                    //Set Sprite
                    Sprite sprite = buildingComp.SpriteCollections.GetBuildingSprite(buildingDirection, buildingComp.size);
                    buildingObj.GetComponent<SpriteRenderer>().sprite = sprite;

                    //Set Transform
                    buildingObj.transform.position = SetTransformOnSize(buildingComp.size, buildingDirection,buildingPos);
                    buildingObj.SetActive(true);

                  

                    //This has to be called first to set up for the next function, save parking nodes to set adj list to road nodes later
                    SetBuildingAndInsideRoads(buildingComp, buildingNode, buildingComp.size, buildingDirection);
                    buildingComp.CenterPos = GetCenterPos(buildingPos, buildingDirection, buildingComp.size);
                   
                    //Set road to building
                    Node roadNode = SpawnRoad(buildingNode, buildingComp.size, buildingDirection, randomIndex);
                    buildingComp.RoadNode = roadNode;
                    buildingComp.RoadNode.SetBelongedBuilding(buildingComp.gameObject);
                    _roadManager.PlaceNode(roadNode);
                    
                    if (buildingComp.size == ParkingLotSize._1x1)
                    {
                        _roadManager.SetAdjList(roadNode, buildingNode);
                        _roadManager.CreateMesh(roadNode); 
                    }
                    else
                    {
                        //Set adj to all parking nodes
                        SetClosestDrawable(roadNode, buildingComp.ParkingNodes);
                        _roadManager.CreateMesh(roadNode, GetRoadDirection(roadNode, buildingComp.ParkingNodes, buildingDirection));
                    }
                 

                    buildingComp.ParkingPos = GetParkingPos(buildingNode.WorldPosition, buildingDirection, buildingComp.size);

                    Vector3 SetTransformOnSize(ParkingLotSize parkingLotSize, BuildingDirection direction, Vector2 spawnPos)
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

                }

                turnCount++; // Move to the next building info
            }

            _isProcessingWave = false;
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
                
                //Because the 1x1 single house, the car can go inside the house
                if (parkingLotSize != ParkingLotSize._1x1)
                {
                    insideRoadNode.SetDrawable(false);
                }
                
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
        private Node SpawnRoad(Node buildingNode, ParkingLotSize parkingLotSize, BuildingDirection buildingDirection, int roadRngIndex)
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

                Vector2 chosenOffset = offsets[roadRngIndex];
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

        private bool TryGetPrefab(BuildingType buildingType, out GameObject buildingPrefab)
        {
            for (int i = 0; i < BuildingPrefabs.Count; i++)
            {
                if (BuildingPrefabs[i].BuildingType == buildingType)
                {
                    buildingPrefab = BuildingPrefabs[i].Prefab;
                    return true;
                }
            }

            buildingPrefab = null;
            return false;
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
                    if (size == ParkingLotSize._1x1)
                    {
                        return new List<BuildingSpawnInfo>
                        {
                            new BuildingSpawnInfo(){Position =  spawnNode.WorldPosition, BuildingDirection = BuildingDirection.Up},
                            new BuildingSpawnInfo(){Position =spawnNode.WorldPosition, BuildingDirection = BuildingDirection.Down},
                            new BuildingSpawnInfo(){Position =spawnNode.WorldPosition, BuildingDirection = BuildingDirection.Left},
                            new BuildingSpawnInfo(){Position =spawnNode.WorldPosition, BuildingDirection = BuildingDirection.Right},
                        }; 
                    }
                    else
                    {
                        int sizeMultiplier = size == ParkingLotSize._2x2 ? 2 : 3;
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
                   
                }
            } while (attempt < maxAttempt);

            return new List<BuildingSpawnInfo>();
        }
        private int GetBuildingNumbByWave(SpawningWaveInfo waveInfo)
        {
            int sum = 0;
            foreach (BuildingInfo building in waveInfo.BuildingInfos)
            {
                sum += building.Amount;
            }

            return sum;
        }
        #endregion
        
        #region Gizmos

        private void OnDrawGizmos()
        {
            if(!isGizmos || _waveInfos.Length <=0) return;
            for(int i = 0 ; i < maxWaves; i++)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(Vector2.zero, _waveInfos[i].ZoneRadius);
            }
        }
        #endregion
    }

    public struct BuildingSpawnInfo
    {
        public Vector2 Position;
        public BuildingDirection BuildingDirection;
    }
}
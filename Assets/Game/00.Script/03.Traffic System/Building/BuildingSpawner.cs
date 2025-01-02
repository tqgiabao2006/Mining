using System.Collections;
using System.Collections.Generic;
using Game._00.Script._00.Manager;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Mesh_Generator;
using UnityEngine;
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
        [Header("Gizmos")] 
        [SerializeField] public bool isGizmos = false;
    
        [Header("BuildingBase Prefabs")]
        public List<BuildingPrefabPair> BuildingPrefabs = new List<BuildingPrefabPair>();
    
        [SerializeField] public int maxWaves = 2;
        private SpawningWaveInfo[] _waveInfos;

        [Header("BuildingBase Settings")] [SerializeField]
        public float buildingBoundary = 0.5f;
        public int currentWave = 0;
        private Coroutine _spawnWaveCoroutine;

        private List<Vector2> _usedPositions; //Check current building positions to avoid spawn in the same place
    
        private bool _isProcessingWave = false; //Avoid being notified multiple times when processing
    
        private ObjectPooling _objectPooling;
        private RoadMesh _roadMesh;
        private Invertory _invertory;
        private BuildingManager _buildingManager;
    
        private void Start()
        {
            IntialSetUp();
            WaveSetUp();
            ProcessWave(0);
        }

        #region Initialize
        private void IntialSetUp()
        {
            _objectPooling = GameManager.Instance.ObjectPooling;
            _invertory = FindObjectOfType<Invertory>();
            _buildingManager = FindObjectOfType<BuildingManager>();
            _roadMesh = FindObjectOfType<RoadMesh>();
            _waveInfos = new SpawningWaveInfo[maxWaves];
        
            _usedPositions = new List<Vector2>();
        }
        private void WaveSetUp()
        {
            //Level 1:
            _waveInfos[0] = new SpawningWaveInfo(0, 3, 5, new List<BuildingInfo>()
            {
                new BuildingInfo(BuildingType.NormalCell, 1, 0f),
                new BuildingInfo(BuildingType.Heart, 1, 2f),
                new BuildingInfo(BuildingType.NormalCell, 2, 4f),
                new BuildingInfo(BuildingType.Heart, 1, 5f),
            });
            //Level 2:
            _waveInfos[1] = new SpawningWaveInfo(1, 5, 10,new List<BuildingInfo>()
            {
                new BuildingInfo(BuildingType.Lung, 1, 0f),
                new BuildingInfo(BuildingType.NormalCell, 1, 3f),
                new BuildingInfo(BuildingType.Lung, 1, 4f)
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
            if(flag != NotificationFlags.UpdateLevel || data is not int ) return;
            if (!_isProcessingWave)
            {
                ProcessWave((int) data);
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
            float maxRoadLength = roadNumb * GridManager.NodeDiameter;
        
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
                        Debug.Log("Can't find building prefab");
                    }
                
                    //Spawned object
                    GameObject buildingObj = _objectPooling.GetObj(buildingPrefab);
                    BuildingBase buildingComp = buildingObj.GetComponent<BuildingBase>();
                
                    Vector2 spawnedPos = GetRandomPosition(ref maxRoadLength, waveInfo.ZoneRadius, _usedPositions);
                
                    Node buildingNode = GridManager.NodeFromWorldPosition(spawnedPos);

                    buildingComp.Initialize(buildingNode, buildingType, spawnedPos);
                    _buildingManager.RegisterBuilding(buildingComp);
                    buildingObj.transform.position = SetTransformOnSize(buildingComp.size, buildingComp.BuildingDirection, spawnedPos);
                    buildingObj.transform.rotation = SetRotationOnDirection(buildingComp.BuildingDirection);
                    buildingObj.transform.localScale = SetScaleOnSize(buildingComp.size);
                    buildingObj.SetActive(true);

                    Vector3 SetScaleOnSize(ParkingLotSize size)
                    {
                        return size switch
                        {
                            ParkingLotSize._2x2 => new Vector3(1.5f, 0.9f, 1),
                            ParkingLotSize._2x3 => new Vector3(1.5f, 1.5f, 1),
                            _ => new Vector3(1, 1, 1),
                        };
                    }
                
                    Quaternion SetRotationOnDirection(BuildingDirection direction) =>
                        direction switch
                        {
                            BuildingDirection.Left or BuildingDirection.Right => Quaternion.Euler(0, 0, -90),
                            _ => Quaternion.Euler(0, 0, 0)
                        };

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
                                    offset = new Vector2((direction == BuildingDirection.Right ? 1: -1), 1);
                                }
                                else
                                {
                                    offset = new Vector2(-1,(direction == BuildingDirection.Up? 1: -1));
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
        private Vector2 GetRandomPosition(ref float remainRoadLength, float currentZoneRadius, List<Vector2> usedPosition)
        {
            if (usedPosition.Count == 0)
            {
                Vector2 firstPos = Random.insideUnitCircle * currentZoneRadius;
                Vector2 roundedPos = GridManager.NodeFromWorldPosition(firstPos).WorldPosition;
                usedPosition.Add(roundedPos);
                return roundedPos;
            }
        
            int maxAttempt = 100;
            int attempt = 0;
            Vector3 spawnedPos;
        
            float maxLength = Random.Range(buildingBoundary + GridManager.NodeRadius, remainRoadLength/ 2f);
       
            do
            {
                spawnedPos = Random.insideUnitCircle * currentZoneRadius;
                Vector2 roundedPos = GridManager.NodeFromWorldPosition(spawnedPos).WorldPosition;
                spawnedPos = roundedPos;
            
                float dst = Vector3.Distance(usedPosition[usedPosition.Count -1], spawnedPos);
                if (dst > buildingBoundary + 1f && dst <= maxLength && !usedPosition.Contains(spawnedPos) && GridManager.NodeFromWorldPosition(spawnedPos).IsEmpty)
                {
                    remainRoadLength -= dst;
                    usedPosition.Add(spawnedPos);
                    return spawnedPos;
                }
                attempt++;
            
            } while (attempt < maxAttempt);

            return GridManager.NodeFromWorldPosition(Vector2.zero).WorldPosition;
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
}
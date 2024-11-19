using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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

public class Spawner : MonoBehaviour
{
    //Vector3 zone center, float radius
    private Dictionary<Vector3, float> _zoneDictionary = new Dictionary<Vector3, float>();
    [Header("Gizmos")] 
    [SerializeField] public bool isGizmos = false;
    [Header("Building Prefabs")]
    public List<BuildingPrefabPair> BuildingPrefabs = new List<BuildingPrefabPair>();
    
    [SerializeField] public int maxWaves = 2;
    private SpawningWaveInfo[] _waveInfos;

    [Header("Building Settings")] [SerializeField]
    public float buildingBoundary = 0.5f;
    public int currentWave = 0;
    private Coroutine _spawnWaveCoroutine;
    
    private ObjectPooling _objectPooling;
    private GameManager _gameManager;
    private Grid _grid;
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
        _gameManager = GameManager.Instance;
        _objectPooling = _gameManager.ObjectPooling;
        _grid = _gameManager.Grid;  
        _invertory = FindObjectOfType<Invertory>();
        _buildingManager = _gameManager.BuildingManager;
        _roadMesh = FindObjectOfType<RoadMesh>();
        _waveInfos = new SpawningWaveInfo[maxWaves];
    }
    private void WaveSetUp()
    {
        //Level 1:
        _waveInfos[0] = new SpawningWaveInfo(0, 3, 5, new List<BuildingInfo>()
        {
            new BuildingInfo(BuildingType.Heart, 1, 0f),
            new BuildingInfo(BuildingType.NormalCell, 1, 1f),
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
        List<Vector2> usedPositions = new List<Vector2>(); // Track used positions
        float startTime = Time.time + waveInfo.WaveDelay; // Set the start time with wave delay
        int turnCount = 0;
        int roadNumb = _invertory.GetPossitiveNumbRoad();
        float maxRoadLength = roadNumb * _grid.NodeDiameter;
        
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
                    // Add necessary components to the new prefab
                }
                
                //Spawned object
                GameObject building = _objectPooling.GetObj(buildingPrefab);
                Building buildingComponent = building.GetComponent<Building>();
                _buildingManager.RegisterBuilding(buildingComponent);
                
                Vector2 spawnedPos = GetRandomPosition(ref maxRoadLength, waveInfo.ZoneRadius, usedPositions);
                Node buildingNode = _grid.NodeFromWorldPosition(spawnedPos);

                buildingComponent.Initialize(buildingNode,_grid, buildingType, spawnedPos);
                building.transform.position = spawnedPos;
                building.SetActive(true);   
                
            }

            turnCount++; // Move to the next building info
        }
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
            Vector2 roundedPos = _grid.NodeFromWorldPosition(firstPos).WorldPosition;
            usedPosition.Add(roundedPos);
           return roundedPos;
        }
        
        int maxAttempt = 100;
        int attempt = 0;
        Vector3 spawnedPos;
        
        float maxLength = Random.Range(buildingBoundary + _grid.NodeRadius, remainRoadLength/ 2f);
       
        do
        {
            spawnedPos = Random.insideUnitCircle * currentZoneRadius;
            Vector2 roundedPos = _grid.NodeFromWorldPosition(spawnedPos).WorldPosition;
            spawnedPos = roundedPos;
            
            float dst = Vector3.Distance(usedPosition[usedPosition.Count -1], spawnedPos);
            if (dst > buildingBoundary + 1f && dst <= maxLength)
            {
                remainRoadLength -= dst;
                usedPosition.Add(spawnedPos);
                return spawnedPos;
            }
            attempt++;
        } while (attempt < maxAttempt);

        Debug.Log("Could not find a random place");
        return _grid.NodeFromWorldPosition(Vector2.zero).WorldPosition;
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

    


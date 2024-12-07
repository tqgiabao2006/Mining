using Unity.Entities;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;



namespace Game._00.Script._07._Car_spawner_system.Building_Spawner_ECS
{

    public class BuildingSpawnerECS_Author_Component : MonoBehaviour, IObserver
    {
        //Vector3 zone center, float radius
        private Dictionary<Vector3, float> _zoneDictionary = new Dictionary<Vector3, float>();
        [Header("Gizmos")] [SerializeField] public bool isGizmos = false;

        [Header("BuildingBase Prefabs")]
        public List<BuildingPrefabPair> BuildingPrefabs = new List<BuildingPrefabPair>();

        [SerializeField] public int maxWaves = 2;
        private SpawningWaveInfo[] _waveInfos;

        [Header("BuildingBase Settings")] [SerializeField]
        public float buildingBoundary = 0.5f;

        public int currentWave = 0;
        private Coroutine _spawnWaveCoroutine;

        private List<Vector2> _usedPositions; //Check current building positions to avoid spawn in the same place

        private ObjectPooling _objectPooling;
        private GridManager _gridManager;
        private RoadMesh _roadMesh;
        private Invertory _invenrtory;
        private BuildingManager _buildingManager;

        private class Baker : Baker<BuildingSpawnerECS_Author_Component>
        {
            public override void Bake(BuildingSpawnerECS_Author_Component author)
            {

            }
        }

        private void Start()
        {
            InitialSetUp();
            WaveSetUp();
            ProcessWave(0); //Testing only

        }

        /// <summary>
        /// Contains all wave info
        /// </summary>
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
            _waveInfos[1] = new SpawningWaveInfo(1, 5, 10, new List<BuildingInfo>()
            {
                new BuildingInfo(BuildingType.Lung, 1, 0f),
                new BuildingInfo(BuildingType.NormalCell, 1, 3f),
                new BuildingInfo(BuildingType.Lung, 1, 4f)
            });
        }

        private void InitialSetUp()
        {
            _objectPooling = GameManager.Instance.ObjectPooling;
            _gridManager = GameManager.Instance.GridManager;
            _invenrtory = FindObjectOfType<Invertory>();
            _buildingManager = GameManager.Instance.BuildingManager;
            _roadMesh = FindObjectOfType<RoadMesh>();
            _waveInfos = new SpawningWaveInfo[maxWaves];

            _usedPositions = new List<Vector2>();
        }

        private void ProcessWave(int currentLevel)
        {
            SpawningWaveInfo waveInfo = _waveInfos[currentLevel];

            if (_spawnWaveCoroutine != null)
            {
                StopCoroutine(_spawnWaveCoroutine);
            }

            // Start the coroutine for spawning
            // _spawnWaveCoroutine = StartCoroutine(SpawnCoroutine(waveInfo));
        }

        /// <summary>
        /// Receiving game state (current level) to spawn wave
        /// </summary>
        /// <param name="data"> int currentLevel </param>
        /// <param name="flag"> string "Update Level" </param>
        public void OnNotified(object data, string flag)
        {

        }
    }

    public struct WaveInfos : IComponentData
    {
        public int MaxWaves;
        public NativeArray<SpawningWaveInfo> Waves;

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
    public struct SpawningWaveInfo
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
    public struct BuildingInfo
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

}
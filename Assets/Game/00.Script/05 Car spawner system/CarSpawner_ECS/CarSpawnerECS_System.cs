using System;
using System.Collections.Generic;
using Game._00.Script._00._Core_Assembly_Def;
using Game._00.Script._02._System_Manager;
using Game._00.Script._02._System_Manager.Observer;
using Game._00.Script._03._Building;
using Game._00.Script._05._Manager;
using Game._00.Script.ECS_Test.FactoryECS;
using Game._00.Script.NewPathFinding;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Object = System.Object;

namespace Game._00.Script._05_Car_spawner_system.CarSpawner_ECS
{
   [BurstCompile]
   [UpdateInGroup (typeof(PresentationSystemGroup))]
    public partial class CarSpawnSystem : SystemBase, IObserver
    {
        private PathRequestManager _pathRequestManager;
        private bool _isNotified = false;
        //use this for parallel spawn waves in different places to spawn multiple car in the same building
        protected override void OnCreate()
        {
            RequireForUpdate<SpawnGameObjectHolder>();
        }
        
        public void OnNotified(object data, string flag)
        {
            if (data is ValueTuple<Node, Node, string> && flag == NotificationFlags.SpawnCar)
            {
                if (!_isNotified) //To avoid duplicate OnNotified call
                {
                    _isNotified = true;
                }
                else
                {
                    return;
                }    
                
                ValueTuple<Node, Node, string> startEndBuildings = (ValueTuple<Node, Node, string>)data;
                Vector3[] waypoints = _pathRequestManager.GetPathWaypoints(startEndBuildings.Item1.WorldPosition, startEndBuildings.Item2.WorldPosition);
                BlobAssetReference<BlobArray<float3>> waypointsBlob = CreateWaypointsBlob(waypoints);
                SpawnCarEntity(startEndBuildings.Item3, new SpawnData()
                {
                    StartPos = new float3(startEndBuildings.Item1.WorldPosition.x, startEndBuildings.Item1.WorldPosition.y, 0),
                    EndPos = new float3(startEndBuildings.Item2.WorldPosition.x, startEndBuildings.Item2.WorldPosition.y, 0),
                    Waypoints = waypointsBlob, 
                });
                _isNotified = false;
            }
        }

        protected override void OnUpdate()
        {
            //Delay time to get the _pathRequestManager because OnCreate() is called before Awake() to initialize the class
            if (_pathRequestManager == null)
            {
                _pathRequestManager = GameManager.Instance.PathRequestManager;
            }
        }
        
        /// <summary>
        /// Using entity manager to run this in main thread, moving car in jobs => more optimized
        /// </summary>
        /// <param name="objectFlags"></param>
        /// <param name="spawnData"></param>
        public void SpawnCarEntity(string objectFlags, SpawnData spawnData)
        {  
            SpawnGameObjectHolder objectHolder = SystemAPI.GetSingleton<SpawnGameObjectHolder>();

            Entity spawnedEntity = Entity.Null;
            if (objectFlags == ObjectFlags.RedBlood)
            {
                spawnedEntity = EntityManager.Instantiate(objectHolder.RedBlood);
            }else if (objectFlags == ObjectFlags.BlueBlood)
            {
                spawnedEntity = EntityManager.Instantiate(objectHolder.BlueBlood);
            }
            float3 spawnPosition = new float3(spawnData.StartPos.x, spawnData.StartPos.y, 0);

            if (!EntityManager.HasComponent<ParkingWaypoints>(spawnedEntity))
            {
                EntityManager.AddBuffer<ParkingWaypoints>(spawnedEntity);
            }
            
            // Set components directly using EntityManager
            EntityManager.SetComponentData(spawnedEntity, new LocalTransform
            {
                Position = spawnPosition,
                Rotation = quaternion.identity,
                Scale = 0.4f
            });

            EntityManager.AddComponentData(spawnedEntity, new FollowPathData()
            {
                WaypointsBlob = spawnData.Waypoints
            });
            
            EntityManager.SetComponentData(spawnedEntity, new CanRun()
            {
                Value = true,
            });
        }
        
        /// <summary>
        /// Convert Vector3[] waypoints to BlobAsset more optimized for ECS and Job system
        /// </summary>
        /// <param name="waypoints"></param>
        /// <returns></returns>
        public BlobAssetReference<BlobArray<float3>> CreateWaypointsBlob(Vector3[] waypoints)
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<BlobArray<float3>>();
                var waypointArray = builder.Allocate(ref root, waypoints.Length);

                for (int i = 0; i < waypoints.Length; i++)
                {
                    waypointArray[i] = new float3(waypoints[i].x, waypoints[i].y, waypoints[i].z);
                }

                return builder.CreateBlobAssetReference<BlobArray<float3>>(Allocator.Persistent);
            }
        }
    }
    
}
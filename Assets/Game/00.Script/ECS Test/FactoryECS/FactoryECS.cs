using System.Collections.Generic;
using Game._00.Script._05._Manager;
using Game._00.Script.NewPathFinding;
using Game._03._Scriptable_Object;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game._00.Script.ECS_Test.FactoryECS
{
    public class FactoryECS : Singleton<FactoryECS>
    {
        public NewPathRequestManager _pathRequestManager;

        private void Start()
        {
            _pathRequestManager = GameManager.Instance.NewPathRequestManager;
        }
        public Entity CreateCarEntity(string objectFlags, FactoryData factoryData)
        {  
            Vector3[] waypoints =  _pathRequestManager.GetPathWaypoints(factoryData.StartPos, factoryData.EndPos);

            BlobAssetReference<BlobArray<float3>> WaypointsBlob = CreateWaypointsBlob(waypoints);
            
            if (objectFlags == ObjectFlags.Car)
            {
                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                Entity carEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(carEntity, new Speed()
                {
                    Value = factoryData.Speed
                });
                entityManager.AddComponentData(carEntity, new FollowPathData()
                {
                    WaypointsBlob = WaypointsBlob
                });
                entityManager.AddComponentData(carEntity, new LocalTransform()
                {
                    Position = factoryData.StartPos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                entityManager.AddComponentData(carEntity, new LocalTransform());
                
                //Lack instantiate an entity to scene
                //Lack instantiate an entity to scene
                //Lack instantiate an entity to scene
                //Lack instantiate an entity to scene
                //Lack instantiate an entity to scene
                //Lack instantiate an entity to scene
                //Lack instantiate an entity to scene
                //Lack instantiate an entity to scene
                //Lack instantiate an entity to scene
                
                return carEntity;
            }
            
            return Entity.Null;
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

    public struct FactoryData
    {
        public float Speed;
        public float3 StartPos;
        public float3 EndPos;
        public GameObject CarPrefab;
    }

    public struct FollowPathData : IComponentData
    {
        public BlobAssetReference<BlobArray<float3>> WaypointsBlob;
        public int CurrentIndex;
    }
    
    public struct Speed : IComponentData
    {
        public float Value;
    }

    public readonly partial struct CarAspect : IAspect
    {
        public readonly RefRO<Speed> Speed;
        public readonly RefRO<FollowPathData> FollowPathData;
        public readonly RefRW<LocalTransform> LocalTransform;
    }
    
    [BurstCompile]
    partial struct FollowPathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CarAspect>();
        }

        public void OnUpdate(ref SystemState state)
        {
            FollowPathJob followPathJob = new FollowPathJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            state.Dependency = followPathJob.ScheduleParallel(state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct FollowPathJob : IJobEntity
    {
        public float DeltaTime;
        public float Speed;

        public void Execute(ref LocalTransform localTransform, ref FollowPathData followPathData, in Speed speed)
        {
            if (!followPathData.WaypointsBlob.IsCreated) return;

            ref BlobArray<float3> waypoints = ref followPathData.WaypointsBlob.Value;
            if (followPathData.CurrentIndex < waypoints.Length)
            {
                float3 nextWaypoint = waypoints[followPathData.CurrentIndex];
                float3 direction = math.normalize(nextWaypoint - localTransform.Position);
                float distanceToMove = speed.Value * DeltaTime;

                localTransform.Position += direction * distanceToMove;

                if (math.distance(localTransform.Position, nextWaypoint) <= 0.05f)
                {
                    followPathData.CurrentIndex++;
                }
            }
        }
    }
    
}
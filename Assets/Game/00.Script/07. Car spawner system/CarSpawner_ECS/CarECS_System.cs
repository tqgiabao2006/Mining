using System;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using Game._00.Script.ECS_Test.FactoryECS;
using Game._00.Script.NewPathFinding;
using Game._03._Scriptable_Object;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game._00.Script.ECS_Test.FactoryECS
{
   
    [BurstCompile]
    partial struct FollowPathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FollowPathData>();
            state.RequireForUpdate<Speed>();
            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<CanRun>();
        }
        public void OnUpdate(ref SystemState state)
        {
            foreach(CarAspect carAspect in SystemAPI.Query<CarAspect>())
            {
                FollowPathJob followPathJob = new FollowPathJob
                {
                    DeltaTime = SystemAPI.Time.DeltaTime
                };
                state.Dependency = followPathJob.ScheduleParallel(state.Dependency);
            }

        }
    }
    [BurstCompile]
    public partial struct FollowPathJob : IJobEntity
    {
        public float DeltaTime;
       // private bool isBackWard
        public void Execute(CarAspect car)
        {
            if (!car.FollowPathData.ValueRO.WaypointsBlob.IsCreated || !car.CheckCanRun()) return;

            ref BlobArray<float3> waypoints = ref car.FollowPathData.ValueRO.WaypointsBlob.Value;
            
            // Check if we're at a valid index within the range of waypoints
            if (car.FollowPathData.ValueRO.CurrentIndex < waypoints.Length)
            {
                float3 nextWaypoint = waypoints[car.FollowPathData.ValueRO.CurrentIndex];
                float3 direction = math.normalize(nextWaypoint - car.LocalTransform.ValueRO.Position);
                
                if (math.distance(car.LocalTransform.ValueRO.Position, nextWaypoint) >= 0.05f) //Avoid null direction
                {
                    car.LocalTransform.ValueRW.Position += direction * car.Speed.ValueRO.Value * DeltaTime;
                }
                else
                {   
                    if (car.FollowPathData.ValueRO.CurrentIndex == 0)
                    {
                        car.FollowPathData.ValueRW.CurrentDirection = 1;
                    }
                    else if (car.FollowPathData.ValueRO.CurrentIndex == waypoints.Length - 1)
                    {
                        car.FollowPathData.ValueRW.CurrentDirection = -1;
                    } 
                    car.FollowPathData.ValueRW.CurrentIndex +=car.FollowPathData.ValueRO.CurrentDirection;
                    
                    
                }
            }
        }
    }

}
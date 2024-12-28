using System.Drawing;
using Game._00.Script.ECS_Test.FactoryECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Color = UnityEngine.Color;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Game._00.Script._05_Car_spawner_system.CarSpawner_ECS
{
    
    [BurstCompile]
    [CreateAfter(typeof(BuildPhysicsWorld))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    partial struct FollowPathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FollowPathData>();
            state.RequireForUpdate<Speed>();
            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<CanRun>();
            state.RequireForUpdate<StopDistance>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }
        public void OnUpdate(ref SystemState state)
        {
            FollowPathJob followPathJob = new FollowPathJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>()
            };
            state.Dependency = followPathJob.ScheduleParallel(state.Dependency);
        }
    }
    [BurstCompile]
    public partial struct FollowPathJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;

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
                
                // Face to the next waypoint:
                float angle = math.atan2(direction.y, direction.x) - 90 * Mathf.Deg2Rad; // -90 because by default, the prefab faces upward
                car.LocalTransform.ValueRW.Rotation = quaternion.Euler(0, 0, angle);
                
                //Change speed when car are nearby
                RaycastInput input = new RaycastInput()
                {
                    Start = car.LocalTransform.ValueRO.Position + direction * car.ColliderBound.ValueRO.Value,
                    End = direction  * car.StopDistance.ValueRO.CheckDst,
                    Filter = CollisionFilter.Default
                };
                
                
                Debug.DrawRay(input.Start,  direction  * car.StopDistance.ValueRO.StopDst, Color.yellow);
                // Debug.DrawRay(input.Start, input.End, Color.red);
                
                //Because when come to corner, the ray cast will check cars from other lane which create endless traffic conjuction
                //So we temporally uncheck when close to corner (math.distance < threshold)
                if (math.distance(car.LocalTransform.ValueRO.Position, nextWaypoint) >= 0.5f)
                {
                    if (PhysicsWorld.CastRay(input, out RaycastHit hit)) //Deceleration
                    {
                        float distance = math.distance(car.LocalTransform.ValueRO.Position, hit.Position);
                        if (distance <= car.StopDistance.ValueRO.StopDst + car.ColliderBound.ValueRO.Value &&
                            distance > car.ColliderBound.ValueRO.Value) //Too close so stop
                        {
                            car.Speed.ValueRW.CurSpeed = 0;
                        }
                        else
                        {
                            float deceleration = (car.Speed.ValueRO.CurSpeed - car.Speed.ValueRO.MinSpeed) / car.Speed.ValueRO.TimeChangeSpeed;
                            float speed = car.Speed.ValueRO.CurSpeed;
                            speed -= deceleration * DeltaTime;
                            car.Speed.ValueRW.CurSpeed = math.clamp(speed, car.Speed.ValueRO.MinSpeed, car.Speed.ValueRO.MaxSpeed);
                        }
                    }
                    else if (car.Speed.ValueRO.CurSpeed < car.Speed.ValueRO.MaxSpeed) //Acceleration
                    {
                        float acceleration= ( car.Speed.ValueRO.MaxSpeed - car.Speed.ValueRO.CurSpeed)/car.Speed.ValueRO.TimeChangeSpeed;
                        float speed = car.Speed.ValueRO.CurSpeed;
                        speed += acceleration * DeltaTime;
                        car.Speed.ValueRW.CurSpeed = math.clamp(speed, car.Speed.ValueRO.MinSpeed, car.Speed.ValueRO.MaxSpeed);
                    }
                }
                
               
                
                
                if (math.distance(car.LocalTransform.ValueRO.Position, nextWaypoint) >= 0.05f) //Avoid null buildingDirection
                {
                    car.LocalTransform.ValueRW.Position += direction * car.Speed.ValueRO.CurSpeed * DeltaTime;
                }
                else
                {
                    if (car.FollowPathData.ValueRO.CurrentIndex == waypoints.Length - 1)
                    {
                        car.FollowPathData.ValueRW.CurrentIndex = 0;
                    }
                    else
                    {
                         
                        car.FollowPathData.ValueRW.CurrentIndex++;
                    }
                }
            }
        }
        
    }

}
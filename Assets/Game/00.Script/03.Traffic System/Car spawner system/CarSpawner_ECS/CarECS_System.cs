using System.IO;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
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

namespace  Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS
{
    [BurstCompile]
    [CreateAfter(typeof(BuildPhysicsWorld))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    partial struct ParkingSystem : ISystem
    {        
        private EntityManager _entityManager;
        public void OnCreate(ref SystemState state)
        {
            // Fundamental components
            state.RequireForUpdate<State>();
            state.RequireForUpdate<Speed>();
            state.RequireForUpdate<LocalTransform>();

            // Path-following components
            state.RequireForUpdate<FollowPathData>();

            // Traffic simulation components
            state.RequireForUpdate<StopDistance>();
            state.RequireForUpdate<ColliderBound>();

            // Parking and waypoint components
            state.RequireForUpdate<ParkingLot>();
            state.RequireForUpdate<EnterExitPoint>();
            state.RequireForUpdate<IsParking>();
            state.RequireForUpdate<ParkingData>();

            // Physics system components
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }


        public void OnUpdate(ref SystemState state)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((CarAspect car, Entity entity) in SystemAPI.Query<CarAspect>().WithEntityAccess())
            {
                if (car.State.ValueRO.Value != CarState.Parking) continue;
                ParkingJob parkingJob = new ParkingJob
                {
                    DeltaTime = deltaTime,
                    PhysicsWorld = physicsWorld,
                };
                parkingJob.ScheduleParallel();
            }
        }
    }
    
    partial struct CarStateTransitionSystem : ISystem
    {
        EntityManager _entityManager;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            
            // Fundamental components
            state.RequireForUpdate<State>();
            state.RequireForUpdate<Speed>();
            state.RequireForUpdate<LocalTransform>();

            // Path-following components
            state.RequireForUpdate<FollowPathData>();

            // Traffic simulation components
            state.RequireForUpdate<StopDistance>();
            state.RequireForUpdate<ColliderBound>();

            // Parking and waypoint components
            state.RequireForUpdate<ParkingLot>();
            state.RequireForUpdate<EnterExitPoint>();
            state.RequireForUpdate<IsParking>();
            state.RequireForUpdate<ParkingData>();
        }


        public void OnUpdate(ref SystemState state)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            foreach ((CarAspect car, Entity entity) in SystemAPI.Query<CarAspect>().WithEntityAccess())
            {
                
                if (car.State.ValueRO.Value == CarState.FollowingPath && 
                    (math.distance(car.LocalTransform.ValueRO.Position, car.EnterExitPoint.ValueRO.BigEnter) <= 0.05f && car.EnterExitPoint.ValueRO.IsForward) //If reach enter large, enter small
                     || (math.distance(car.LocalTransform.ValueRO.Position, car.EnterExitPoint.ValueRO.SmallEnter) <= 0.05f && !car.EnterExitPoint.ValueRO.IsForward))
                {
                    Node node = GridManager.NodeFromWorldPosition(new Vector2(car.LocalTransform.ValueRO.Position.x, car.LocalTransform.ValueRO.Position.y));
                    if (node.BelongedBuilding == null) continue;
    
                    BuildingBase building = node.BelongedBuilding.GetComponent<BuildingBase>();
                    building.GetParkingRequest(entity); // Ensure parking waypoints exist
                    
                    if (!car.ParkingData.ValueRO.HasPath || !car.ParkingData.ValueRO.WaypointsBlob.IsCreated) 
                    {        
                        continue;
                    }

                    car.ParkingData.ValueRW.CurrentIndex = 0; 
                    car.State.ValueRW.Value = CarState.Parking;
                }
                else if (car.State.ValueRO.Value == CarState.Parking &&
                         car.ParkingData.ValueRO.WaypointsBlob.IsCreated)
                {
                    if (car.ParkingData.ValueRO.CurrentIndex >= car.ParkingData.ValueRO.WaypointsBlob.Value.Waypoints.Length - 1)
                    { 
                        car.EnterExitPoint.ValueRW.IsForward = !car.EnterExitPoint.ValueRO.IsForward;
                        car.ParkingData.ValueRW.HasPath = false;
                        car.State.ValueRW.Value = CarState.FollowingPath;
                        
                        //Reset current index of follow path data based on direction 
                        if (!car.EnterExitPoint.ValueRW.IsForward) //If return to small => set to exit of big building
                        {
                            car.FollowPathData.ValueRW.CurrentIndex = car.EnterExitPoint.ValueRO.ExitIndex; // Set to exit waypoint 
                        }
                        else //If from small to big => set = 0;
                        {
                            car.FollowPathData.ValueRW.CurrentIndex = 0;
                        }
                       
                    }
                }
            }
        }
    }

    
    public partial struct ParkingJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        [ReadOnly] public float DeltaTime;
        public void Execute(ref ParkingData parkingData, ref State state, ref Speed speedStats, ref LocalTransform localTransform, in StopDistance stopDistance, in ColliderBound colliderBound )
        {
            if (!parkingData.WaypointsBlob.IsCreated || parkingData.WaypointsBlob.Value.Waypoints.Length == 0)
            {
                return;
            }
            
            ref BlobArray<ParkingWaypoint> waypoints = ref parkingData.WaypointsBlob.Value.Waypoints;
            float3 nextWaypoint = waypoints[parkingData.CurrentIndex].Value;
            float3 direction = math.normalize(nextWaypoint - localTransform.Position);
            
            // Face to the next waypoint:
            float angle = math.atan2(direction.y, direction.x) - 90 * Mathf.Deg2Rad;
            localTransform.Rotation = quaternion.Euler(0, 0, angle);

            if (parkingData.CurrentIndex < waypoints.Length -1)
            {
                // Only perform deceleration and raycast when appropriate (you might have a different threshold for this)
                if (math.distance(localTransform.Position, nextWaypoint) >= 0.5f)
                {
                    RaycastInput input = new RaycastInput()
                    {
                        Start = localTransform.Position + direction * colliderBound.Value,
                        End = localTransform.Position + direction * stopDistance.CheckDst,
                        Filter = CollisionFilter.Default
                    };

                    Debug.DrawRay(input.Start, direction * stopDistance.StopDst, Color.yellow);

                    if (PhysicsWorld.CastRay(input, out RaycastHit hit)) //Deceleration
                    {
                        float distance = math.distance(localTransform.Position, hit.Position);
                        if (distance <= stopDistance.StopDst + colliderBound.Value &&
                            distance > colliderBound.Value)
                        {
                            speedStats.CurSpeed = 0;
                        }
                        else
                        {
                            float deceleration = (speedStats.CurSpeed - speedStats.MinSpeed) /
                                                 speedStats.TimeChangeSpeed;
                            float speed = speedStats.CurSpeed;
                            speed -= deceleration * DeltaTime;
                            speedStats.CurSpeed = math.clamp(speed, speedStats.MinSpeed, speedStats.MaxSpeed);
                        }
                    }
                    else if (speedStats.CurSpeed < speedStats.MaxSpeed) //Acceleration
                    {
                        float acceleration = (speedStats.MaxSpeed - speedStats.CurSpeed) / speedStats.TimeChangeSpeed;
                        float speed = speedStats.CurSpeed;
                        speed += acceleration * DeltaTime;
                        speedStats.CurSpeed = math.clamp(speed, speedStats.MinSpeed, speedStats.MaxSpeed);
                    }
                }

                float distanceToWaypoint = math.distance(localTransform.Position, nextWaypoint);
                if (distanceToWaypoint >= 0.05f) // Still moving towards waypoint
                {
                    localTransform.Position += direction * speedStats.CurSpeed * DeltaTime;
                }
                else // Reached waypoint
                {
                    if (parkingData.CurrentIndex == waypoints.Length - 1)
                    {
                        state.Value = CarState.FollowingPath;
                    }
                    else
                    {
                        parkingData.CurrentIndex++;
                    }
                }
            }
        }

    }
    
    [BurstCompile]
    [CreateAfter(typeof(BuildPhysicsWorld))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    partial struct FollowPathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            
            state.RequireForUpdate<FollowPathData>();
            state.RequireForUpdate<State>();
            state.RequireForUpdate<EnterExitPoint>();
            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<Speed>();
            state.RequireForUpdate<StopDistance>();
            state.RequireForUpdate<ColliderBound>();
        }

        public void OnUpdate(ref SystemState state)
        {
            FollowPathJob followPathJob = new FollowPathJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>(),
            };
            followPathJob.ScheduleParallel();
        }
    }

        
    [BurstCompile]
    public partial struct FollowPathJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        public void Execute(ref FollowPathData followPathData, ref State state, ref EnterExitPoint enterExitPoint, ref LocalTransform localTransform, ref Speed speedStat,in StopDistance stopDistance, in ColliderBound colliderBound)
        {
            
            //Check state before execution
            if (!followPathData.WaypointsBlob.IsCreated || state.Value != CarState.FollowingPath) return;
            
            ref BlobArray<float3> waypoints = ref followPathData.WaypointsBlob.Value;
            //Because this is a rounded path from start -> building -> start, that each waypoint has at least 1 in parallel
            int mid = waypoints.Length / 2;
            int enterIndex = mid-1;
            int exitIndex = mid;
            
            enterExitPoint.EnterIndex = enterIndex;
            enterExitPoint.ExitIndex = exitIndex;
            enterExitPoint.BigEnter = waypoints[enterIndex];
            enterExitPoint.Exit = waypoints[exitIndex];
            
            enterExitPoint.SmallEnter = waypoints[waypoints.Length - 1];   
            
            if (math.distance(localTransform.Position, enterExitPoint.BigEnter) <= 0.05f)
            {
                state.Value = CarState.Parking;
                return;
            }

            if (followPathData.CurrentIndex < waypoints.Length)
            {
                float3 nextWaypoint = waypoints[followPathData.CurrentIndex];
                float3 direction = math.normalize(nextWaypoint - localTransform.Position);

                // Face to the next waypoint:
                float angle = math.atan2(direction.y, direction.x) -
                              90 * Mathf.Deg2Rad; // -90 because by default, the prefab faces upward
                localTransform.Rotation = quaternion.Euler(0, 0, angle);
                //Change speed when car are nearby
                RaycastInput input = new RaycastInput()
                {
                    Start = localTransform.Position + direction * colliderBound.Value,
                    End = direction * stopDistance.CheckDst,
                    Filter = CollisionFilter.Default
                };

                Debug.DrawRay(input.Start, direction * stopDistance.StopDst, Color.yellow);

                //Because when come to corner, the ray cast will check cars from other lane which create endless traffic conjunction
                //So we temporally uncheck when close to corner (math.distance < threshold)
                if (math.distance(localTransform.Position, nextWaypoint) >= 0.5f)
                {
                    if (PhysicsWorld.CastRay(input, out RaycastHit hit)) //Deceleration
                    {
                        float distance = math.distance(localTransform.Position, hit.Position);
                        if (distance <= stopDistance.StopDst + colliderBound.Value &&
                            distance > colliderBound.Value) //Too close so stop
                        {
                            speedStat.CurSpeed = 0;
                        }
                        else
                        {
                            float deceleration = (speedStat.CurSpeed - speedStat.MinSpeed) / speedStat.TimeChangeSpeed;
                            float speed = speedStat.CurSpeed;
                            speed -= deceleration * DeltaTime;
                            speedStat.CurSpeed = math.clamp(speed, speedStat.MinSpeed, speedStat.MaxSpeed);
                        }
                    }
                    else if (speedStat.CurSpeed < speedStat.MaxSpeed) //Acceleration
                    {
                        float acceleration = (speedStat.MaxSpeed - speedStat.CurSpeed) / speedStat.TimeChangeSpeed;
                        float speed = speedStat.CurSpeed;
                        speed += acceleration * DeltaTime;
                        speedStat.CurSpeed = math.clamp(speed, speedStat.MinSpeed, speedStat.MaxSpeed);
                    }
                }

                if (math.distance(localTransform.Position, nextWaypoint) >=
                    0.05f) //Avoid null buildingDirection
                {
                    localTransform.Position += direction * speedStat.CurSpeed * DeltaTime;
                }
                else
                {
                    //When reach final point transition back to parking
                    if (followPathData.CurrentIndex == waypoints.Length - 1)
                    { 
                        state.Value = CarState.Parking;
                    }
                    else
                    {
                        followPathData.CurrentIndex++;
                    }
                }
                
                Debug.Log(followPathData.CurrentIndex);
            }
        }
    }
}
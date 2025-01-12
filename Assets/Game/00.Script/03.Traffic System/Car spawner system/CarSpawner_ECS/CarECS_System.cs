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
            Debug.Log("Parking");
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((CarAspect car, Entity entity) in SystemAPI.Query<CarAspect>().WithEntityAccess())
            {
                DynamicBuffer<ParkingWaypoint> parkingWaypointsBuffer = _entityManager.GetBuffer<ParkingWaypoint>(entity);
                NativeArray<float3> waypoints = new NativeArray<float3>(parkingWaypointsBuffer.Length, Allocator.Temp);
                for(int i = 0; i < parkingWaypointsBuffer.Length; ++i)
                {
                    waypoints[i] = parkingWaypointsBuffer[i].Value;
                }
                ParkingJob parkingJob = new ParkingJob
                {
                    DeltaTime = deltaTime,
                    PhysicsWorld = physicsWorld,
                    Waypoints = waypoints
                };
                waypoints.Dispose();
                parkingJob.ScheduleParallel();
            }
        }
    }
    
    partial struct CarStateTransitionSystem : ISystem
    {
        EntityManager _entityManager;
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
            Debug.Log("Transition");
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            foreach ((CarAspect car, Entity entity) in SystemAPI.Query<CarAspect>().WithEntityAccess())
            {
                if (car.State.ValueRO.Value == CarState.FollowingPath && 
                    math.distance(car.LocalTransform.ValueRO.Position, car.EnterExitPoint.ValueRO.Enter) <= 0.05f)
                { 
                    if (!car.ParkingData.ValueRO.HasPath) 
                    {
                        Node node = GridManager.NodeFromWorldPosition(
                            new Vector2(car.LocalTransform.ValueRO.Position.x, car.LocalTransform.ValueRO.Position.y));
                    
                        if (node.BelongedBuilding == null) continue;
                        BuildingBase building = node.BelongedBuilding.GetComponent<BuildingBase>();
                        
                        //Get request before schedule
                        building.GetParkingRequest(entity);
                        car.ParkingData.ValueRW.HasPath = true;
                    }
                    car.State.ValueRW.Value = CarState.Parking;
                }
                else if (car.State.ValueRO.Value == CarState.Parking &&
                         car.ParkingData.ValueRO.CurrentIndex >= _entityManager.GetBuffer<ParkingWaypoint>(entity).Length)
                {
                    car.State.ValueRW.Value = CarState.FollowingPath;
                    car.FollowPathData.ValueRW.CurrentIndex = car.EnterExitPoint.ValueRO.ExitIndex; // Set to exit waypoint
                }
            }
        }
    }

    
    [BurstCompile]
    public partial struct ParkingJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        [ReadOnly] public NativeArray<float3> Waypoints;
        [ReadOnly] public float DeltaTime;
        public void Execute(CarAspect car)
        {
            //Check state before execution
            if (car.State.ValueRO.Value != CarState.Parking) return;
            
            float3 nextWaypoint = Waypoints[car.ParkingData.ValueRO.CurrentIndex];
            float3 direction = math.normalize(nextWaypoint - car.LocalTransform.ValueRO.Position);
    
            // Face to the next waypoint:
            float angle = math.atan2(direction.y, direction.x) -
                          90 * Mathf.Deg2Rad; // -90 because by default, the prefab faces upward
            car.LocalTransform.ValueRW.Rotation = quaternion.Euler(0, 0, angle);
    
            //Change speed when car are nearby
            RaycastInput input = new RaycastInput()
            {
                Start = car.LocalTransform.ValueRO.Position + direction * car.ColliderBound.ValueRO.Value,
                End = direction * car.StopDistance.ValueRO.CheckDst,
                Filter = CollisionFilter.Default
            };
    
    
            Debug.DrawRay(input.Start, direction * car.StopDistance.ValueRO.StopDst, Color.yellow);
    
            //Because when come to corner, the ray cast will check cars from other lane which create endless traffic conjunction
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
                        float deceleration = (car.Speed.ValueRO.CurSpeed - car.Speed.ValueRO.MinSpeed) /
                                             car.Speed.ValueRO.TimeChangeSpeed;
                        float speed = car.Speed.ValueRO.CurSpeed;
                        speed -= deceleration * DeltaTime;
                        car.Speed.ValueRW.CurSpeed =
                            math.clamp(speed, car.Speed.ValueRO.MinSpeed, car.Speed.ValueRO.MaxSpeed);
                    }
                }
                else if (car.Speed.ValueRO.CurSpeed < car.Speed.ValueRO.MaxSpeed) //Acceleration
                {
                    float acceleration = (car.Speed.ValueRO.MaxSpeed - car.Speed.ValueRO.CurSpeed) /
                                         car.Speed.ValueRO.TimeChangeSpeed;
                    float speed = car.Speed.ValueRO.CurSpeed;
                    speed += acceleration * DeltaTime;
                    car.Speed.ValueRW.CurSpeed = 
                        math.clamp(speed, car.Speed.ValueRO.MinSpeed, car.Speed.ValueRO.MaxSpeed);
                }
                
                if (math.distance(car.LocalTransform.ValueRO.Position, nextWaypoint) >=
                    0.05f) //Avoid null buildingDirection
                {
                    car.LocalTransform.ValueRW.Position += direction * car.Speed.ValueRO.CurSpeed * DeltaTime;
                }
                else
                {
                    car.ParkingData.ValueRW.CurrentIndex++;
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
                FollowPathJob followPathJob = new FollowPathJob
                {
                    DeltaTime = SystemAPI.Time.DeltaTime,
                    PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>(),
                };
                Debug.Log("Has car aspsect");
                // state.Dependency = followPathJob.ScheduleParallel(state.Dependency);
                Test test = new Test();
                test.ScheduleParallel();

            }
        }


        public partial struct Test : IJobEntity
        {
            public void Execute()
            {
                Debug.Log("Test");
            }
        }

    [BurstCompile]
    public partial struct FollowPathJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        public void Execute(CarAspect car)
        {
            //Check state before execution
            if (!car.FollowPathData.ValueRO.WaypointsBlob.IsCreated || !car.CheckCanRun() || car.State.ValueRO.Value != CarState.FollowingPath) return;
         
            ref Unity.Entities.BlobArray<float3> waypoints = ref car.FollowPathData.ValueRO.WaypointsBlob.Value;
            
            //Because this is a rounded path from start -> building -> start, that each waypoint has at least 1 in parallel
            int enterIndex = waypoints.Length / 2;
            int exitIndex = enterIndex + 1;
            
            car.EnterExitPoint.ValueRW.EnterIndex = enterIndex;
            car.EnterExitPoint.ValueRW.ExitIndex = exitIndex;
            car.EnterExitPoint.ValueRW.Enter = waypoints[enterIndex];
            car.EnterExitPoint.ValueRW.Exit = waypoints[exitIndex];

            if (math.distance(car.LocalTransform.ValueRO.Position, car.EnterExitPoint.ValueRO.Enter) <= 0.05f)
            {
                car.State.ValueRW.Value = CarState.Parking;
                return;
            }
            
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
                
                //Because when come to corner, the ray cast will check cars from other lane which create endless traffic conjunction
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


                if (math.distance(car.LocalTransform.ValueRO.Position, nextWaypoint) >=
                    0.05f) //Avoid null buildingDirection
                {
                    car.LocalTransform.ValueRW.Position += direction * car.Speed.ValueRO.CurSpeed * DeltaTime;
                }
                else
                {
                        car.FollowPathData.ValueRW.CurrentIndex++;
                }
            }
        }
    }
    
}
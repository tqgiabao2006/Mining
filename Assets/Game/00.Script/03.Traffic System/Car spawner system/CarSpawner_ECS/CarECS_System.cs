using System.IO;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using Game._00.Script._03.Traffic_System.PathFinding;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Color = UnityEngine.Color;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS
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


            JobHandle parkingJobHandle = new ParkingJob
            {
                DeltaTime = deltaTime,
                PhysicsWorld = physicsWorld,
            }.ScheduleParallel(state.Dependency); // Assign previous dependency
            state.Dependency = parkingJobHandle; // Ensure proper job completion before next update
        }
    }

    public partial class CarStateTransitionSystem : SystemBase
    {
        private EntityManager _entityManager;
        private PathRequestManager _pathRequestManager;

        protected override void OnCreate()
        {
            RequireForUpdate<PhysicsWorldSingleton>();

            // Fundamental components
            RequireForUpdate<State>();
            RequireForUpdate<Speed>();
            RequireForUpdate<LocalTransform>();

            // Path-following components
            RequireForUpdate<FollowPathData>();

            // Traffic simulation components
            RequireForUpdate<StopDistance>();
            RequireForUpdate<ColliderBound>();

            // Parking and waypoint components
            RequireForUpdate<ParkingLot>();
            RequireForUpdate<IsParking>();
            RequireForUpdate<ParkingData>();
        }


        protected override void OnUpdate()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (_pathRequestManager == null)
            {
                _pathRequestManager = PathRequestManager.Instance;
            }

            foreach ((CarAspect car, Entity entity) in SystemAPI.Query<CarAspect>().WithEntityAccess())
            {
                Debug.Log(car.State.ValueRO.Value);
                if (car.State.ValueRO.Value == CarState.FollowingPath
                    && math.distance(car.LocalTransform.ValueRO.Position, car.FollowPathData.ValueRO.WaypointsBlob.Value[car.FollowPathData.ValueRO.WaypointsBlob.Value.Length - 1]) <= 0.05f && car.FollowPathData.ValueRW.CurrentIndex ==
                    car.FollowPathData.ValueRO.WaypointsBlob.Value.Length - 1)
                {
                    Node node = GridManager.NodeFromWorldPosition(new Vector2(car.LocalTransform.ValueRO.Position.x,
                        car.LocalTransform.ValueRO.Position.y));
                    if (node.BelongedBuilding == null) continue;

                    BuildingBase building = node.BelongedBuilding.GetComponent<BuildingBase>();
                    
                    building.GetParkingRequest(entity); // Ensure parking waypoints exist

                    if (!car.ParkingData.ValueRO.WaypointsBlob.IsCreated)
                    {
                        car.ParkingData.ValueRW.WaypointsBlob.Dispose();
                    }

                    if (car.NextDestination.ValueRO.IsGoWork)
                    {
                        car.NextDestination.ValueRW.Business = building.RoadNode.WorldPosition;
                    }
                    
                    car.NextDestination.ValueRW.IsGoWork = !car.NextDestination.ValueRO.IsGoWork;
                    car.ParkingData.ValueRW.CurrentIndex = 0;
                    car.State.ValueRW.Value = CarState.Parking;
                }
                else if (car.State.ValueRO.Value == CarState.Parking &&
                         car.ParkingData.ValueRO.WaypointsBlob.IsCreated)
                {
                    if (car.ParkingData.ValueRO.CurrentIndex ==
                        car.ParkingData.ValueRO.WaypointsBlob.Value.Length - 1 && math.distance(car.LocalTransform.ValueRO.Position, car.ParkingData.ValueRO.WaypointsBlob.Value[car.ParkingData.ValueRO.CurrentIndex]) <= 0.05f)
                    {
                        if (!car.ParkingData.ValueRO.WaypointsBlob.IsCreated)
                        {
                            car.ParkingData.ValueRW.WaypointsBlob.Dispose();
                        }
                        
                        Node node = GridManager.NodeFromWorldPosition(new Vector2(car.LocalTransform.ValueRO.Position.x,
                            car.LocalTransform.ValueRO.Position.y));
                        
                        if (node.BelongedBuilding == null) continue;

                        BuildingBase building = node.BelongedBuilding.GetComponent<BuildingBase>();
                        
                        //Get path
                        Vector3 nextDestination = car.NextDestination.ValueRO.IsGoWork
                            ? car.NextDestination.ValueRO.Business
                            : car.NextDestination.ValueRO.Home; 
                        
                        Vector3[] path = _pathRequestManager.GetPathWaypoints(building.RoadNode.WorldPosition, nextDestination);
                        
                        BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                        ref BlobArray<float3> root = ref builder.ConstructRoot<BlobArray<float3>>();
                        var waypoint = builder.Allocate(ref root, path.Length);

                        for (int i = 0; i < path.Length; i++)
                        {
                            waypoint[i] = path[i];
                        }

                        //Reset and set new data
                        car.FollowPathData.ValueRW.WaypointsBlob = builder.CreateBlobAssetReference<BlobArray<float3>>(Allocator.Persistent); 
                        car.FollowPathData.ValueRW.CurrentIndex = 0;
                        car.State.ValueRW.Value = CarState.FollowingPath;
                    }

                }
            }
        }
    }


    [BurstCompile]
    public partial struct ParkingJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        [ReadOnly] public float DeltaTime;

        public void Execute(ref ParkingData parkingData, ref State state, ref Speed speedStats,
            ref LocalTransform localTransform, in StopDistance stopDistance, in ColliderBound colliderBound)
        {
            if (!parkingData.WaypointsBlob.IsCreated || parkingData.WaypointsBlob.Value.Length == 0 ||
                state.Value != CarState.Parking)
                return;

            ref BlobArray<float3> waypoints = ref parkingData.WaypointsBlob.Value;
            if (parkingData.CurrentIndex >= waypoints.Length)
                return;

            float3 nextWaypoint = waypoints[parkingData.CurrentIndex];
            float3 direction = math.normalize(nextWaypoint - localTransform.Position);
            float distanceToWaypoint = math.distance(localTransform.Position, nextWaypoint);

            AdjustRotation(ref localTransform, direction);
            HandleSpeedAndCollision(ref speedStats, localTransform.Position, direction, stopDistance, colliderBound);
            MoveTowardsWaypoint(ref localTransform, direction, speedStats.CurSpeed, distanceToWaypoint);

            if (distanceToWaypoint < 0.02f && parkingData.CurrentIndex < waypoints.Length - 1)
                parkingData.CurrentIndex++;
        }

        private void AdjustRotation(ref LocalTransform localTransform, float3 direction)
        {
            float angle = math.atan2(direction.y, direction.x) - 90 * Mathf.Deg2Rad;
            localTransform.Rotation = quaternion.Euler(0, 0, angle);
        }

        private void HandleSpeedAndCollision(ref Speed speedStats, float3 position, float3 direction,
            in StopDistance stopDistance, in ColliderBound colliderBound)
        {
            RaycastInput input = new RaycastInput
            {
                Start = position + direction * colliderBound.Value,
                End = position + direction * stopDistance.CheckDst,
                Filter = CollisionFilter.Default
            };

            if (PhysicsWorld.CastRay(input, out RaycastHit hit))
            {
                float distance = math.distance(position, hit.Position);
                if (distance <= stopDistance.StopDst + colliderBound.Value)
                    speedStats.CurSpeed = 0;
                else
                    AdjustSpeed(ref speedStats, -1);
            }
            else
            {
                AdjustSpeed(ref speedStats, 1);
            }
        }

        private void AdjustSpeed(ref Speed speedStats, int direction)
        {
            float speedChange = (speedStats.MaxSpeed - speedStats.MinSpeed) / speedStats.TimeChangeSpeed * DeltaTime;
            speedStats.CurSpeed = math.clamp(speedStats.CurSpeed + (speedChange * direction), speedStats.MinSpeed,
                speedStats.MaxSpeed);
        }

        private void MoveTowardsWaypoint(ref LocalTransform localTransform, float3 direction, float speed,
            float distance)
        {
            if (distance >= 0.02f)
                localTransform.Position += direction * speed * DeltaTime;
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

            public void Execute(ref FollowPathData followPathData, ref State state, ref LocalTransform localTransform,
                ref Speed speedStat, in StopDistance stopDistance, in ColliderBound colliderBound)
            {
                if (!followPathData.WaypointsBlob.IsCreated || state.Value != CarState.FollowingPath)
                    return;

                ref BlobArray<float3> waypoints = ref followPathData.WaypointsBlob.Value;
                if (followPathData.CurrentIndex >= waypoints.Length)
                    return;

                float3 nextWaypoint = waypoints[followPathData.CurrentIndex];
                float3 direction = math.normalize(nextWaypoint - localTransform.Position);
                float distanceToWaypoint = math.distance(localTransform.Position, nextWaypoint);

                AdjustRotation(ref localTransform, direction);
                HandleSpeedAndCollision(ref speedStat, localTransform.Position, direction, stopDistance, colliderBound);
                MoveTowardsWaypoint(ref localTransform, direction, speedStat.CurSpeed, distanceToWaypoint);

                if (distanceToWaypoint < 0.05f && followPathData.CurrentIndex < waypoints.Length - 1)
                    followPathData.CurrentIndex++;
            }

            private void AdjustRotation(ref LocalTransform localTransform, float3 direction)
            {
                float angle = math.atan2(direction.y, direction.x) - 90 * Mathf.Deg2Rad;
                localTransform.Rotation = quaternion.Euler(0, 0, angle);
            }

            private void HandleSpeedAndCollision(ref Speed speedStat, float3 position, float3 direction,
                in StopDistance stopDistance, in ColliderBound colliderBound)
            {
                RaycastInput input = new RaycastInput
                {
                    Start = position + direction * colliderBound.Value,
                    End = position + direction * stopDistance.CheckDst,
                    Filter = CollisionFilter.Default
                };

                if (PhysicsWorld.CastRay(input, out RaycastHit hit))
                {
                    float distance = math.distance(position, hit.Position);
                    if (distance <= stopDistance.StopDst + colliderBound.Value)
                        speedStat.CurSpeed = 0;
                    else
                        AdjustSpeed(ref speedStat, -1);
                }
                else
                {
                    AdjustSpeed(ref speedStat, 1);
                }
            }

            private void AdjustSpeed(ref Speed speedStat, int direction)
            {
                float speedChange = (speedStat.MaxSpeed - speedStat.MinSpeed) / speedStat.TimeChangeSpeed * DeltaTime;
                speedStat.CurSpeed = math.clamp(speedStat.CurSpeed + (speedChange * direction), speedStat.MinSpeed,
                    speedStat.MaxSpeed);
            }

            private void MoveTowardsWaypoint(ref LocalTransform localTransform, float3 direction, float speed,
                float distance)
            {
                if (distance >= 0.05f)
                    localTransform.Position += direction * speed * DeltaTime;
            }
        }
    }

using Game._00.Script._00.Manager.Custom_Editor;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS
{
    public enum CarState
    {
        FollowingPath,
        Parking
    }
    public class CarECS_Author_Component: MonoBehaviour
    { 
        [CustomReadOnly] [SerializeField] private float miningTime = 2f; 
         [SerializeField] private float maxSpeed = 2f; 
        [SerializeField] private float minSpeed = 0.5f; 
        [CustomReadOnly] [SerializeField] private float timeChangeSpeed = 0.5f;
        [SerializeField] private float stopDistance = 0.2f;
        [SerializeField] private float checkDistance = 0.5f;
        [SerializeField] private float colliderBound = 0.7f;
        private class Baker: Baker<CarECS_Author_Component>
        {
            public override void Bake(CarECS_Author_Component author)
            {
                Entity entity = GetEntity(TransformUsageFlags.Renderable);
                DependsOn(author.transform);

                if (author.maxSpeed <= 0 || author.miningTime <= 0 || author.stopDistance <= 0)
                {
                    return;
                }

                //Core components
                AddComponent(entity, new State());
                AddComponent(entity, new Self()
                {
                    Value = entity,
                });

                // Follow path components
                AddComponent(entity, new Speed()
                {
                    CurSpeed = author.maxSpeed,
                    MaxSpeed = author.maxSpeed,
                    MinSpeed = author.minSpeed,
                    TimeChangeSpeed = author.timeChangeSpeed
                });
    
                AddComponent(entity, new FollowPathData
                {
                    CurrentIndex = 0,
                    WaypointsBlob = BlobAssetReference<BlobArray<float3>>.Null // Initialize later in system logic
                });

                // Traffic simulation components
                AddComponent(entity, new CanRun() { Value = true });
                AddComponent(entity, new StopDistance()
                {
                    StopDst = author.stopDistance,
                    CheckDst = author.checkDistance
                });
                AddComponent(entity, new ColliderBound() { Value = author.colliderBound });

                // Parking components
                AddComponent(entity, new ParkingData { CurrentIndex = 0, HasPath = false });
                AddComponent(entity, new EnterExitPoint()
                {
                    IsForward = true
                });
                AddComponent(entity, new IsParking());
                AddComponent(entity, new ParkingLot());

            }    
        }
    }

    public struct State : IComponentData
    {
        public CarState Value;
    }

   
    public struct Speed : IComponentData
    {
        public float MaxSpeed;
        public float CurSpeed;
        public float MinSpeed;
        public float TimeChangeSpeed;
    }
    public struct FollowPathData : IComponentData
    {
        public BlobAssetReference<BlobArray<float3>> WaypointsBlob;
        public int CurrentIndex;
    }

    public struct ParkingWaypoint
    {
        public float3 Value;
    }

    public struct ParkingData : IComponentData
    {
        public BlobAssetReference<ParkingWaypointBlob> WaypointsBlob;
        public int CurrentIndex;
        public bool HasPath;
    }

    public struct ParkingWaypointBlob
    {
        public BlobArray<ParkingWaypoint> Waypoints;
    }


    public struct ParkingLot : IComponentData
    {
        public Building.ParkingLot Value;
    }

    /// <summary>
    /// Use for store enter, exit wayPoint in floa3[] waypoints to transition from following path to following parking lot
    /// </summary>
    public struct EnterExitPoint : IComponentData
    {
        public float3 BigEnter; //Enter large building
        public float3 SmallEnter; //Enter back to small house
        public float3 Exit;
        
        public int EnterIndex;
        public int ExitIndex;

        public bool IsForward; //Check the direction is it entering or returning back
    }

    public struct IsParking : IComponentData
    {
        public bool Value;
    }
    
    public struct CanRun : IComponentData
    {
        public bool Value;
    }
    
    public struct ColliderBound : IComponentData
    {
        public float Value;
    }

    public struct Self : IComponentData
    {
        public Entity Value;
    }

    public struct StopDistance : IComponentData
    {
        public float StopDst;  // Use to stop
        public float CheckDst; // Use to check for accelerating, decelerating
    }
    

}
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._03.Traffic_System.Building;
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
        Parking,
        Mining,
        Idle
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
                AddComponent(entity, new State()
                {
                    Value = CarState.Idle
                });
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
                AddComponent(entity, new IsParking());
                AddComponent(entity, new ParkingLot());
                AddComponent(entity, new MiningTime()
                {
                    Value = 2f
                });
                
                AddComponent(entity, new NextDestination());

            }    
        }
    }

    public struct State : IComponentData
    {
        public CarState Value;
    }

    public struct NextDestination : IComponentData
    {
        public Vector3 Business;
        public Vector3 Home;
        public bool IsGoWork;
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


    public struct ParkingData : IComponentData
    {
        public BlobAssetReference<BlobArray<float3>> WaypointsBlob;
        public float3 ParkingPos;
        public int CurrentIndex;
        public bool HasPath;
    }

    public struct MiningTime: IComponentData
    {
        public float Value;
    }


    public struct ParkingLot : IComponentData
    {
        public Building.ParkingLot Value;
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
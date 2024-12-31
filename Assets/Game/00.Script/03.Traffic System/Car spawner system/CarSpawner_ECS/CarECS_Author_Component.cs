using Game._00.Script._00.Manager.Custom_Editor;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS
{
    public class CarECS_Author_Component: MonoBehaviour
    { 
        [CustomReadOnly] [SerializeField] private float miningTime = 2f; 
        [CustomReadOnly] [SerializeField] private float maxSpeed = 2f; 
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
                
                AddComponent(entity, new Speed()
                {
                    CurSpeed = author.maxSpeed,
                    MaxSpeed = author.maxSpeed,
                    MinSpeed = author.minSpeed,
                    TimeChangeSpeed = author.timeChangeSpeed
                });
                AddComponent(entity, new CanRun()
                {
                    Value = false
                });
                AddComponent(entity, new StopDistance()
                {
                   StopDst  = author.stopDistance,
                   CheckDst = author.checkDistance
                });
                AddComponent(entity, new ColliderBound()
                {
                    Value = author.colliderBound
                });
            }    
        }
    }

    public struct ColliderBound : IComponentData
    {
        public float Value;
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

    [InternalBufferCapacity(6)] //Max element is 6 ways point from the road node to parking lot to road node
    public struct ParkingWaypoints : IBufferElementData
    {
        public float3 Value;
    }
    
    public struct CanRun : IComponentData
    {
        public bool Value;
    }

    public struct StopDistance : IComponentData
    {
        public float StopDst;  // Use to stop
        public float CheckDst; // Use to check for accelerating, decelerating
    }
    
    [WithAll]
    public readonly partial struct CarAspect: IAspect 
    {
        public readonly RefRW<Speed> Speed;
        public readonly RefRW<FollowPathData> FollowPathData;
        public readonly RefRW<LocalTransform> LocalTransform;
        public readonly RefRO<CanRun> CanRun;
        public readonly RefRO<StopDistance> StopDistance;
        public readonly RefRO<ColliderBound> ColliderBound;
        
        public bool CheckCanRun()
        {
            return CanRun.ValueRO.Value;
        }
    }

}
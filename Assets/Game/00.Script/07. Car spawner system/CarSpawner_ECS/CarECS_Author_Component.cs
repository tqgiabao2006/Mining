using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;



namespace Game._00.Script.ECS_Test.FactoryECS
{
    public class CarECS_Author_Component: MonoBehaviour
    { 
        [SerializeField] public float speed;
        [SerializeField] public float miningTime;
        private class Baker: Baker<CarECS_Author_Component>
        {
            public override void Bake(CarECS_Author_Component author)
            {
                Entity entity = GetEntity(TransformUsageFlags.Renderable);
                DependsOn(author.transform);

                if (author.speed == 0 || author.miningTime == 0)
                {
                    return;
                }
                
                AddComponent(entity, new Speed()
                {
                    Value = author.speed
                });
                AddComponent(entity, new LocalTransform()
                {
                  Rotation  = quaternion.identity,
                  Scale = 1f
                });
                AddComponent(entity, new CanRun()
                {
                    Value = false
                });
            }    
        }
    }
    public struct Speed : IComponentData
    {
        public float Value;
    }
    public struct FollowPathData : IComponentData
    {
        public BlobAssetReference<BlobArray<float3>> WaypointsBlob;
        public int CurrentIndex;
        public int CurrentDirection; //1 = move forward, -1 = move backward
    }

    public struct CanRun : IComponentData
    {
        public bool Value;
    }
    
    [WithAll]
    public readonly partial struct CarAspect: IAspect 
    {
        public readonly RefRO<Speed> Speed;
        public readonly RefRW<FollowPathData> FollowPathData;
        public readonly RefRW<LocalTransform> LocalTransform;
        public readonly RefRO<CanRun> CanRun;

        public bool CheckCanRun()
        {
            return CanRun.ValueRO.Value;
        }
    }

}
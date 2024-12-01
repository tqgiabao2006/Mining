using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game._00.Script.ECS_Test
{
    public class RotateECS: MonoBehaviour
    {
        public float Speed = 3f;

        private class Baker: Baker<RotateECS>
        {
            public override void Bake(RotateECS authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new  RotateSpeedComponennt()
                {
                    Value = authoring.Speed
                });
            }
        }
    }
    
    partial struct RotateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RotateSpeedComponennt>();
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (RotateAspect rotateAspect in SystemAPI.Query<RotateAspect>())
            {
                rotateAspect.RotateObjet(SystemAPI.Time.DeltaTime);
            }

            RotateJob rotateJob = new RotateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };
            rotateJob.Schedule();
        }

        public partial struct RotateJob : IJobEntity
        {
            public float DeltaTime;
            public void Execute(ref LocalTransform localTransform, in RotateSpeedComponennt rotateSpeed)
            {
                localTransform = localTransform.RotateZ(rotateSpeed.Value * DeltaTime);
            }
        }
    }

    public struct RotateSpeedComponennt : IComponentData
    {
        public float Value;
    }

    public readonly partial struct RotateAspect : IAspect
    {
        public readonly RefRO<RotateSpeedComponennt> rotateSpeed;
        public readonly RefRW<LocalTransform> localTransform;

        public void RotateObjet(float deltaTime)
        {
            localTransform.ValueRW = localTransform.ValueRW.RotateZ(rotateSpeed.ValueRO.Value * deltaTime);
        }
    }
}
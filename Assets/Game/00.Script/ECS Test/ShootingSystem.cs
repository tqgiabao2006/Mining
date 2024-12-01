using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game._00.Script.ECS_Test
{
    public partial class ShootingSystem: SystemBase
    {
        public event EventHandler OnShoot;
        protected override void OnCreate()
        {
            RequireForUpdate<Player>();
        }
        [BurstCompile]
        protected override void OnUpdate()
        {
            if (!Input.GetKeyDown(KeyCode.Space)) return;

            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator. Temp);
            ConfigSpawnerComponent configSpawner = SystemAPI.GetSingleton<ConfigSpawnerComponent>();
            
            foreach (RefRO<LocalTransform> localTransform in SystemAPI.Query<RefRO<LocalTransform>>())
            {
                ConfigSpawnerComponent spawnerConfig = SystemAPI.GetSingleton<ConfigSpawnerComponent>();
                for (int i = 0; i < spawnerConfig.NumbSpawn; i++)
                {
                    Entity prefabEntity = entityCommandBuffer.Instantiate(spawnerConfig.PrefabEntity);
                    
                    entityCommandBuffer.SetComponent(prefabEntity, new LocalTransform()
                    {
                        Position = new float3(UnityEngine.Random.Range(-10,10), UnityEngine.Random.Range(-6,6), 0),
                        Rotation =  Quaternion.identity,
                        Scale =  0.5f
                    });
                    OnShoot?.Invoke(this, EventArgs.Empty);

                }
            }
            entityCommandBuffer.Playback(EntityManager);
        }
    }
}
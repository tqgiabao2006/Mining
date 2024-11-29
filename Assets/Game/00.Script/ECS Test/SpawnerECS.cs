using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class SpawnerECS : MonoBehaviour
{
    public GameObject prefab;
    public int numbSpawn;
    private class Baker : Baker<SpawnerECS>
    {
        public override void Bake(SpawnerECS spawner)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ConfigSpawnerComponent()
            {
                PrefabEntity = GetEntity(spawner.prefab, TransformUsageFlags.Dynamic),
                NumbSpawn = spawner.numbSpawn, 
                
            });
        }
    }
}

partial struct SpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigSpawnerComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        ConfigSpawnerComponent spawnerConfig = SystemAPI.GetSingleton<ConfigSpawnerComponent>();
        for (int i = 0; i < spawnerConfig.NumbSpawn; i++)
        {
           Entity prefabEntity =  state.EntityManager.Instantiate(spawnerConfig.PrefabEntity);
           SystemAPI.SetComponent(prefabEntity, new LocalTransform()
           {
               Position = new float3(UnityEngine.Random.Range(-10,10), UnityEngine.Random.Range(-6,6), 0),
               Rotation =  Quaternion.identity,
               Scale =  0.5f
           });
        }

        state.Enabled = false;
    }
    
}



public struct ConfigSpawnerComponent : IComponentData
{
    public Entity PrefabEntity;
    public int NumbSpawn;
}
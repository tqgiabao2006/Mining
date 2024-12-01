using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game._00.Script.ECS_Test
{
    public class PlayerECS:MonoBehaviour
    {
        [SerializeField] public GameObject testPrefab;
        private void Start()
        {
           ShootingSystem shootingSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ShootingSystem>();

           shootingSystem.OnShoot += ShootingSystem_OnShoot;
        }

        private void ShootingSystem_OnShoot(object sender, EventArgs e)
        {
            Entity playerEntity = (Entity)sender;
           LocalTransform localPos = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<LocalTransform>(playerEntity);
           Instantiate(testPrefab, localPos.Position, quaternion.identity);
        }
        public class Baker : Baker<PlayerECS>
        {
            public override void Bake(PlayerECS authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Player());
        }   }
        
    }

    partial struct Player : IComponentData
    {
        
    }
}
using Unity.Entities;
using UnityEngine;

namespace Game._00.Script._07._Car_spawner_system.Building_Spawner_ECS
{
    public class BuildingSpawnerECS_Author:MonoBehaviour
    {
        
          private class Baker : Baker<BuildingSpawnerECS_Author>
        {
            public override void Bake(BuildingSpawnerECS_Author authoring)
            {
                
            }
        }
    }
}
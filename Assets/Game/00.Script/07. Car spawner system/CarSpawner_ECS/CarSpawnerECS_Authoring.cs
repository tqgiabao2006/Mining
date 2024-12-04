
using System;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using Game._00.Script.NewPathFinding;
using Game._03._Scriptable_Object;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms;

namespace Game._00.Script.ECS_Test.FactoryECS
{
  public class CarSpawnerECS_Authoring: MonoBehaviour
    {
        private PathRequestManager _pathRequestManager;
        public PrefabManager prefabManager;

        public GameObject prefab1;
        public GameObject prefab2;
        
        private void Start()
        {
            _pathRequestManager = GameManager.Instance.PathRequestManager;
        }
        
        private class Baker: Baker<CarSpawnerECS_Authoring>
        {
            public override void Bake(CarSpawnerECS_Authoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnGameObjectHolder()
                {
                    Entity1 = GetEntity(authoring.prefab1, TransformUsageFlags.Dynamic),
                    Entity2 = GetEntity(authoring.prefab2, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
  
  
}
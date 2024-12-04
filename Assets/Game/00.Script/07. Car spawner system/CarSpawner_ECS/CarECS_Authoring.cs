using UnityEngine;
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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;


namespace Game._00.Script.ECS_Test.FactoryECS
{
    public class CarECS_Authoring: MonoBehaviour
    { 
        [SerializeField] public float speed;
        [SerializeField] public float miningTime;
        private class Baker: Baker<CarECS_Authoring>
        {
            public override void Bake(CarECS_Authoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Speed()
                {
                    Value = authoring.speed
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
}
using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._03.Traffic_System.PathFinding;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using Unity.Physics;
using Unity.Transforms;
using BlobBuilder = Unity.Entities.BlobBuilder;

namespace  Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS
{
   [BurstCompile]
   [UpdateInGroup (typeof(PresentationSystemGroup))]
    public partial class CarSpawnSystem : SystemBase, IObserver
    {
        private bool _isNotified = false;
        //use this for parallel spawn waves in different places to spawn multiple car in the same building
        protected override void OnCreate()
        {
            Debug.Log("CarSpawnSystem.OnCreate");
            RequireForUpdate<SpawnGameObjectHolder>();
        }

     
        public void OnNotified(object data, string flag)
        {
            if (data is not SpawnCarRequest || flag != NotificationFlags.SPAWN_CAR)
                return;
            
            if (!_isNotified) //To avoid duplicate OnNotified call
            {
                _isNotified = true;
            }
            else
            {
                return;
            }
            SpawnCarRequest request = (SpawnCarRequest)data;

            SpawnCarEntity(request);  
            _isNotified = false;
       
        }

        protected override void OnUpdate()
        {
           
        }
        
        /// <summary>
        /// Using entity manager to run this in main thread, moving car in jobs => more optimized
        /// </summary>
        /// <param name="objectFlags"></param>
        /// <param name="spawnData"></param>
        public void SpawnCarEntity(SpawnCarRequest spawnData)
        {  
            SpawnGameObjectHolder objectHolder = SystemAPI.GetSingleton<SpawnGameObjectHolder>();

            Entity spawnedEntity = Entity.Null;
            if (spawnData.ObjectFlag== ObjectFlags.RED_CAR)
            {
                spawnedEntity = EntityManager.Instantiate(objectHolder.RedCar);
            }else if (spawnData.ObjectFlag == ObjectFlags.BLUE_CAR)
            {
                spawnedEntity = EntityManager.Instantiate(objectHolder.BlueCar);
            }
            
            spawnData.Home.AddCarEntity(spawnedEntity);
            
            // Set components directly using EntityManager
            EntityManager.SetComponentData(spawnedEntity, new LocalTransform
            {
                Position = spawnData.StartNodePosition,
                Rotation = spawnData.Rotation,
                Scale = 1
            });
            
            EntityManager.SetComponentData(spawnedEntity, new CanRun()
            {
                Value = true,
            });
            
            EntityManager.SetComponentData(spawnedEntity, new NextDestination()
            {
               Home = spawnData.StartNodePosition, 
               IsGoWork =  true
            });
        }
    }
    
}
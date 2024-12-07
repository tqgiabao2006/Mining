using System;
using System.Linq;
using Unity.VisualScripting;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script.ECS_Test.FactoryECS;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game._00.Script._05._Manager
{
    
    public enum BuildingType
    {
        Heart,
        Lung,
        Stomach,
        
        NormalCell,
        None
    }

    
    public class BuildingManager: SubjectBase, IObserver
    {
        public GameObject Prefab;
        public CarSpawnSystem CarSpawnSystem;
        //Directed graph => adjacent list => building type + its output
        private Dictionary<BuildingType, List<BuildingType>> _outputMap = new Dictionary<BuildingType, List<BuildingType>>();
       
        private Dictionary<BuildingType, List<BuildingBase>> _currentBuildings = new Dictionary<BuildingType, List<BuildingBase>>();
        public Dictionary<BuildingType, List<BuildingBase>> CurrentBuildings
        {
            get => _currentBuildings;
        } 
        
        private List<BuildingBase> _unconnectedBuildings = new List<BuildingBase>();
        private List<BuildingBase> _connectedBuildings = new List<BuildingBase>();
        
        private void Awake()
        {
            InitialInputOutputMap();
            ObserversSetup();
        }
        private void InitialInputOutputMap()
        {
            _outputMap.Add(BuildingType.Lung, new List<BuildingType>() { BuildingType.NormalCell, BuildingType.Heart });
            _outputMap.Add(BuildingType.Heart, new List<BuildingType>() { BuildingType.Lung, BuildingType.NormalCell });
            _outputMap.Add(BuildingType.NormalCell, new List<BuildingType>() { BuildingType.Heart });
        }
        
        public void RegisterBuilding(BuildingBase buildingBase)
        {
            if (_currentBuildings.ContainsKey(buildingBase.BuildingType))
            {
                _currentBuildings[buildingBase.BuildingType].Add(buildingBase);
            }
            else
            {
                _currentBuildings.Add(buildingBase.BuildingType, new List<BuildingBase>() { buildingBase });
            }
            _unconnectedBuildings.Add(buildingBase);
        }

        public List<BuildingBase> GetOutputBuildings(BuildingType buildingType)
        {
            List<BuildingBase> buildings = new List<BuildingBase>();
            List<BuildingType> buildingTypes = _outputMap[buildingType];
            foreach (BuildingType type in buildingTypes)
            {
                if (_currentBuildings.TryGetValue(type, out var building))
                {
                    buildings.AddRange(building);
                }
            }
            return buildings;   
        }
        public override void ObserversSetup()
        {
            // Get the CarSpawnSystem
            IObserver spawnSystemInstance = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CarSpawnSystem>();
            if (spawnSystemInstance != null)
            {
                _observers.Add(spawnSystemInstance);
            }
        }

        /// <summary>
        /// Notify ECS Spawner System to spawn car find path direction in it
        /// </summary>
        /// <param name="data"></param>
        /// <param name="flag"></param>
        public void OnNotified(object data, string flag)
        {
            if (flag != NotificationFlags.CheckingConnection ||
                data is not (ValueTuple<Func<List<BuildingBase>, BuildingBase, BuildingBase>, BuildingBase>)) return;
            
            ValueTuple<Func<List<BuildingBase>, BuildingBase, BuildingBase>, BuildingBase> givenData = (ValueTuple<Func<List<BuildingBase>, BuildingBase,BuildingBase>, BuildingBase>) data;
            
            if(givenData.Item2== null) //Check all
            { 
                List<BuildingBase> removedNodes = new List<BuildingBase>();
                
                foreach (BuildingBase building in _unconnectedBuildings)
                {
                    BuildingBase closestBuilding = givenData.Item1(GetOutputBuildings(building.BuildingType), building);
                    if (closestBuilding && !_connectedBuildings.Contains(closestBuilding)) //Avoid double check 2 building connected
                    {
                        Debug.Log("Notify ECS Spawner");
                        Notify((building, closestBuilding), NotificationFlags.SpawnCar);
                        removedNodes.Add(building);
                        _connectedBuildings.Add(building);
                    }
                }
                //Remove connected building
                foreach (BuildingBase building in removedNodes)
                {
                    _unconnectedBuildings.Remove(building);
                }
            }
            else //Check specific
            {
                Debug.Log("Check specific");

                BuildingBase closestBuilding = givenData.Item1(GetOutputBuildings(givenData.Item2.BuildingType), givenData.Item2);
                if (closestBuilding)
                {
                    Notify((givenData.Item2, closestBuilding), NotificationFlags.SpawnCar);
                    _connectedBuildings.Remove(givenData.Item2);
                    _connectedBuildings.Add(givenData.Item2);
                }
            }
       
            
        }
    }
}
using System;
using System.Linq;
using Unity.VisualScripting;
using System.Collections;
using System.Collections.Generic;
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
        //Directed graph => adjacent list => building type + its output
        private Dictionary<BuildingType, List<BuildingType>> _outputMap = new Dictionary<BuildingType, List<BuildingType>>();
       
        private Dictionary<BuildingType, List<Building>> _currentBuildings = new Dictionary<BuildingType, List<Building>>();
        public Dictionary<BuildingType, List<Building>> CurrentBuildings
        {
            get => _currentBuildings;
        } 
        
        private List<Building> _unconnectedBuildings = new List<Building>();
        private List<Building> _connectedBuildings = new List<Building>();
        private void Awake()
        {
            InitialInputOutputMap();
        }
        private void InitialInputOutputMap()
        {
            _outputMap.Add(BuildingType.Lung, new List<BuildingType>() { BuildingType.NormalCell, BuildingType.Heart });
            _outputMap.Add(BuildingType.Heart, new List<BuildingType>() { BuildingType.Lung, BuildingType.NormalCell });
            _outputMap.Add(BuildingType.NormalCell, new List<BuildingType>() { BuildingType.Heart });
        }
        
        public void RegisterBuilding(Building building)
        {
            if (_currentBuildings.ContainsKey(building.BuildingType))
            {
                _currentBuildings[building.BuildingType].Add(building);
            }
            else
            {
                _currentBuildings.Add(building.BuildingType, new List<Building>() { building });
            }
            _unconnectedBuildings.Add(building);
        }

        public List<Building> GetOutputBuildings(BuildingType buildingType)
        {
            List<Building> buildings = new List<Building>();
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
            // Sync the observers list and currentbuilding list
            var observerSet = new HashSet<IObserver>(_observers);

            foreach (var buildingList in _currentBuildings.Values)
            {
                foreach (var building in buildingList)
                {
                    if (observerSet.Add(building))
                    {
                        _observers.Add(building);
                        Attach(building);
                    }
                }
            }
        }

        public void OnNotified(object data, string flag)
        {
            
            if (flag != NotificationFlags.CheckingConnection ||
                data is not (ValueTuple<Func<List<Building>, Building, bool>, Building>)) return;
            
            ValueTuple<Func<List<Building>, Building, bool>, Building> givenData = (ValueTuple<Func<List<Building>, Building, bool>, Building>) data;
            
            if(givenData.Item2== null) //Check all
            { 
                List<Building> removedNodes = new List<Building>();
                
                foreach (Building building in _unconnectedBuildings)
                {
                    if (givenData.Item1(GetOutputBuildings(building.BuildingType), building))
                    {
                        NotifySpecific(true, NotificationFlags.SpawnCar, building.GetComponent<IObserver>());
                        removedNodes.Add(building);
                        _connectedBuildings.Add(building);
                    }
                }

                //Remove connected building
                foreach (Building building in removedNodes)
                {
                    _unconnectedBuildings.Remove(building);
                }
            }
            else //Check specific
            {
                if (givenData.Item1(_unconnectedBuildings, givenData.Item2))
                {
                    NotifySpecific(true, NotificationFlags.SpawnCar, givenData.Item2.GetComponent<IObserver>());
                    _connectedBuildings.Remove(givenData.Item2);
                    _connectedBuildings.Add(givenData.Item2);
                }
            }
       
            
        }
    }
}
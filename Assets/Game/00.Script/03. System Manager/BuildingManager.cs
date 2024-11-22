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
            // Sync the observers list and currentbuilding list
            var observerSet = new HashSet<IObserver>(_observers);

            foreach (var buildingList in _currentBuildings.Values)
            {
                foreach (var building in buildingList)
                {
                    // Ensure building is an IObserver
                    if (building is IObserver observer)
                    {
                        if (observerSet.Add(observer))
                        {
                            _observers.Add(observer);
                            Attach(observer);
                        }
                    }
                }
            }
            
            _observers.Clear();
            _observers.AddRange(observerSet);
        }

        public void OnNotified(object data, string flag)
        {
            
            if (flag != NotificationFlags.CheckingConnection ||
                data is not (ValueTuple<Func<List<BuildingBase>, BuildingBase, bool>, BuildingBase>)) return;
            
            ValueTuple<Func<List<BuildingBase>, BuildingBase, bool>, BuildingBase> givenData = (ValueTuple<Func<List<BuildingBase>, BuildingBase, bool>, BuildingBase>) data;
            
            if(givenData.Item2== null) //Check all
            { 
                List<BuildingBase> removedNodes = new List<BuildingBase>();
                
                foreach (BuildingBase building in _unconnectedBuildings)
                {
                    if (givenData.Item1(GetOutputBuildings(building.BuildingType), building))
                    {
                        NotifySpecific(true, NotificationFlags.SpawnCar, building.GetComponent<IObserver>());
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
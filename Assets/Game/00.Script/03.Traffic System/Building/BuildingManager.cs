using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS;
using Unity.Entities;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.Building
{

    public class BuildingManager: SubjectBase, IObserver
    {
        //Directed graph => adjacent list => building type + its output
        private Dictionary<BuildingType, List<BuildingType>> _outputMap = new Dictionary<BuildingType, List<BuildingType>>();
       
        private Dictionary<BuildingType, List<BuildingBase>> _currentBuildings = new Dictionary<BuildingType, List<BuildingBase>>();
        public Dictionary<BuildingType, List<BuildingBase>> CurrentBuildings
        {
            get => _currentBuildings;
        }
        private Dictionary<BuildingType, CarSpawnInfo> _carSpawnInfos;
        
        private Dictionary<GameObject, List<Node>>_unconnectedBuildings = new Dictionary<GameObject, List<Node>>();
        
        //Use dictionary because two roads can be connected but not to others
        private Dictionary<int, List<BuildingBase>> _connectedBuildings = new Dictionary<int, List<BuildingBase>>();
        
        
        private void Awake()
        {
            InputOutputMapSetup();
            ObserversSetup();
            CarSpawnInfoSetup();
        }
        
        #region Set up
        private void InputOutputMapSetup()
        {
            _outputMap.Add(BuildingType.Lung, new List<BuildingType>() { BuildingType.NormalCell, BuildingType.Heart });
            _outputMap.Add(BuildingType.Heart, new List<BuildingType>() { });
            _outputMap.Add(BuildingType.NormalCell, new List<BuildingType>() { BuildingType.Heart });
        }
        
        public void CarSpawnInfoSetup()
        {
            _carSpawnInfos = new Dictionary<BuildingType, CarSpawnInfo>();
            _carSpawnInfos.Add(BuildingType.NormalCell, new CarSpawnInfo()
            {
                Car = ObjectFlags.RedBlood,
                Amount = 5,
                DelayTime = 2f
            });
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
        #endregion
        
        public void RegisterBuilding(BuildingBase building)
        {
            if (_currentBuildings.ContainsKey(building.BuildingType))
            {
                _currentBuildings[building.BuildingType].Add(building);
            }
            else
            {
                _currentBuildings.Add(building.BuildingType, new List<BuildingBase>() { building });
            }
            
            _unconnectedBuildings.Add(building.gameObject, building.ParkingNodes);
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

        /// <summary>
        /// Spawn multiple cars have waiting time between by notifying spawn car system through time
        /// Can not bring this function to the system itself because it makes system ignore other notification when 2, 3 cars spawned
        /// in the same time, job can't work for structural change like instantiate entity
        /// </summary>
        /// <returns></returns>
        public IEnumerator SpawnCarWaves(Node start, Node end, CarSpawnInfo spawnInfo)
        {
            for (int i = 0; i < spawnInfo.Amount; i++)
            {
                Notify((start, end, spawnInfo.Car), NotificationFlags.SpawnCar);
                yield return new WaitForSeconds(spawnInfo.DelayTime);
            }
            yield return null;
        }

        /// <summary>
        /// Notify ECS Spawner System to spawn car find path buildingDirection in it
        /// </summary>
        /// <param name="data"></param>
        /// <param name="flag"></param>
        public void OnNotified(object data, string flag)
        {
            if (flag == NotificationFlags.CheckingConnection &&
                data is (Func<List<Node>, Node, Node>))
            {
                //Check all in unconnected graph:
                Func<List<Node>, Node, Node> givenData = (Func<List<Node>, Node, Node>)data;
                List<GameObject> removedObj = new List<GameObject>();
            
                foreach (GameObject buildingObj in _unconnectedBuildings.Keys)
                {
                    //Get all ouput buildings' parking nodes
                    BuildingBase building = buildingObj.GetComponent<BuildingBase>();
                    List<Node> roadNodes = new List<Node>();
                    foreach (BuildingBase b in GetOutputBuildings(building.BuildingType))
                    {
                       roadNodes.Add(b.RoadNode);
                    }

                    if (roadNodes.Count == 0)
                    {
                        return;
                    }

                    Node startNode = building.RoadNode;
                    Node endNode = givenData(roadNodes, building.RoadNode);
                    
                    if (endNode != null)
                    {
                        CarSpawnInfo carSpawnInfo = _carSpawnInfos[building.BuildingType];
                        StartCoroutine(SpawnCarWaves(startNode, endNode, carSpawnInfo));
                       
                        //Add to remove list to remove later
                        if (_unconnectedBuildings.ContainsKey(endNode.BelongedBuilding))
                        {
                            removedObj.Add(endNode.BelongedBuilding);
                        }
                        removedObj.Add(building.gameObject);
                        
                        //Add to connected:
                        if (!_connectedBuildings.ContainsKey(building.OriginBuildingNode.GraphIndex))
                        {
                           _connectedBuildings.Add(building.OriginBuildingNode.GraphIndex, new List<BuildingBase>());
                        }
                        _connectedBuildings[building.OriginBuildingNode.GraphIndex].Add(building);
                        _connectedBuildings[building.OriginBuildingNode.GraphIndex].Add(endNode.BelongedBuilding.GetComponent<BuildingBase>());
                    }
                }
                //Remove unconnected building
                foreach (GameObject buildingObj in removedObj)
                {
                    _unconnectedBuildings.Remove(buildingObj);
                }
            }
        }
    }

    public struct CarSpawnInfo
    {
        public string Car;
        public float DelayTime; //Wait after previous wave finish
        public int Amount;
    }
}
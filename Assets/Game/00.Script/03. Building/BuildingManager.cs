using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._00._Core_Assembly_Def;
using Game._00.Script._02._System_Manager.Observer;
using Game._00.Script._05_Car_spawner_system.CarSpawner_ECS;
using Game._00.Script._05._Manager;
using Unity.Entities;
using UnityEngine;

namespace Game._00.Script._03._Building
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
        
        private Dictionary<BuildingBase, List<Node>>_unconnectedBuildings = new Dictionary<BuildingBase, List<Node>>();
        
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
            _outputMap.Add(BuildingType.Heart, new List<BuildingType>() { BuildingType.Lung, BuildingType.NormalCell });
            _outputMap.Add(BuildingType.NormalCell, new List<BuildingType>() { BuildingType.Heart });
        }
        
        public void CarSpawnInfoSetup()
        {
            _carSpawnInfos = new Dictionary<BuildingType, CarSpawnInfo>();
            _carSpawnInfos.Add(BuildingType.Lung, new CarSpawnInfo()
            {
                Car = ObjectFlags.RedBlood,
                Amount = 2,
                DelayTime = 1f,
            });
            _carSpawnInfos.Add(BuildingType.Heart, new CarSpawnInfo()
            {
                Car = ObjectFlags.RedBlood,
                Amount = 5,
                DelayTime = 0.5f
            });
            _carSpawnInfos.Add(BuildingType.NormalCell, new CarSpawnInfo()
            {
                Car = ObjectFlags.BlueBlood,
                Amount = 2,
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
            
            _unconnectedBuildings.Add(building, building.ParkingNodes);
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
                List<BuildingBase> removedNodes = new List<BuildingBase>();
            
                foreach (BuildingBase building in _unconnectedBuildings.Keys)
                {
                    //Get all ouput buildings' parking nodes
                    List<Node> parkingNodes = new List<Node>();
                    foreach (BuildingBase b in GetOutputBuildings(building.BuildingType))
                    {
                        if (b.parkingLotSize == ParkingLotSize._1x1) //The 1x1 building the car move to the building
                        {
                            parkingNodes.Add(GridManager.NodeFromWorldPosition(b.transform.position));
                        }
                        else
                        {
                            parkingNodes.AddRange(b.ParkingNodes);
                        }
                    }

                    if (parkingNodes.Count == 0)
                    {
                        return;
                    }

                    Node startNode = building.ParkingNodes[0];
                    Node endNode = givenData(parkingNodes, building.ParkingNodes[0]);
                    
                    if (endNode != null)
                    {
                        CarSpawnInfo carSpawnInfo = _carSpawnInfos[building.BuildingType];
                        StartCoroutine(SpawnCarWaves(startNode, endNode, carSpawnInfo));
                       
                        //Add to remove list to remove later
                        if (_unconnectedBuildings.ContainsKey(endNode.BelongedBuilding))
                        {
                            removedNodes.Add(endNode.BelongedBuilding);
                        }
                        removedNodes.Add(building);
                        
                        //Add to connected:
                        if (!_connectedBuildings.ContainsKey(building.OriginBuildingNode.GraphIndex))
                        {
                           _connectedBuildings.Add(building.OriginBuildingNode.GraphIndex, new List<BuildingBase>());
                        }
                        _connectedBuildings[building.OriginBuildingNode.GraphIndex].Add(building);
                        _connectedBuildings[building.OriginBuildingNode.GraphIndex].Add(endNode.BelongedBuilding);
                    }
                }
                //Remove unconnected building
                foreach (BuildingBase building in removedNodes)
                {
                    _unconnectedBuildings.Remove(building);
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
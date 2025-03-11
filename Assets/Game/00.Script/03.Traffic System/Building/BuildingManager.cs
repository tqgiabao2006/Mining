using System.Collections.Generic;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS;
using Game._00.Script._03.Traffic_System.PathFinding;
using Game._00.Script._03.Traffic_System.Road;
using Unity.Entities;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.Building
{

    public class BuildingManager: SubjectBase, IObserver
    {
        //Directed graph => adjacent list => building type + its output
        private Dictionary<BuildingType, List<BuildingType>> _inputMap = new Dictionary<BuildingType, List<BuildingType>>();
        private Dictionary<BuildingType, List<Home>> _currentHomes;
        private Dictionary<BuildingType, List<Business>> _currentBusiness;


        private List<Business> _unconnectedBusinesses;
        private List<Home> _unconnectedHomes;
        private Dictionary<int, List<BuildingBase>> _connectedBuildings;
        
        private RoadManager _roadManager; 
        private PathRequestManager _pathRequestManager;
        
        private void Start()
        {
            InputOutputMapSetup();
            ObserversSetup();

            _roadManager = FindObjectOfType<RoadManager>();
            _pathRequestManager = PathRequestManager.Instance;
            
            _currentHomes = new Dictionary<BuildingType, List<Home>>();
            _currentBusiness = new Dictionary<BuildingType, List<Business>>();
            _unconnectedBusinesses = new List<Business>();
            _unconnectedHomes = new List<Home>();
            _connectedBuildings = new Dictionary<int , List<BuildingBase>>();
        }
        
        #region Set up
        private void InputOutputMapSetup()
        {
            //Business has no output
            _inputMap.Add(BuildingType.BusinessRed, new List<BuildingType>() { BuildingType.HomeRed });
            _inputMap.Add(BuildingType.BusinessYellow, new List<BuildingType>() { BuildingType.HomeYellow });
            _inputMap.Add(BuildingType.BusinessBlue, new List<BuildingType>() { BuildingType.HomeBlue });
            
        }
        
      
        public override void ObserversSetup()
        {
            // Get the CarSpawnSystem
            IObserver spawnSystemInstance = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CarSpawnSystem>();
            if (spawnSystemInstance != null)
            {
                _observers.Add(spawnSystemInstance);
            }
            IObserver carRequestSystem =World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CarRequest_System>();
            _observers.Add(carRequestSystem);
        }
        #endregion
        
        public void RegisterBuilding(BuildingBase building)
        {
            if (building is Home)
            {
               _unconnectedHomes.Add((Home)building); 
                if (_currentHomes.ContainsKey(building.BuildingType))
                {
                    _currentHomes[building.BuildingType].Add((Home)building);
                }
                else
                {
                    _currentHomes.Add(building.BuildingType, new List<Home>() { (Home)building });
                }
            }else if (building is Business)
            {
               _unconnectedBusinesses.Add((Business)building); 
                if (_currentBusiness.ContainsKey(building.BuildingType))
                {
                   _currentBusiness[building.BuildingType].Add((Business)building);
                }
                else
                {
                    _currentBusiness.Add(building.BuildingType, new List<Business>() { (Business)building });
                } 
            }
        }

        public List<Home> GetInputBuildings(BuildingType buildingType)
        {
            if (_inputMap.ContainsKey(buildingType))
            {
                return _currentHomes[buildingType];
            }
            return new List<Home>();
        }

        public bool IsOutput(BuildingType business, BuildingType home)
        {
            if (_inputMap.TryGetValue(home, out var buildings))
            {
                if (buildings.Contains(business))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Spawn multiple cars have waiting time between by notifying spawn car system through time
        /// Can not bring this function to the system itself because it makes system ignore other notification when 2, 3 cars spawned
        /// in the same time, job can't work for structural change like instantiate entity
        /// </summary>
        /// <returns></returns>
        public void SpawnCarWaves(Home home, Vector3 startNodePosition, Quaternion rotation, string objectFlag)
        {
            
           Notify(new SpawnCarRequest()
           {
               Home = home,
               StartNodePosition = startNodePosition,
               Rotation = rotation,
               ObjectFlag = objectFlag
           }, NotificationFlags.SpawnCar); 
        }

        
        /// <summary>
        /// Create waypoints, notify the car request system to create new blob array waypoints, change car to follow path state
        /// </summary>
        /// <param name="home"></param>
        /// <param name="business"></param>
        public void DemandCars(Entity carEntity,Home home, Business business)
        {
            Vector3[] waypoints = _pathRequestManager.GetPathWaypoints(home.RoadNode.WorldPosition, business.RoadNode.WorldPosition);
            Notify(new DemandCarRequest()
            {
                CarEntity = carEntity,
                Waypoints = waypoints,
            }, NotificationFlags.DemandCar);
        }

        /// <summary>
        /// Notify ECS Spawner System to spawn car find path buildingDirection in it
        /// </summary>
        /// <param name="data"></param>
        /// <param name="flag"></param>
        public void OnNotified(object data, string flag)
        {

            if (flag == NotificationFlags.CheckingConnection)
            {
                int i = _unconnectedHomes.Count - 1; 
                while (i >= 0)
                {
                    bool found = false;
                    int j = _unconnectedBusinesses.Count - 1; 
                    while (j >= 0)
                    {
                        if (_inputMap[_unconnectedBusinesses[j].BuildingType].Contains(_unconnectedHomes[i].BuildingType))
                        {
                            if (_unconnectedHomes[i].RoadNode.GraphIndex == _unconnectedBusinesses[j].RoadNode.GraphIndex &&
                                _unconnectedHomes[i].RoadNode.GraphIndex != -1)
                            {
                                found = true;
                                _unconnectedBusinesses[j].IsConnected = true;
                                _unconnectedHomes[i].IsConnected = true;

                                if (_connectedBuildings.ContainsKey(_unconnectedBusinesses[j].RoadNode.GraphIndex))
                                {
                                    _connectedBuildings[_unconnectedHomes[i].RoadNode.GraphIndex].Add(_unconnectedHomes[i]);
                                }
                                else
                                {
                                    _connectedBuildings.Add(_unconnectedBusinesses[j].RoadNode.GraphIndex, new List<BuildingBase>());
                                }
                                _connectedBuildings[_unconnectedHomes[i].RoadNode.GraphIndex].Add(_unconnectedBusinesses[j]);
                                
                                _unconnectedBusinesses[j].AddHome(_unconnectedHomes[i]);
                                
                                _unconnectedBusinesses.RemoveAt(j);
                                _unconnectedHomes.RemoveAt(i);
                                break; 
                            }
                        }
                        j--; 
                    }

                    if (!found)
                    {
                        i--; 
                    }
                }

                Debug.Log("Unconnected: " + _unconnectedBusinesses.Count + " Home " + _unconnectedHomes.Count);
                Debug.Log("Connected: " + _connectedBuildings.Count);
            }
        }
    }

}

public struct DemandCarRequest
{
    public Entity CarEntity;
    public Vector3[] Waypoints;
}

public struct SpawnCarRequest
{
    public Home Home;
    public Vector3 StartNodePosition;
    public Quaternion Rotation;
    public string ObjectFlag;
}
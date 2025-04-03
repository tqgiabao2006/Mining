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
        
        private Dictionary<BuildingColor, List<Home>> _currentHomes;
        
        private Dictionary<BuildingColor, List<Business>> _currentBusiness;

        private Dictionary<BuildingColor, int> _currentCars;

        private Dictionary<BuildingColor, int> _currentDemands;
        
        private List<Business> _unconnectedBusinesses;
        
        private List<Home> _unconnectedHomes;
        
        private Dictionary<int, List<BuildingBase>> _connectedBuildings;

        private PathRequestManager _pathRequestManager;

        public int HomeCount
        {
            get { return _currentHomes.Count; }
        }

        public int BusinessCount
        {
            get { return _currentBusiness.Count; }
        }

        public int TotalCount
        {
            get { return _currentHomes.Count + _currentBusiness.Count; }
        }
        private void Start()
        {
            ObserversSetup();

            _pathRequestManager = PathRequestManager.Instance;

            _currentHomes = new Dictionary<BuildingColor, List<Home>>();
            
            _currentBusiness = new Dictionary<BuildingColor, List<Business>>();
            
            _currentCars = new Dictionary<BuildingColor, int>();
            
            _currentDemands = new Dictionary<BuildingColor, int>();
            
            _unconnectedBusinesses = new List<Business>();
            
            _unconnectedHomes = new List<Home>();
            
            _connectedBuildings = new Dictionary<int , List<BuildingBase>>();
        }
        #region Set up
        
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
                Home home = (Home)building;
               _unconnectedHomes.Add(home); 
                if (_currentHomes.ContainsKey(building.BuildingColor))
                {
                    _currentHomes[building.BuildingColor].Add(home);
                    _currentCars[building.BuildingColor] += home.NumbCars;
                }
                else
                {
                    _currentHomes.Add(building.BuildingColor, new List<Home>() { home });
                    _currentCars.Add(building.BuildingColor, home.NumbCars);
                }
            }else if (building is Business)
            {
                Business business = (Business)building;
               _unconnectedBusinesses.Add(business); 
                if (_currentBusiness.ContainsKey(building.BuildingColor))
                {
                   _currentBusiness[building.BuildingColor].Add(business);
                   _currentDemands[building.BuildingColor] += business.Demands;
                }
                else
                {
                    _currentBusiness.Add(building.BuildingColor, new List<Business>() {business});
                    _currentDemands.Add(building.BuildingColor, business.Demands);
                } 
            }
        }

        public List<Home> GetInputBuildings(BuildingColor color)
        {
            if (_currentHomes.ContainsKey(color))
            {
                return _currentHomes[color];
            }
            return new List<Home>();
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
           }, NotificationFlags.SPAWN_CAR); 
        }

        public int GetCarNumb(BuildingColor color)
        {
            return _currentCars.ContainsKey(color) ? _currentCars[color] : 0;
        }

        public int GetDemand(BuildingColor color)
        {
            return _currentDemands.ContainsKey(color) ? _currentDemands[color] : 0;
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
            }, NotificationFlags.DEMAND_CAR);
        }

        /// <summary>
        /// Notify ECS Spawner System to spawn car find path buildingDirection in it
        /// </summary>
        /// <param name="data"></param>
        /// <param name="flag"></param>
        public void OnNotified(object data, string flag)
        {

            if (flag == NotificationFlags.CHECK_CONNECTION)
            {
                int i = _unconnectedHomes.Count - 1; 
                while (i >= 0)
                {
                    bool found = false;
                    int j = _unconnectedBusinesses.Count - 1; 
                    while (j >= 0)
                    {
                        if (_currentHomes[_unconnectedBusinesses[j].BuildingColor].Count > 0)
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
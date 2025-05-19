using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS;
using Game._00.Script._03.Traffic_System.PathFinding;
using Game._00.Script._03.Traffic_System.Road;
using Unity.Entities;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.Building
{
    public enum BuildingType
    {
        Home,
        Business
    }

    public enum BuildingColor
    {
        Red,
        Blue
    }

    /// <summary>
    /// Note: keep the order exactly this because JSON databased on this to convert
    /// </summary>
    public enum BuildingDirection
    {
        Left,
        Right,
        Up,
        Down,
    }

    public enum ParkingLotSize
    {
        _1x1,
        _2x2,
        _2x3
    }

    public class BuildingManager: SubjectBase, IObserver
    {
        //Directed graph => adjacent list => building type + its output

        [Tooltip("Show current demand, current cars")]
        [Header("Debug")]
        [SerializeField] private bool isGizmos;
        
        private Dictionary<BuildingColor, List<Home>> _currentHomes;
        
        private Dictionary<BuildingColor, List<Business>> _currentBusiness;
        
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
            IObserver carRequestSystem =World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<DemandCarRequestSystem>();
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
                }
                else
                {
                    _currentHomes.Add(building.BuildingColor, new List<Home>() { home });
                }
            }else if (building is Business)
            {
                Business business = (Business)building;
               _unconnectedBusinesses.Add(business); 
                if (_currentBusiness.ContainsKey(building.BuildingColor))
                {
                   _currentBusiness[building.BuildingColor].Add(business);
                }
                else
                {
                    _currentBusiness.Add(building.BuildingColor, new List<Business>() {business});
                } 
            }
        }

        public void IncreaseDemand(int increase = 1)
        {
            int lowest = int.MaxValue;
            Business business = null;
            
            foreach (List<Business> l in _currentBusiness.Values)
            {
                foreach (Business b in l)
                {
                    if (b.Demands < lowest)
                    {
                        lowest = b.Demands;
                        business = b;
                    }
                }
            }

            if (business != null)
            {
                business.Increase();
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
        /// Get min distance to surrounding buildings
        /// </summary>
        /// <param name="candidate">node position of candidate</param>
        /// <param name="color">color of candidate</param>
        /// <returns></returns>
        public float DstToClosestBuilding(Vector2 candidate, BuildingColor color)
        {
            //Not set to float.MaxValue because the edge case no home spawned match the color => avoid max value + some float value
            float minDst = 10000;

            if (!_currentBusiness.ContainsKey(color))
            {
                return 10000;
            }
            
            foreach (Business business in _currentBusiness[color])
            {
                float dst = Vector2.SqrMagnitude(candidate - business.WorldPosition);

                if (dst < minDst)
                {
                    minDst = dst;
                }
            }
            
            return minDst;
        }

        public float DstToClosestHome(Vector2 candidate, BuildingColor color)
        {
            //Not set to float.MaxValue because the edge case no home spawned match the color => avoid max value + some float value
            float minDst = 10000;
            
            if (!_currentHomes.ContainsKey(color))
            {
                return 10000;
            }
            foreach (Home home in _currentHomes[color])
            {
                float dst = Vector2.SqrMagnitude(candidate - home.WorldPosition);

                if (dst < minDst)
                {
                    minDst = dst;
                }
            }
            
            return minDst;
        }

        /// <summary>
        /// Get number of home surrounding a given candidate
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public int GetHomeDensity(Vector2 candidate, int radius)
        {
            HashSet<Home> occured = new HashSet<Home>();
            int cnt = 0;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }
                    
                    Vector2 pos = candidate + Vector2.right * x * GridManager.NodeDiameter + Vector2.up *  y * GridManager.NodeDiameter;

                    GameObject building = GridManager.NodeFromWorldPosition(pos).BelongedBuilding;

                    if (building !=null)
                    {
                        Home home = building.GetComponent<Home>();
                        if (home != null && !occured.Contains(home))
                        {
                            cnt++;
                            occured.Add(home);
                        }
                    }
                }
            }
            return cnt;
        }
        
        
        /// <summary>
        /// Get number of business surrounding a given candidate
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public int GetBusinessDensity(Vector2 candidate, int radius)
        {
            HashSet<Business> occured = new HashSet<Business>();
            int cnt = 0;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }
                    
                    Vector2 pos = candidate + Vector2.right * x * GridManager.NodeDiameter + Vector2.up *  y * GridManager.NodeDiameter;

                    GameObject building = GridManager.NodeFromWorldPosition(pos).BelongedBuilding;

                    if (building != null)
                    {
                        Business business = building.GetComponent<Business>();
                        if (business != null && !occured.Contains(business))
                        {
                            occured.Add(business);
                            cnt++;
                        }
                    }
                }
            }
            
            return cnt;
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
            if (!_currentHomes.ContainsKey(color))
            {
                return 0;
            }
            return _currentHomes[color].Count * 2;
        }

        public int GetDemand(BuildingColor color)
        {
            if (!_currentBusiness.ContainsKey(color))
            {
                return 0;
            }

            int cnt = 0;
            foreach (Business b in _currentBusiness[color])
            {
                cnt += b.Demands;
            }
            return cnt;
        }
        
        /// <summary>
        /// Create waypoints, notify the car request system to create new blob array waypoints, change car to follow path state
        /// </summary>
        /// <param name="home"></param>
        /// <param name="business"></param>
        public void DemandCars(Entity carEntity,Home home, Business business)
        {
            Vector3[] waypoints = _pathRequestManager.GetPathWaypoints(home.RoadNode.WorldPosition, business.RoadNode.WorldPosition);
            Debug.Log("Demand car");
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
                            if (_unconnectedHomes[i].RoadNode.GraphIndex == _unconnectedBusinesses[j].RoadNode.GraphIndex 
                                && _unconnectedHomes[i].RoadNode.GraphIndex != -1 
                                && _unconnectedBusinesses[j].BuildingColor ==  _unconnectedHomes[i].BuildingColor)
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
                    else
                    {
                        break;
                    }
                }
            }
        }


        private void OnGUI()
        {
            if (!isGizmos)
            {
                return;
            }
            
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;

            Vector2 topLeft = new Vector2(10, 30);
            int i = 0;

            foreach (BuildingColor color in _currentHomes.Keys)
            {
                GUI.Label(new Rect(10, 10 + topLeft.y *i, 200, 200), 
                    $"Car {color}: {GetCarNumb(color)}", style);
                i++;
            }

            foreach (BuildingColor color in _currentBusiness.Keys)
            {
                GUI.Label(new Rect(10, 10 + topLeft.y *i, 200, 200), 
                    $"Demand {color}: {GetDemand(color)}", style);
                i++;
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
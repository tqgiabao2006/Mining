using Game._00.Script._02._System_Manager;
using Game._00.Script.NewPathFinding;
using UnityEngine;

namespace Game._00.Script._05._Manager
{
    public class GameManager : Singleton<GameManager>
    {
        public GameStateManager GameStateManager { get; private set; }
        public ObjectPooling ObjectPooling { get; private set; }
        public RoadManager RoadManager { get; private set; }
        public BuildingManager BuildingManager { get; private set; }
        public GridManager GridManager { get; private set; }
        
        public PathFinding  PathFinding  { get; private set; }
        public PathRequestManager PathRequestManager { get; private set; }
        
        private void Awake()
        {
            // Initialize components
            GridManager = GetComponentInChildren<GridManager>();
            
            GameStateManager = GetComponent<GameStateManager>();
            ObjectPooling = GetComponentInChildren<ObjectPooling>();
            RoadManager = GetComponentInChildren<RoadManager>();
            BuildingManager = GetComponentInChildren<BuildingManager>();
            
            PathFinding = GetComponentInChildren<PathFinding>();
            PathFinding.Initialize();
            PathRequestManager = GetComponentInChildren<PathRequestManager>();
            PathRequestManager.Initialize();
            
            //Initialize all references after
            GameStateManager.Initialize();

        }
    }
}

using Game._00.Script.NewPathFinding;
using UnityEngine;

namespace Game._00.Script._05._Manager
{
    [RequireComponent(typeof(ObjectPooling), typeof(RoadManager), typeof(TestRequestManager))]
    public class GameManager : Singleton<GameManager>
    {
        public GameStateManager GameStateManager { get; private set; }
        public ObjectPooling ObjectPooling { get; private set; }
        public RoadManager RoadManager { get; private set; }
        public BuildingManager BuildingManager { get; private set; }
        public GridManager GridManager { get; private set; }
        
        public NewPathFinding.PathFinding  PathFinding  { get; private set; }
        public PathRequestManager PathRequestManager { get; private set; }
        

        private void Awake()
        {
            // Initialize components
            GridManager = FindObjectOfType<GridManager>();
            GameStateManager = GetComponent<GameStateManager>();
            ObjectPooling = GetComponent<ObjectPooling>();
            RoadManager = GetComponent<RoadManager>();
            BuildingManager = GetComponent<BuildingManager>();
            
            PathFinding = GetComponent<NewPathFinding.PathFinding>();
            PathFinding.Initialize();
            PathRequestManager = GetComponent<PathRequestManager>();
            PathRequestManager.Initialize();
            
            //Initialize all references after
            GameStateManager.Initialize();

        }
    }
}

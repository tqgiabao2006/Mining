using Game._00.Script.NewPathFinding;
using UnityEngine;

namespace Game._00.Script._05._Manager
{
    [RequireComponent(typeof(ObjectPooling), typeof(RoadManager), typeof(PathRequestManager))]
    public class GameManager : Singleton<GameManager>
    {
        public GameStateManager GameStateManager { get; private set; }
        public PathFinding PathFinding { get; private set; }
        public ObjectPooling ObjectPooling { get; private set; }
        public RoadManager RoadManager { get; private set; }
        public PathRequestManager PathRequestManager { get; private set; }
        public BuildingManager BuildingManager { get; private set; }
        public Grid Grid { get; private set; }
        
        public NewPathFinding.NewPathFinding  NewPathFinding  { get; private set; }
        public NewPathRequestManager NewPathRequestManager { get; private set; }
        


        

        private void Awake()
        {
            // Initialize components
            Grid = FindObjectOfType<Grid>();
            GameStateManager = GetComponent<GameStateManager>();
            ObjectPooling = GetComponent<ObjectPooling>();
            RoadManager = GetComponent<RoadManager>();
            
            // PathRequestManager = GetComponent<PathRequestManager>();
            // PathFinding = FindObjectOfType<PathFinding>();
            
            BuildingManager = GetComponent<BuildingManager>();
            
            //Test:
            NewPathFinding = GetComponent<NewPathFinding.NewPathFinding>();
            NewPathFinding.Initialize();
            NewPathRequestManager = GetComponent<NewPathRequestManager>();
            NewPathRequestManager.Initialize();
            
            //Initialize all references after
            GameStateManager.Initialize();

        }
    }
}

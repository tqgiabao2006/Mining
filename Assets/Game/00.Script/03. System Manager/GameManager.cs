using UnityEngine;

namespace Game._00.Script._05._Manager
{
    [RequireComponent(typeof(GameStateManager), typeof(ObjectPooling))]
    public class GameManager : Singleton<GameManager>
    {
        private GameStateManager gameStateManager;
        private ObjectPooling objectPooling;
        private RoadManager roadManager;
        private PathRequestManager pathRequestManager;
        private PathFinding pathFinding;

        // Properties with getters and setters
        public GameStateManager GameStateManager
        {
            get => gameStateManager;
            private set => gameStateManager = value;
        }

        public PathFinding PathFinding
        {
            get => pathFinding;
            private set => pathFinding = value;
        }

        public ObjectPooling ObjectPooling
        {
            get => objectPooling;
            private set => objectPooling = value;
        }

        public RoadManager RoadManager
        {
            get => roadManager;
            private set => roadManager = value;
        }

        public PathRequestManager PathRequestManager
        {
            get => pathRequestManager;
            private set => pathRequestManager = value;
        }

        private void Awake()
        {
            // Initialize components
            GameStateManager = GetComponent<GameStateManager>();
            ObjectPooling = GetComponent<ObjectPooling>();
            RoadManager = GetComponent<RoadManager>();
            PathRequestManager = GetComponent<PathRequestManager>();
            PathFinding = FindObjectOfType<PathFinding>();
        }
    }
}

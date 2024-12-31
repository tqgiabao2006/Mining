namespace Game._00.Script._00.Manager
{
    public class GameManager : Singleton<GameManager>
    {
            public GameStateManager GameStateManager { get; private set; }
            public ObjectPooling ObjectPooling { get; private set; }
           
            private void Awake()
            {
                GameStateManager = GetComponent<GameStateManager>();
                ObjectPooling = GetComponentInChildren<ObjectPooling>();
                GameStateManager.Initialize();

            }
    }
}

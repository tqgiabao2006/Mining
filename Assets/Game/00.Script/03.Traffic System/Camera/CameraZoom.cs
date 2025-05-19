using Game._00.Script._00.Manager;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._04.Timer;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game._00.Script.Camera
{
    public struct Zone
    {
        public Vector2 BotLeftPivot; //Use for disc posion mainly
        public Vector2 Size;
    }
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraZoom : Singleton<CameraZoom>
    { 
        [SerializeField] private bool enabledZoom;
        
        [SerializeField] private bool drawInteractableZone;

        [SerializeField] private int maxSize = 14;
        
        [SerializeField] private float zoomSpeed; // 0.05 for 30 min levels
        
        [Tooltip("The ratio: interactable/whole screne")]
        [Range(0,1)]
        [SerializeField]  
        private float zoneRatio;
        
        private UnityEngine.Camera _camera;
        
        private Timer _timer;

        public bool EnabledZoom
        {
            set
            {
                enabledZoom = value;
            }
        }
        public Zone InteractZone
        {
            get;
            private set;
        }

        public Zone SpawnZone
        {
            get;
            private set;
        }
        private void Start()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            
            _timer = GetComponent<Timer>();
            //Set Initial first
            UpdateBound();
        }

        private void Update()
        {
            if (enabledZoom)
            {
                Zoom();
            }
            UpdateBound();
        }

        private void Zoom()
        {
            this._camera.orthographicSize = Mathf.Min( _camera.orthographicSize + zoomSpeed *_timer.TimeScale* Time.deltaTime, maxSize );
        }
        private void UpdateBound()
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            float sizeX = zoneRatio * halfWidth * 2;
            float sizeY = zoneRatio * halfHeight * 2;

            //Make sure at least 1 node lesser than size
            float spawnX = Mathf.Min(Mathf.CeilToInt(sizeX *3/4f), sizeX - 2);
            float spawnY = Mathf.Min(Mathf.CeilToInt(sizeY *3/4f), sizeY - 2);
            
            // Round to the nearest multiple of NodeDiameter
            sizeX = Mathf.RoundToInt(sizeX / GridManager.NodeDiameter) * GridManager.NodeDiameter;
            sizeY = Mathf.RoundToInt(sizeY / GridManager.NodeDiameter) * GridManager.NodeDiameter;

            spawnX = Mathf.RoundToInt(spawnX / GridManager.NodeDiameter) * GridManager.NodeDiameter;
            spawnY = Mathf.RoundToInt(spawnY/GridManager.NodeDiameter) * GridManager.NodeDiameter;
            
            //Round to even number
            sizeX += sizeX % 2;
            sizeY += sizeY % 2;
            
            spawnX += spawnX % 2;
            spawnY += spawnY % 2;

            InteractZone = new Zone()
            {
                BotLeftPivot = new Vector2(-sizeX/2, -sizeY/2),
                Size = new Vector2(sizeX, sizeY),
            };

            SpawnZone = new Zone()
            {
                BotLeftPivot = new Vector2(-spawnX/2, -spawnY/2),
                Size = new Vector2(spawnX, spawnY),
            };
        }


        private void OnDrawGizmos()
        {
            if (!drawInteractableZone)
            {
                return;
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(InteractZone.BotLeftPivot, 0.5f);
            Gizmos.DrawWireCube(this.transform.position, this.InteractZone.Size);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(this.transform.position, this.SpawnZone.Size);
            Gizmos.DrawWireSphere(SpawnZone.BotLeftPivot, 0.5f);
        }
    }

}
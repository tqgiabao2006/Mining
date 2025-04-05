using Game._00.Script._00.Manager;
using Game._00.Script._02.Grid_setting;
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

        public Zone Zone
        {
            get;
            private set;
        }
        private void Start()
        {
            _camera = GetComponent<UnityEngine.Camera>();
        }

        private void Update()
        {
            if (!enabledZoom)
            {
                return;
            }
            Zoom();
            UpdateBound();
        }

        private void Zoom()
        {
            this._camera.orthographicSize = Mathf.Min( _camera.orthographicSize + zoomSpeed * Time.deltaTime, maxSize );
        }
        private void UpdateBound()
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            float sizeX = zoneRatio * halfWidth * 2;
            float sizeY = zoneRatio * halfHeight * 2;

            // Round to the nearest multiple of NodeDiameter
            sizeX = Mathf.RoundToInt(sizeX / GridManager.NodeDiameter) * GridManager.NodeDiameter;
            sizeY = Mathf.RoundToInt(sizeY / GridManager.NodeDiameter) * GridManager.NodeDiameter;
            
            //Round to even number
            sizeX += sizeX % 2;
            sizeY += sizeY % 2;

            Zone = new Zone()
            {
                BotLeftPivot = new Vector2(-sizeX/2, sizeY/2),
                Size = new Vector2(sizeX, sizeY),
            };
        }


        private void OnDrawGizmos()
        {
            if (!drawInteractableZone)
            {
                return;
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(Zone.BotLeftPivot, 0.5f);
            Gizmos.DrawWireCube(this.transform.position, this.Zone.Size);
        }
    }

}
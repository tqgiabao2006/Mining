using Game._00.Script._00.Manager;
using Game._00.Script._02.Grid_setting;
using UnityEngine;

namespace Game._00.Script.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraZoom : Singleton<CameraZoom>
    {
        [SerializeField] private bool drawInteractableZone;

        [SerializeField] private int maxSize = 14;
        
        [SerializeField] private float zoomSpeed; // 0.05 for 30 min levels
        
        [Tooltip("The ratio: interactable/whole screne")]
        [Range(0,1)]
        [SerializeField]  
        private float zoneRatio;
        
        private UnityEngine.Camera _camera;
        
        public Vector2 Bound
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
            
            Bound = new Vector2(sizeX, sizeY);
        }


        private void OnDrawGizmos()
        {
            if (!drawInteractableZone)
            {
                return;
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(this.transform.position, 0.5f);
            Gizmos.DrawWireCube(this.transform.position, this.Bound);
        }
    }

}
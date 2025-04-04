using UnityEngine;
using Camera = UnityEngine.Camera;
namespace Game._00.Script.Demos
{
    public class MouseLine : MonoBehaviour
    {
        private Vector2 _startPosition;
        private Vector2 _lastMousePosition = Vector2.zero;
        private LineRenderer _lineRenderer;

        private int count = 0;

        void Start()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 0; // Initialize with zero points
        }

        void Update()
        {
            if(!Input.GetMouseButton(0)) return;
            if(Input.GetMouseButtonUp(0)) _lineRenderer.positionCount = 0;
            _startPosition = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition);
            this.gameObject.transform.position = _startPosition;

            if (Vector2.Distance(_startPosition, _lastMousePosition) > 0.5f)
            {
                count++;

                // Increase the position count of the LineRenderer
                _lineRenderer.positionCount = count;

                // Add the new position
                _lineRenderer.SetPosition(count - 1, _startPosition);

                // Update the last mouse position
                _lastMousePosition = _startPosition;
            }
        }
    }
}
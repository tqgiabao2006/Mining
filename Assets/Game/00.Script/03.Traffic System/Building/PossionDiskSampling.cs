using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using URandom = UnityEngine.Random;

namespace Game._00.Script._03.Traffic_System.Building
{
    public class PossionDiskSampling : MonoBehaviour
    {
        [SerializeField] private float _gizmosRadius;
        [SerializeField] private Vector2 _zoneSize;
        [SerializeField] private float _radius;
        [SerializeField] private int _attempts;
        private List<Vector2> _points;
        
        private void Start()
        {
            _points = Spawn( _zoneSize, _radius,_attempts);
        }

        public List<Vector2> Spawn(Vector2 zoneSize, float radius, int maxAttempt)
        {
            float cellSize = radius / Mathf.Sqrt(2);
            Vector2 worldOrigin = (Vector2)transform.position;

            int[,] grid = new int[Mathf.CeilToInt(zoneSize.x / cellSize), Mathf.CeilToInt(zoneSize.y / cellSize)];
            List<Vector2> points = new List<Vector2>();
            List<Vector2> spawnPoints = new List<Vector2>();

            spawnPoints.Add(worldOrigin + zoneSize / 2);

            while (spawnPoints.Count > 0)
            {
                int pointIndex = URandom.Range(0, spawnPoints.Count);
                Vector2 point = spawnPoints[pointIndex];

                bool isAccepted = false;
                for (int i = 0; i < maxAttempt; i++)
                {
                    float angle = URandom.value * Mathf.PI * 2;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 candidate = point + direction * URandom.Range(radius, 2 * radius);

                    if (IsValid(candidate, cellSize, grid, worldOrigin, zoneSize, points, radius))
                    {
                        points.Add(candidate);
                        spawnPoints.Add(candidate);
                        grid[(int)((candidate.x - worldOrigin.x) / cellSize), (int)((candidate.y - worldOrigin.y) / cellSize)] = points.Count;
                        isAccepted = true;
                        break;
                    }
                }

                if (!isAccepted)
                {
                    spawnPoints.RemoveAt(pointIndex);
                }
            }

            return points;
        }


        private bool IsValid(Vector2 candidate, float cellSize, int[,] grid, Vector2 worldOrigin, Vector2 zoneSize, List<Vector2> points, float radius)
        {
            if (candidate.x < worldOrigin.x || candidate.x > worldOrigin.x + zoneSize.x ||
                candidate.y < worldOrigin.y || candidate.y > worldOrigin.y + zoneSize.y)
            {
                return false;
            }

            int x = (int)((candidate.x - worldOrigin.x) / cellSize);
            int y = (int)((candidate.y - worldOrigin.y) / cellSize);

            int startX = Mathf.Max(0, x - 2);
            int endX = Mathf.Min(x + 2, grid.GetLength(0) - 1);
            int startY = Mathf.Max(0, y - 2);
            int endY = Mathf.Min(y + 2, grid.GetLength(1) - 1);

            for (int i = startX; i <= endX; i++)
            {
                for (int j = startY; j <= endY; j++)
                {
                    int pointIndex = grid[i, j] - 1;
                    if (pointIndex >= 0)
                    {
                        float dstSqrt = (candidate - points[pointIndex]).sqrMagnitude;
                        if (dstSqrt < radius * radius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }


        private void OnDrawGizmos()
        {
            if (_points == null)
            {
                return;
            }
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(
                new Vector3(this.transform.position.x + _zoneSize.x / 2f, this.transform.position.y + _zoneSize.y / 2f, 0),
                new Vector3(_zoneSize.x, _zoneSize.y, 0)
            );
            
            Gizmos.color = Color.red;
            for (int i = 0; i < _points.Count; i++)
            {
                Handles.Label(_points[i], i.ToString());
            }
        }
    }
}

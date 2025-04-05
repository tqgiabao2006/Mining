using System;
using System.Collections.Generic;
using System.Linq;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.MapData;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using URandom = UnityEngine.Random;

namespace Game._00.Script._03.Traffic_System.Building
{
    public class PossionDisc
    { 
        private MapSupplyDemand _mapSupplyDemand;
        
        private Vector2 _zoneSize; 
        
        private int _attempts;
        
        private float _radius;

        private Vector2 _worldPivot;

        private Dictionary<ParkingLotSize, List<Vector2>> _sizeMap; //Map by size

        public List<Vector2> this[ParkingLotSize size]
        {
            get
            {
                if (_sizeMap.ContainsKey(size) && _sizeMap != null)
                {
                    return _sizeMap[size];
                }
                
                DebugUtility.LogError("Key not found in Heat Map", "PosionDisc");
                return new List<Vector2>();
            }
        }

        public PossionDisc(Vector2 worldPivot, Vector2 zoneSize)   
        {
            _attempts = 30;
            
            _sizeMap = new Dictionary<ParkingLotSize, List<Vector2>>();
            
            _worldPivot = worldPivot;
            
            _zoneSize = zoneSize;
            
            _radius = GridManager.NodeRadius;
            
            ParkingLotSize[] size = (ParkingLotSize[])Enum.GetValues(typeof(ParkingLotSize));
            
            for (int i = 0; i < size.Length; i++)
            {
                float scaledRadius = GetScaleRadius(size[i]);
                List<Vector2> points = Spawn(_zoneSize, _worldPivot, scaledRadius, _attempts);
                
                if (_sizeMap.ContainsKey(size[i]))
                {
                    _sizeMap[size[i]].AddRange(points);
                }
                else
                {
                    _sizeMap.Add(size[i], points);
                }
            } 
        }
        private float GetScaleRadius(ParkingLotSize size) => size switch
        {
            ParkingLotSize._1x1 => _radius * 1,
            ParkingLotSize._2x2 => _radius * 3,
            ParkingLotSize._2x3 => _radius * 5,
            _ => _radius // Default fallback
        };
        
        private List<Vector2> Spawn(Vector2 zoneSize, Vector2 worldPivot,float scaledRadius, int maxAttempt)
        {
            float cellSize = scaledRadius / Mathf.Sqrt(2); 

            int gridWidth = Mathf.CeilToInt(zoneSize.x / cellSize);
            int gridHeight = Mathf.CeilToInt(zoneSize.y / cellSize);
            int[,] grid = new int[gridWidth, gridHeight];

            List<Vector2> points = new List<Vector2>();
            List<Vector2> spawnPoints = new List<Vector2>();

            spawnPoints.Add(worldPivot + zoneSize / 2);

            while (spawnPoints.Count > 0)
            {
                int pointIndex = URandom.Range(0, spawnPoints.Count);
                Vector2 point = spawnPoints[pointIndex];

                bool isAccepted = false;
                for (int i = 0; i < maxAttempt; i++)
                {
                    float angle = URandom.value * Mathf.PI * 2;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 candidate = GridManager.NodeFromWorldPosition(
                        point + direction * URandom.Range(scaledRadius, 2 * scaledRadius)
                    ).WorldPosition;

                    if (IsValid(candidate, cellSize, grid, worldPivot, zoneSize, points, scaledRadius))
                    {
                        points.Add(candidate);
                        spawnPoints.Add(candidate);
                        int x = Mathf.FloorToInt((candidate.x - worldPivot.x) / cellSize);
                        int y = Mathf.FloorToInt((candidate.y - worldPivot.y) / cellSize);
                        grid[x, y] = points.Count;
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

        private bool IsValid(Vector2 candidate, float cellSize, int[,] grid, 
            Vector2 worldOrigin, Vector2 zoneSize, List<Vector2> points, float scaledRadius)
        {
            if (candidate.x < worldOrigin.x || candidate.x > worldOrigin.x + zoneSize.x ||
                candidate.y < worldOrigin.y || candidate.y > worldOrigin.y + zoneSize.y)
            {
                return false;
            }

            int x = Mathf.FloorToInt((candidate.x - worldOrigin.x) / cellSize);
            int y = Mathf.FloorToInt((candidate.y - worldOrigin.y) / cellSize);

            int neighborSize = Mathf.CeilToInt(scaledRadius / cellSize);
            int startX = Mathf.Max(0, x - neighborSize);
            int endX = Mathf.Min(x + neighborSize, grid.GetLength(0) - 1);
            int startY = Mathf.Max(0, y - neighborSize);
            int endY = Mathf.Min(y + neighborSize, grid.GetLength(1) - 1);

            for (int i = startX; i <= endX; i++)
            {
                for (int j = startY; j <= endY; j++)
                {
                    int pointIndex = grid[i, j] - 1;
                    if (pointIndex >= 0)
                    {
                        float dstSqr = (candidate - points[pointIndex]).sqrMagnitude;
                        if (dstSqr < scaledRadius * scaledRadius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        // private void OnDrawGizmos()
        // {
        //     if (_points == null)
        //     {
        //         return;
        //     }
        //
        //     Gizmos.color = Color.blue;
        //     Gizmos.DrawWireCube(
        //         new Vector3(this.transform.position.x + _zoneSize.x / 2f, this.transform.position.y + _zoneSize.y / 2f, 0),
        //         new Vector3(_zoneSize.x, _zoneSize.y, 0)
        //     );
        //
        //     Gizmos.color = Color.red;
        //     for (int i = 0; i < _points.Count; i++)
        //     {
        //         Handles.Label(_points[i], i.ToString());
        //     }
        // }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using Game._00.Script.Camera;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Game._00.Script._03.Traffic_System.MapData
{
    public class MapSupplyDemand : MonoBehaviour
    {
        [Header("Debug Property")] 
        [SerializeField] private bool isGizmos;

        [SerializeField] private bool drawSupply;

        [SerializeField] private bool drawDemand;

        [SerializeField] private bool drawUnspawnable;

        //Weight of each node in grid layout (2D array)
        private Dictionary<string, float[,]> _layerWeights; //String is layer: Demand (business), Supply (home). and unspawnable,
        
        private Vector2 _size = Vector2.zero;

        public readonly float[] WeightValue = { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f};

        private CameraZoom _cameraZoom;
        
        public Vector2 Size
        {
            get
            {
                return _size;
            }
        }

        public List<Vector2> this[ParkingLotSize size, float weight]
        {
            get
            {
                string layerTag = GetLayerTag(size);
                weight = FloorToNearestStep(weight, 0.2f);
                if (_layerWeights.ContainsKey(layerTag))
                { 
                    return GetMatches(weight, layerTag);
                }
                return new List<Vector2>();
            }
        }
        
        
        /// <summary>
        /// Called before give to posion disc
        /// </summary>
        public void SetUp()
        {
            _cameraZoom = CameraZoom.Instance;
            _layerWeights = new Dictionary<string, float[,]>();
            LoadTileLayers();
        }
        
        /// <summary>
        /// Load tile layers by loop and set create alpha node
        /// </summary>
        private void LoadTileLayers()
        {
            Tilemap[]  tilemaps = GetComponentsInChildren<Tilemap>();
            TilemapRenderer[] renderers = GetComponentsInChildren<TilemapRenderer>();

            if (tilemaps.Length > 0)
            {
                _size =  new Vector2(tilemaps[0].size.x, tilemaps[0].size.y);
            }
            if (renderers.Length != tilemaps.Length)
            {
                DebugUtility.LogError("There is a child that does not contain Tilemap renderer or TileMap componenet", this.gameObject.name);
                return;
            }
            
            for(int i =0; i < tilemaps.Length; i++)
            {
                if (IsValidTag(tilemaps[i].gameObject.tag))
                {
                    LoadAlphaNodeMap(tilemaps[i], tilemaps[i].gameObject.tag);
                    renderers[i].enabled = false;
                }
                else
                {
                    DebugUtility.LogError("Invalid tag for map data", this.gameObject.name);
                }
            }
        }

        /// <summary>
        /// Load alpha node in 1 tile map
        /// </summary>
        /// <param name="tilemap">tilemap component</param>
        /// <param name="validTag">tag of layer</param>
        /// <returns>AlphaNode[,]</returns>
        private void LoadAlphaNodeMap(Tilemap tilemap, string validTag)
        {
            BoundsInt bounds = tilemap.cellBounds;
               
            _layerWeights.Add(validTag, new float[GridManager.GridSizeX, GridManager.GridSizeY]);

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    Vector2 worldPos = tilemap.GetCellCenterWorld(cellPos);
                    Vector2Int index = GetGridIndex(worldPos);
                    if (tilemap.HasTile(cellPos))
                    {
                        float alphaVal = FloorToNearestStep(GetSpriteAlpha(tilemap, cellPos), 0.2f);
                    
                        if (validTag == LayerTag.UNSPAWNABLE)
                        {
                            alphaVal = 1;
                        }
                        Debug.Log(index);
                        _layerWeights[validTag][index.x,index.y] = alphaVal;
                    }
                    else
                    { 
                        _layerWeights[validTag][index.x, index.y] = 0;
                    }

                }
            }
        }

        /// <summary>
        /// Get sprite alpha
        /// </summary>
        /// <param name="tilemap">Chosen layer tilemap</param>
        /// <param name="cellPos">cell pos of cell</param>
        /// <returns></returns>
        private float GetSpriteAlpha(Tilemap tilemap, Vector3Int cellPos)
        {
            TileBase tileBase = tilemap.GetTile(cellPos);
            if (tileBase is Tile tile && tile.sprite != null)
            {
                Texture2D tex = tile.sprite.texture;
                Rect spriteRect = tile.sprite.textureRect;
                
                //Pick pixel in corner because the middle has a typo with alpha 1
                int pixelX = Mathf.FloorToInt(spriteRect.x);
                int pixelY = Mathf.FloorToInt(spriteRect.y );

                Color pixelColor = tex.GetPixel(pixelX, pixelY);
                return pixelColor.a;
            }
    
            return 0f; 
        }

        /// <summary>
        /// Check if valid tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private bool IsValidTag(string tag)
        {
            return tag == LayerTag.DEMAND || tag == LayerTag.SUPPLY || tag == LayerTag.UNSPAWNABLE;
        }

        private float FloorToNearestStep(float value, float step)
        {
            return Mathf.Floor(value / step) * step;
        }
        
        private string GetLayerTag(ParkingLotSize size) => size switch
        {
            ParkingLotSize._1x1 => LayerTag.SUPPLY,
            ParkingLotSize._2x2 => LayerTag.DEMAND,
            ParkingLotSize._2x3 => LayerTag.DEMAND,
            _ => LayerTag.UNSPAWNABLE
        };

        /// <summary>
        /// Get points that matches the weight insid the spawn zone
        /// </summary>
        /// <param name="weight">matched weight</param>
        /// <param name="validLayerTag">verified layer tag</param>
        /// <returns></returns>
        private List<Vector2> GetMatches(float weight, string validLayerTag)
        {
            List<Vector2> matches = new List<Vector2>();
            Vector2 pivot = _cameraZoom.SpawnZone.BotLeftPivot;
            Vector2 size = _cameraZoom.SpawnZone.Size;

            float[,] weights = _layerWeights[validLayerTag];
            
            for (float x = pivot.x + GridManager.NodeRadius; x < pivot.x + size.x - GridManager.NodeDiameter; x += GridManager.NodeDiameter)
            {
                for (float y = pivot.y + GridManager.NodeRadius; y < pivot.y + size.y - GridManager.NodeDiameter; y += GridManager.NodeDiameter)
                {
                    Vector2Int indexes = GetGridIndex(new Vector2(x, y));

                    if (Mathf.Approximately(weights[indexes.x, indexes.y], weight))
                    {
                        matches.Add(new Vector2(x, y));
                    }
                }
            }
            return matches;
        }

        /// <summary>
        /// Convert from world pos to index x,y in the grid
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        private Vector2Int GetGridIndex(Vector2 worldPos)
        {
            int gridSizeX = GridManager.GridSizeX;
            int gridSizeY = GridManager.GridSizeY;
            Vector2 gridWorldSize = GridManager.GridWorldSize;
            
            // Check for the zero vector case
            if (worldPos == Vector2.zero)
            {
                // Return the center node of the _gridManager
                int centerX = gridSizeX / 2;
                int centerY = gridSizeY / 2;
                return new Vector2Int(centerX, centerY);
            }
       
            float percentX = worldPos.x / gridWorldSize.x + 0.5f;
            float percentY = worldPos.y / gridWorldSize.y + 0.5f;
            //if worldPosition = (0,y) percentX = 0, (x, y) = 1, in center = 0.5x
            // worldPoint.x/worldSize.x = the index x-axis of it, + 0.5f is center of it;


            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);
            //Make sure it not outsize the _gridManager


            int x = Mathf.FloorToInt(Mathf.Clamp((gridSizeX) * percentX, 0, gridSizeX - 1));
            //gridSizeX - 1 because in the array system, count from 0, so do it avoid out range of array
            int y = Mathf.FloorToInt(Mathf.Clamp((gridSizeY) * percentY, 0, gridSizeY - 1));
            
            
            return new Vector2Int(x, y);
        }
        private bool IsVectorEqual(Vector2 a, Vector2 b, float tolerance = 0.05f)
        {
            return (a-b).sqrMagnitude <= tolerance;
        }
        

        private void OnDrawGizmos()
        {
            if (!isGizmos || _layerWeights == null)
            {
                return;
            }
            
             Vector2 worldBottomLeft = Vector2.zero - Vector2.right * GridManager.GridWorldSize.x / 2 
                                                    - Vector2.up * GridManager.GridWorldSize.y / 2;
             
            for (int x = 0; x < GridManager.GridSizeX; x++)
            {
                for (int y = 0; y < GridManager.GridSizeY; y++)
                {
                    Vector3 worldPos = worldBottomLeft + Vector2.right * (x * GridManager.NodeDiameter + GridManager.NodeRadius)
                                                         + Vector2.up * (y * GridManager.NodeDiameter + GridManager.NodeRadius);
                    if (drawDemand)
                    {
                        Handles.Label(worldPos, _layerWeights[LayerTag.DEMAND][x,y].ToString());
                    }

                    if (drawSupply)
                    {
                        Handles.Label(worldPos, _layerWeights[LayerTag.SUPPLY][x,y].ToString());
                    }

                    if (drawUnspawnable)
                    {
                        Handles.Label(worldPos, _layerWeights[LayerTag.UNSPAWNABLE][x,y].ToString());
                    }
                    
                    
                }
            }
        }
    }
}

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
        [Header("Debug Property")] [SerializeField]
        private bool isGizmos;

        [SerializeField] private bool drawSupply;

        [SerializeField] private bool drawDemand;

        [SerializeField] private bool drawUnspawnable;

        private Dictionary<(string,float), HashSet<Vector2>> _layerWeight;

        private Vector2 _size = Vector2.zero;

        public readonly float[] WeightValue = { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f};
        
        private PossionDisc _possionDisc;
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
                if (_layerWeight.ContainsKey((layerTag, weight)) && _layerWeight != null && _possionDisc != null)
                { 
                    HashSet<Vector2> weights = _layerWeight[(layerTag, weight)];
                    List<Vector2> randomPos = _possionDisc[size];

                    //Filter out by weight
                    List<Vector2> matches = randomPos.Where(pos => weights.Any(w => IsVectorEqual(w, pos))).ToList();
                    Debug.Log(matches.Count);
                    return matches;
                }
                return new List<Vector2>();
            }
        }
        
        
        /// <summary>
        /// Called before give to posion disc
        /// </summary>
        public void SetUp()
        {
            _layerWeight = new Dictionary<(string, float), HashSet<Vector2>>();
            LoadTileLayers();
            _possionDisc = new PossionDisc(CameraZoom.Instance.Zone.BotLeftPivot, CameraZoom.Instance.Zone.Size);
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
        /// <param name="weight"></param>
        /// <returns>AlphaNode[,]</returns>
        private void LoadAlphaNodeMap(Tilemap tilemap, string validTag)
        {
            int yMin = tilemap.cellBounds.yMin;
            int yMax = tilemap.cellBounds.yMax;
            int xMin = tilemap.cellBounds.xMin;
            int xMax = tilemap.cellBounds.xMax;
            
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    Vector3Int gridPos = new Vector3Int(x,y,0);
                    if (tilemap.HasTile(gridPos))
                    {
                        Vector3 worldPos = tilemap.layoutGrid.CellToWorld(gridPos);
                        Vector2 nodePos = GridManager.NodeFromWorldPosition(worldPos).WorldPosition;
                        float alphaVal = FloorToNearestStep(GetSpriteAlpha(tilemap, gridPos), 0.2f);

                        if (_layerWeight.ContainsKey((validTag, alphaVal)))
                        {
                            _layerWeight[(validTag, alphaVal)].Add(nodePos);
                        }
                        else
                        {
                            _layerWeight.Add((validTag, alphaVal), new HashSet<Vector2>());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get sprite alpha
        /// </summary>
        /// <param name="tilemap">Chosen layer tilemap</param>
        /// <param name="gridPos">grid pos of cell</param>
        /// <returns></returns>
        private float GetSpriteAlpha(Tilemap tilemap, Vector3Int gridPos)
        {
            TileBase tileBase = tilemap.GetTile(gridPos);
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

        private bool IsVectorEqual(Vector2 a, Vector2 b, float tolerance = 0.05f)
        {
            return (a-b).sqrMagnitude <= tolerance;
        }
        
            
        public int GetNodeCount(string layerTag, float weight)
        {
            return layerTag switch
            {
                LayerTag.DEMAND => _layerWeight.ContainsKey((layerTag,weight)) ? _layerWeight[(layerTag,weight)].Count : 0,
                LayerTag.SUPPLY => _layerWeight.ContainsKey((layerTag,weight)) ? _layerWeight[(layerTag,weight)].Count : 0,
                LayerTag.UNSPAWNABLE => _layerWeight.ContainsKey((layerTag,weight)) ? _layerWeight[(layerTag,weight)].Count : 0,
                _ => 0
            };
        }

        private void OnDrawGizmos()
        {
            if (!isGizmos)
            {
                return;
            }

            if (drawDemand) 
            {
                for (int i = 0; i < WeightValue.Length; i++)
                {
                    if (_layerWeight.ContainsKey((LayerTag.DEMAND, WeightValue[i])))
                    {
                        HashSet<Vector2> points = _layerWeight[(LayerTag.DEMAND, WeightValue[i])];
                        foreach(Vector2 point in points)
                        {
                            Handles.Label(point, 
                                String.Format("{0:F1}", WeightValue[i]),
                                new GUIStyle()
                                {
                                    fontSize = 20,
                                    normal = new GUIStyleState()
                                    {
                                        textColor = Color.yellow,
                                    }
                                });
                        }
                    }
                }
            }
            
            if (drawSupply) 
            {
                for (int i = 0; i < WeightValue.Length; i++)
                {
                    if (_layerWeight.ContainsKey((LayerTag.SUPPLY, WeightValue[i])))
                    {
                        HashSet<Vector2> points = _layerWeight[(LayerTag.SUPPLY, WeightValue[i])];
                        foreach(Vector2 point in points)
                        {
                            Handles.Label(point, 
                                String.Format("{0:F1}", WeightValue[i]),
                                new GUIStyle()
                                {
                                    fontSize = 20,
                                    normal = new GUIStyleState()
                                    {
                                        textColor = Color.red,
                                    }
                                });
                        }
                    }
                }
            }
            
            if (drawUnspawnable) 
            {
                for (int i = 0; i < WeightValue.Length; i++)
                {
                    if (_layerWeight.ContainsKey((LayerTag.UNSPAWNABLE, WeightValue[i])))
                    {
                        HashSet<Vector2> points = _layerWeight[(LayerTag.UNSPAWNABLE, WeightValue[i])];
                        foreach(Vector2 point in points)
                        {
                            Handles.Label(point, 
                                String.Format("{0:F1}", WeightValue[i]),
                                new GUIStyle()
                                {
                                    fontSize = 20,
                                    normal = new GUIStyleState()
                                    {
                                        textColor = Color.black,
                                    }
                                });
                        }
                    }
                }
            }

        }
    }
}

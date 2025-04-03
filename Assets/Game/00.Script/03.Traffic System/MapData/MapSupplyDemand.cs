using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._00.Manager.Observer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game._00.Script._03.Traffic_System.MapData
{
    public struct AlphaNode
    {
        public Vector2 Position;
        public float Alpha;
    }
    
    public class MapSupplyDemand : MonoBehaviour
    {
        [Header("Debug Property")]
        [SerializeField] private bool isGizmos;

        [SerializeField] private bool drawSupply;

        [SerializeField] private bool drawDemand;

        [SerializeField] private bool drawUnspawnable; 
        
        private Dictionary<string, AlphaNode[,]> _layerAlpha;
 
        private void Awake()
        {
            _layerAlpha = new Dictionary<string, AlphaNode[,]>();
            LoadTileLayers();
        }
        
        /// <summary>
        /// Load tile layers by loop and set create alpha node
        /// </summary>
        private void LoadTileLayers()
        {
            Tilemap[]  tilemaps = GetComponentsInChildren<Tilemap>();
            TilemapRenderer[] renderers = GetComponentsInChildren<TilemapRenderer>();

            if (renderers.Length != tilemaps.Length)
            {
                DebugUtility.LogError("There is a child that does not contain Tilemap renderer or TileMap componenet", this.gameObject.name);
                return;
            }
            
            for(int i =0; i < tilemaps.Length; i++)
            {
                if (IsValidTag(tilemaps[i].gameObject.tag))
                {
                    _layerAlpha.Add(tilemaps[i].gameObject.tag, LoadAlphaNodeMap(tilemaps[i], tilemaps[i].gameObject.tag));
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
        private AlphaNode[,] LoadAlphaNodeMap(Tilemap tilemap, string validTag)
        {
            int yMin = tilemap.cellBounds.yMin;
            int yMax = tilemap.cellBounds.yMax;
            int xMin = tilemap.cellBounds.xMin;
            int xMax = tilemap.cellBounds.xMax;
            
            AlphaNode[,] alphaNodes = new AlphaNode[yMax - yMin,  xMax - xMin];

            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    Vector3Int gridPos = new Vector3Int(x,y,0);
                    if (tilemap.HasTile(gridPos))
                    {
                        Vector3 worldPos = tilemap.layoutGrid.CellToWorld(gridPos);
                        float alphaVal = GetSpriteAlpha(tilemap, gridPos);
                        
                        //Remap yMin-Max to index in array [-5,5] to [0,10]
                        alphaNodes[y + Mathf.Abs(yMin), x + Mathf.Abs(xMin)] = new AlphaNode()
                        {
                            Alpha = alphaVal,
                            Position = worldPos,
                        };
                    }
                }
            }
            return alphaNodes;
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
            return tag == LayerFlags.DEMAND || tag == LayerFlags.SUPPLY || tag == LayerFlags.UNSPAWNABLE;
        }

        private void OnDrawGizmos()
        {
            if (!isGizmos)
            {
                return;
            }

            if (_layerAlpha.ContainsKey(LayerFlags.DEMAND) && drawDemand) 
            {
                AlphaNode[,] alphaNodes = _layerAlpha[LayerFlags.DEMAND];
                for (int i = 0; i < alphaNodes.GetLength(0); i++)
                {
                    for (int j = 0; j < alphaNodes.GetLength(1); j++)
                    {
                        Handles.Label(alphaNodes[i,j].Position, 
                            String.Format("{0:F1}", alphaNodes[i,j].Alpha),
                            new GUIStyle()
                            {
                                fontSize = 20,
                                normal = new GUIStyleState()
                                {
                                    textColor = Color.red
                                }
                            });
                    }
                }
            }
            
            if(_layerAlpha.ContainsKey(LayerFlags.SUPPLY) &&  drawSupply)
            {
                AlphaNode[,] alphaNodes = _layerAlpha[LayerFlags.SUPPLY];
                for (int i = 0; i < alphaNodes.GetLength(0); i++)
                {
                    for (int j = 0; j < alphaNodes.GetLength(1); j++)
                    {
                        Handles.Label(alphaNodes[i,j].Position, 
                            String.Format("{0:F1}", alphaNodes[i,j].Alpha),
                            new GUIStyle()
                            {
                                fontSize = 20,
                                normal = new GUIStyleState()
                                {
                                    textColor = Color.green
                                }
                            });
                    }
                }
            }

            if (_layerAlpha.ContainsKey(LayerFlags.UNSPAWNABLE) &&  drawUnspawnable)
            {
                AlphaNode[,] alphaNodes = _layerAlpha[LayerFlags.UNSPAWNABLE];
                for (int i = 0; i < alphaNodes.GetLength(0); i++)
                {
                    for (int j = 0; j < alphaNodes.GetLength(1); j++)
                    {
                        Handles.Label(alphaNodes[i,j].Position, 
                            String.Format("{0:F1}", alphaNodes[i,j].Alpha),
                            new GUIStyle()
                            {
                                fontSize = 20,
                                normal = new GUIStyleState()
                                {
                                    textColor = Color.blue
                                }
                            });
                    }
                }
            }
        }
    }
}

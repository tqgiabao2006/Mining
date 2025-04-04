using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game._00.Script._03.Traffic_System.MapData
{

    public class MapSupplyDemand : MonoBehaviour
    {
        [Header("Debug Property")] [SerializeField]
        private bool isGizmos;

        [SerializeField] private bool drawSupply;

        [SerializeField] private bool drawDemand;

        [SerializeField] private bool drawUnspawnable;

        private Dictionary<string, List<Node>> _layerAlpha;

        private Vector2 _size = Vector2.zero;
        
        public Vector2 Size
        {
            get
            {
                return _size;
            }
        }

        public Node this[string layerTag, int index]
        {
            get
            {
                if (IsValidTag(layerTag))
                {
                    if (index >= 0 && index < _layerAlpha[layerTag].Count)
                    {
                        return _layerAlpha[layerTag][index];
                    }
                }
                
                DebugUtility.LogError($"Invalid layer tag: {layerTag} or index", this.gameObject.name);
                return null;
            }
        }
        
        private void Start()
        {
            _layerAlpha = new Dictionary<string, List<Node>>();
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
                    _layerAlpha.Add(tilemaps[i].gameObject.tag, LoadAlphaNodeMap(tilemaps[i],tilemaps[i].gameObject.tag));
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
        /// <param name="renderer"></param>
        /// <param name="validTag">tag of layer</param>
        /// <returns>AlphaNode[,]</returns>
        private List<Node> LoadAlphaNodeMap(Tilemap tilemap,string validTag)
        {
            int yMin = tilemap.cellBounds.yMin;
            int yMax = tilemap.cellBounds.yMax;
            int xMin = tilemap.cellBounds.xMin;
            int xMax = tilemap.cellBounds.xMax;
            
            List<Node> nodes = new List<Node>();
            
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    Vector3Int gridPos = new Vector3Int(x,y,0);
                    if (tilemap.HasTile(gridPos))
                    {
                        Vector3 worldPos = tilemap.layoutGrid.CellToWorld(gridPos);
                        float alphaVal = GetSpriteAlpha(tilemap, gridPos);
                        Node node = GridManager.NodeFromWorldPosition(worldPos);
                        node.AlphaNode = new AlphaNode()
                        {
                            Value = alphaVal,
                            LayerTag = validTag
                        };
                        nodes.Add(node);
                    }
                }
            }
            return nodes;
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

        public int GetNodeCount(string layerTag)
        {
            return layerTag switch
            {
                LayerTag.DEMAND => _layerAlpha.ContainsKey(layerTag) ? _layerAlpha[layerTag].Count : 0,
                LayerTag.SUPPLY => _layerAlpha.ContainsKey(layerTag) ? _layerAlpha[layerTag].Count : 0,
                LayerTag.UNSPAWNABLE => _layerAlpha.ContainsKey(layerTag) ? _layerAlpha[layerTag].Count : 0,
                _ => 0
            };
        }

        private void OnDrawGizmos()
        {
            if (!isGizmos)
            {
                return;
            }

            if (_layerAlpha.ContainsKey(LayerTag.DEMAND) && drawDemand) 
            {
                List<Node> alphaNodes = _layerAlpha[LayerTag.DEMAND];
                for (int j = 0; j < alphaNodes.Count; j++)
                {
                    Handles.Label(alphaNodes[j].WorldPosition, 
                        String.Format("{0:F1}", alphaNodes[j].AlphaNode.Value),
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
            
            if(_layerAlpha.ContainsKey(LayerTag.SUPPLY) &&  drawSupply)
            {
                List<Node> alphaNodes = _layerAlpha[LayerTag.SUPPLY];
                for (int j = 0; j < alphaNodes.Count; j++)
                {
                    Handles.Label(alphaNodes[j].WorldPosition, 
                        String.Format("{0:F1}", alphaNodes[j].AlphaNode.Value),
                        new GUIStyle()
                        {
                            fontSize = 20,
                            normal = new GUIStyleState()
                            {
                                textColor = Color.yellow
                            }
                        });
                }
            }

            if (_layerAlpha.ContainsKey(LayerTag.UNSPAWNABLE) &&  drawUnspawnable)
            {
                List<Node> alphaNodes = _layerAlpha[LayerTag.UNSPAWNABLE];
                for (int j = 0; j < alphaNodes.Count; j++)
                {
                    Handles.Label(alphaNodes[j].WorldPosition, 
                        String.Format("{0:F1}", alphaNodes[j].AlphaNode.Value),
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

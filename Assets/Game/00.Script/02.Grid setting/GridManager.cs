using System.Collections.Generic;
using Game._00.Script._00.Manager.Custom_Editor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace Game._00.Script._02.Grid_setting
{
    public class GridManager: MonoBehaviour
    {
        [Header("Debug Mode")]
        
        [SerializeField] private bool debugMode = false;
        
        [SerializeField] private bool walkableDisplay;
        
        [SerializeField] private bool drawableDisplay;

        [SerializeField] private bool drawEmpty;

        [SerializeField] private bool drawBuildingZone;

        [SerializeField] private bool drawRoad;

        #region Variables
        //1. GridManager:
        public static Node[,] Grid
        {
            get; 
            private set; 
            
        }
   
        private readonly Vector2 _gridCenter = Vector2.zero;
       
        public static int GridSizeX {get; private set;}
       
        public static int GridSizeY{get; private set;}

        [CustomReadOnly] public static readonly Vector2 GridWorldSize = new Vector2(40,40); 
        
        [CustomReadOnly] public static readonly float NodeRadius = 0.5f;

        
        //Weight:
        public LayerMask UnwalkableMask;
        
        public TerrainType[] WalkableRegions;
        
        LayerMask walkableMask;
       
        Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
   
        //Blur;
      
        int penaltyMin = int.MaxValue;
       
        int penaltyMax = int.MinValue;
   
        public int MaxSize
        {
            get
            {
                return GridSizeX * GridSizeY;
            }
        }

        public int NodeCount
        {
            get { return (int)(GridSizeX * GridSizeY / NodeDiameter); }
        }
        
        public static float NodeDiameter {
            get
            {
                return NodeRadius * 2;
            }
        }



        #endregion
   
        #region Main Function


        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            // Calculate how many nodes (horizontally) the _gridManager can have, return int value so Round To Int 
            GridSizeX = Mathf.RoundToInt(GridWorldSize.x / NodeDiameter);
            GridSizeY = Mathf.RoundToInt(GridWorldSize.y / NodeDiameter);

            foreach (TerrainType region in WalkableRegions)
            {
                walkableMask.value = walkableMask | region.terrainMask.value;
                //bitwise OR operation to combine 2 mask
                //Layer mask store in: 00000 000000 00000 00000 if layer is 9 , the "1" in the 9th place
                /*
               00001010  (walkableMask)
               00000101  (region.terrainMask)
               --------
               00001111  (resulting walkableMask)
           */
                walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
                // if layer is 9 it store in 2^9 = 512


            }
            CreateGrid();
        }
        private void CreateGrid()
        {
            Grid = new Node[GridSizeX, GridSizeY];
            //Get bottom left node position
            UnityEngine.Vector2 worldBottomLeft = (UnityEngine.Vector2)_gridCenter
                                                  - UnityEngine.Vector2.right * GridWorldSize.x / 2 // substact the length of a half square
                                                  - UnityEngine.Vector2.up * GridWorldSize.y / 2;

       
            //this.transform.position = center world;
            for (int x = 0; x < GridSizeX; x++)
            {
                for (int y = 0; y < GridSizeY; y++)
                {
                    UnityEngine.Vector3 worldPoint = worldBottomLeft + UnityEngine.Vector2.right * (x * NodeDiameter + NodeRadius)
                                                                     + UnityEngine.Vector2.up * (y * NodeDiameter + NodeRadius);



                    bool walkable = false;
                    int movementPenalty = 0;
                    Ray ray = new Ray(worldPoint + new UnityEngine.Vector3(0,0,-50),new UnityEngine.Vector3 ( 0, 0, 1));
                    RaycastHit hit;
                    if((Physics.Raycast(ray, out hit, 100)))
                    {
                        if((UnwalkableMask.value & (1 << hit.collider.gameObject.layer)) != 0)
                        {
                            walkable = false;
                        }
                        else
                        {
                            walkable = true;
                            walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                        }
                    }
                    Grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty, this); //Create new node
                    // the (y * nodeDiamter + nodeRadius is calculate the offset of 1 point to the origin)
                    //if P(0,1) => it is 1 * node parament + the radius (diamete/2) is the right center of the point
                }
            }
       
            BlurPenaltyMap(3);
        }

        public void UpdateWalkable(Vector2 roadMesh)
        {
            Node roadNode = NodeFromWorldPosition(roadMesh);
            roadNode.SetWalkable(true);
        }


        void BlurPenaltyMap(int blurSize) {
      
            //Box blur agorithm
            int kernelSize = blurSize * 2 + 1;
            int kernelExtents = (kernelSize - 1) / 2;
       
            //Horizontal:
            int[,] penaltiesHorizontalPass = new int[GridSizeX,GridSizeY];
            int[,] penaltiesVerticalPass = new int[GridSizeX,GridSizeY];


            for (int y = 0; y < GridSizeY; y++) {
                for (int x = -kernelExtents; x <= kernelExtents; x++) {
                    int sampleX = Mathf.Clamp (x, 0, kernelExtents);
                    penaltiesHorizontalPass [0, y] += Grid [sampleX, y].MovementPenalty;
                }


                for (int x = 1; x < GridSizeX; x++) {
                    int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, GridSizeX);
                    int addIndex = Mathf.Clamp(x + kernelExtents, 0, GridSizeX-1);


                    penaltiesHorizontalPass [x, y] = penaltiesHorizontalPass [x - 1, y] - Grid [removeIndex, y].MovementPenalty + Grid [addIndex, y].MovementPenalty;
                }
            }
       
            //Vertical:
            for (int x = 0; x < GridSizeX; x++) {
                for (int y = -kernelExtents; y <= kernelExtents; y++) {
                    int sampleY = Mathf.Clamp (y, 0, kernelExtents);
                    penaltiesVerticalPass [x, 0] += penaltiesHorizontalPass [x, sampleY];
                }


                int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, 0] / (kernelSize * kernelSize));
                Grid [x, 0].MovementPenalty = blurredPenalty;


                for (int y = 1; y < GridSizeY; y++) {
                    int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, GridSizeY);
                    int addIndex = Mathf.Clamp(y + kernelExtents, 0, GridSizeY-1);


                    penaltiesVerticalPass [x, y] = penaltiesVerticalPass [x, y-1] - penaltiesHorizontalPass [x,removeIndex] + penaltiesHorizontalPass [x, addIndex];
                    blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, y] / (kernelSize * kernelSize));
                    Grid [x, y].MovementPenalty = blurredPenalty;
              
              
                    //GetMaxMin ----> Gizmos
                    if (blurredPenalty > penaltyMax) {
                        penaltyMax = blurredPenalty;
                    }
                    if (blurredPenalty < penaltyMin) {
                        penaltyMin = blurredPenalty;
                    }
                }
            }


        }
   
        public static Node NodeFromWorldPosition(Vector2 worldPosition)
        {
            // Check for the zero vector case
            if (worldPosition == Vector2.zero)
            {
                // Return the center node of the _gridManager
                int centerX = GridSizeX / 2;
                int centerY = GridSizeY / 2;
                return Grid[centerX, centerY];
            }
       
            float percentX = worldPosition.x / GridWorldSize.x + 0.5f;
            float percentY = worldPosition.y / GridWorldSize.y + 0.5f;
            //if worldPosition = (0,y) percentX = 0, (x, y) = 1, in center = 0.5x
            // worldPoint.x/worldSize.x = the index x-axis of it, + 0.5f is center of it;


            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);
            //Make sure it not outsize the _gridManager


            int x = Mathf.FloorToInt(Mathf.Clamp((GridSizeX) * percentX, 0, GridSizeX - 1));
            //gridSizeX - 1 because in the array system, count from 0, so do it avoid out range of array
            int y = Mathf.FloorToInt(Mathf.Clamp((GridSizeY) * percentY, 0, GridSizeY - 1));
            return Grid[x, y];
        }
        #endregion

   
        #region Gizmos and UI
        public List<Node> path;
        void OnDrawGizmos() 
        {
            Gizmos.DrawWireCube(_gridCenter, new UnityEngine.Vector2(GridWorldSize.x, GridWorldSize.y));

            if (Grid == null || !debugMode)
            {
                return;
            }
            foreach (Node n in Grid) {

                if (walkableDisplay)
                {
                    // Compute the color based on the movement penalty
                    float normalizedPenalty = Mathf.InverseLerp(penaltyMin, penaltyMax, n.MovementPenalty);
                    Color penaltyColor = Color.Lerp(Color.white, Color.black, normalizedPenalty);
                    Gizmos.color = n.Walkable ? penaltyColor : Color.red;
                        
                }

                if (drawableDisplay)
                {
                    Gizmos.color = n.CanDraw ? Color.white : Color.black;
                }

                if (drawEmpty)
                {
                    Gizmos.color = n.IsEmpty ? Color.white : Color.red;
                }

                if (drawBuildingZone)
                {
                    Gizmos.color = n.IsBuilding ? Color.blue : Color.white;
                }

                if (drawRoad)
                {
                    Gizmos.color = n.IsRoad ? Color.yellow : Color.white;
                }
                // Draw the gizmo cube at the node's position
                Gizmos.DrawCube(n.WorldPosition, Vector2.one * (NodeDiameter -0.05f));
            }
        }
        #endregion
    }
}







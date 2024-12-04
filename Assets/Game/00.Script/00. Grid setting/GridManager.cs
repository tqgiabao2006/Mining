using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;


public class GridManager: MonoBehaviour
{
   [SerializeField] bool displayOnGizmos;

   #region Variables
   //1. GridManager:
   public Node[,] grid { get; private set; }
   
   private Vector2 gridCenter = Vector2.zero;
   public float NodeDiameter{get; private set;}
   public int GridSizeX {get; private set;}
   public int GridSizeY{get; private set;}


   public UnityEngine.Vector2 GridWorldSize; 
   [SerializeField]public float NodeRadius;

   public int NodeCount
   {
       get { return (int)(GridSizeX * GridSizeY / NodeDiameter); }
   }


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



   #endregion
   
   #region Main Function


   private void Awake()
   {
       Initialize();

   }

   private void Initialize()
   {
       // diameter = 2R
       NodeDiameter = NodeRadius * 2;
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
       grid = new Node[GridSizeX, GridSizeY];
       //Get bottom left node position
       UnityEngine.Vector2 worldBottomLeft = (UnityEngine.Vector2)gridCenter
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
               grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty, this); //Create new node
                       // the (y * nodeDiamter + nodeRadius is calculate the offset of 1 point to the origin)
                       //if P(0,1) => it is 1 * node parament + the radius (diamete/2) is the right center of the point
           }
       }
       
       BlurPenaltyMap(3);
   }

   public void UpdateWalkable(Vector2 roadMesh)
   {
       Node roadNode = NodeFromWorldPosition(roadMesh);
       roadNode.Walkable = true;
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
               penaltiesHorizontalPass [0, y] += grid [sampleX, y].MovementPenalty;
           }


           for (int x = 1; x < GridSizeX; x++) {
               int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, GridSizeX);
               int addIndex = Mathf.Clamp(x + kernelExtents, 0, GridSizeX-1);


               penaltiesHorizontalPass [x, y] = penaltiesHorizontalPass [x - 1, y] - grid [removeIndex, y].MovementPenalty + grid [addIndex, y].MovementPenalty;
           }
       }
       
       //Vertical:
       for (int x = 0; x < GridSizeX; x++) {
           for (int y = -kernelExtents; y <= kernelExtents; y++) {
               int sampleY = Mathf.Clamp (y, 0, kernelExtents);
               penaltiesVerticalPass [x, 0] += penaltiesHorizontalPass [x, sampleY];
           }


           int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, 0] / (kernelSize * kernelSize));
           grid [x, 0].MovementPenalty = blurredPenalty;


           for (int y = 1; y < GridSizeY; y++) {
               int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, GridSizeY);
               int addIndex = Mathf.Clamp(y + kernelExtents, 0, GridSizeY-1);


               penaltiesVerticalPass [x, y] = penaltiesVerticalPass [x, y-1] - penaltiesHorizontalPass [x,removeIndex] + penaltiesHorizontalPass [x, addIndex];
               blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, y] / (kernelSize * kernelSize));
               grid [x, y].MovementPenalty = blurredPenalty;
              
              
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
   
   public Node NodeFromWorldPosition(Vector2 worldPosition)
   {
       // Check for the zero vector case
       if (worldPosition == Vector2.zero)
       {
           // Return the center node of the _gridManager
           int centerX = GridSizeX / 2;
           int centerY = GridSizeY / 2;
           return grid[centerX, centerY];
       }
       
       float percentX = worldPosition.x / GridWorldSize.x + 0.5f;
       float percentY = worldPosition.y / GridWorldSize.y + 0.5f;
       //if worldPoision = (0,y) percentX = 0, (x, y) = 1, in center = 0.5x
       // worldpoint.x/worldsize.x = the index x-axis of it, + 0.5f is center of it;


       percentX = Mathf.Clamp01(percentX);
       percentY = Mathf.Clamp01(percentY);
       //Make sure it not outsize the _gridManager


       int x = Mathf.FloorToInt(Mathf.Clamp((GridSizeX) * percentX, 0, GridSizeX - 1));
       //gridSizeX - 1 because in the array system, count from 0, so do it avoid out range of array
       int y = Mathf.FloorToInt(Mathf.Clamp((GridSizeY) * percentY, 0, GridSizeY - 1));
       return grid[x, y];
   }
   #endregion

   
   #region Gizmos and UI
   public List<Node> path;
   void OnDrawGizmos() 
   {
    Gizmos.DrawWireCube(gridCenter, new UnityEngine.Vector2(GridWorldSize.x, GridWorldSize.y));
    
    if (grid != null && displayOnGizmos) 
    {
       foreach (Node n in grid) {
           // Compute the color based on the movement penalty
           float normalizedPenalty = Mathf.InverseLerp(penaltyMin, penaltyMax, n.MovementPenalty);
           Color penaltyColor = Color.Lerp(Color.white, Color.black, normalizedPenalty);
           // Set the gizmo color based on walk ability
           Gizmos.color = n.Walkable ? penaltyColor : Color.red;
           // Draw the gizmo cube at the node's position
           Gizmos.DrawCube(n.WorldPosition, Vector2.one * (NodeDiameter -0.05f));
       }
    }
   }
   #endregion
}

[System.Serializable]
public class TerrainType
{
   public LayerMask terrainMask;
   public int terrainPenalty;
}




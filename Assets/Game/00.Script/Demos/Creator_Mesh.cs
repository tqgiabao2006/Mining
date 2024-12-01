    // using System;
    // using System.Collections.Generic;
    // using Game._00.Script._05._Manager;
    // using Unity.Collections;
    // using Unity.Mathematics;
    // using Unity.VisualScripting;
    // using UnityEngine;
    // using UnityEngine.Serialization;
    // using Vector2 = UnityEngine.Vector2;
    // using Vector3 = UnityEngine.Vector3;
    // public enum RoadType
    // {
    //     T_intersection,
    //     Crossroad,
    //     Connection,
    //     Corner,
    //     DeadEnd,
    //     
    //     None
    // }
    //
    // public struct RoadDetails
    // {
    //     public RoadType Type { get; set; }
    //     public DirectionType Direction { get; set; }
    //
    //     public RoadDetails(RoadType type, DirectionType direction)
    //     {
    //         Type = type;
    //         Direction = direction;
    //     }
    // }
    // [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    // public class Creator_Mesh: MonoBehaviour
    // {
    //     #region Variables:
    //     
    //     [FormerlySerializedAs("_roadWidth")]
    //     [Header("Mesh Setting")]
    //     [SerializeField] public float roadWidth = 1f;
    //     [SerializeField] public Material roadMaterial;
    //     [SerializeField] public int curveSmoothness = 20;
    //      [ReadOnly] private float smoothProp = 0.8f; //For curve measurement
    //     [SerializeField] [Range(0.01f, 0.05f)] public float oppositeCornerSmoothness = 0.02f;
    //
    //     [ReadOnly] private float extraPer = 0.5f; //For cross and T junction only
    //     private MeshFilter _meshFilter;
    //     private MeshRenderer _meshRenderer;
    //     private Material _material;
    //     
    //     private List<Vector3> _vertices;
    //     private List<int> _triangles; 
    //     private List<Vector2> _uvs;
    //     
    //     private GridManager _gridManager;
    //     
    //     private List<CombineInstance> meshCombineList = new List<CombineInstance>();
    //     
    //     private Dictionary<Node, CombineInstance> nodeCombineInstances = new Dictionary<Node, CombineInstance>();
    //     
    //     private GameManager _gameManager;
    //     private RoadManager _roadManager;
    //     
    //     
    //
    //
    //     #endregion
    //
    //     private void Start()
    //     {
    //         Initial();
    //     }
    //
    //     private void Initial()
    //     {
    //         _meshFilter = GetComponent<MeshFilter>();
    //         _gridManager = FindObjectOfType<GridManager>();
    //         _meshRenderer = GetComponent<MeshRenderer>(); 
    //         _gameManager = GameManager.Instance;    
    //         _roadManager = _gameManager.RoadManager;
    //         
    //         // Initialize lists for the mesh data
    //         _vertices = new List<Vector3>();
    //         _triangles = new List<int>();
    //         _uvs = new List<Vector2>();
    //     }
    //
    //     
    //     /// <summary>
    //     /// Call to intialize road for building
    //     /// </summary>
    //     public void SetBuildingRoadMesh(Node node, RoadDetails roadDetails)
    //     {
    //         var generatedMesh = GenerateMesh(node, roadDetails); 
    //         if (generatedMesh != null)
    //         {
    //             StoreMeshData(node, generatedMesh);
    //         }
    //         
    //     }
    //  
    //     /// <summary>
    //     /// Only call in building phase
    //     /// </summary>
    //     /// <param name="node"></param>
    //     /// <param name="isPlacing"></param>
    //     public void ChangeRoadMesh(Node node)
    //     {
    //         var roadDetails = GetRoadDetails(node);
    //         var generatedMesh = GenerateMesh(node, roadDetails);
    //
    //         if (generatedMesh != null)
    //         {
    //             StoreMeshData(node, generatedMesh);
    //         }
    //     }
    //     
    //     private void StoreMeshData(Node node, Mesh generatedMesh)
    //     {
    //         node.SetRoad(true);
    //
    //         // Destroy old mesh if it exists to prevent memory leaks
    //         if (nodeCombineInstances.TryGetValue(node, out CombineInstance oldInstance))
    //         {
    //             DestroyImmediate(oldInstance.mesh);
    //         }
    //
    //         CombineInstance combineInstance = new CombineInstance
    //         {
    //             mesh = generatedMesh
    //         };
    //         nodeCombineInstances[node] = combineInstance;
    //     }
    //
    //     public void ApplyCombinedRoadMesh()
    //     {
    //         // Clear meshCombineList to prevent stacking instances
    //         meshCombineList.Clear();
    //
    //         // Rebuild meshCombineList from updated CombineInstances in nodeCombineInstances dictionary
    //         foreach (var combineInstance in nodeCombineInstances.Values)
    //         {
    //             meshCombineList.Add(combineInstance);
    //         }
    //
    //         if (meshCombineList.Count > 0)
    //         {
    //             Mesh combinedMesh = new Mesh();
    //             combinedMesh.CombineMeshes(meshCombineList.ToArray(), true, false);
    //             combinedMesh.RecalculateNormals();
    //             combinedMesh.RecalculateBounds();
    //
    //             // Clear the old mesh to ensure it's fully replaced
    //             if (_meshFilter.mesh != null)
    //             {
    //                 _meshFilter.mesh.Clear();
    //             }
    //
    //             // Assign the new combined mesh
    //             _meshFilter.mesh = combinedMesh;
    //             _meshRenderer.material = roadMaterial;
    //         }
    //     }
    //     
    //     private Mesh GenerateMesh(Node node, RoadDetails roadDetails)
    //     {
    //         // Generate a mesh based on the road type and properties
    //         switch (roadDetails.Type)
    //         {
    //             case RoadType.DeadEnd:
    //                 return CreateDeadEndMesh(roadDetails, node, roadWidth, curveSmoothness);
    //             case RoadType.Connection:
    //                 if (roadDetails.Direction == DirectionType.Diagonal)
    //                 {
    //                     List<Node> neighborsNodes = node.GetNeighbours();
    //                     Vector2 dir = neighborsNodes[0].WorldPosition - neighborsNodes[1].WorldPosition;
    //                     float angle = GetVectorAngle(dir, Vector2.right);
    //                     Mesh diagMesh = CreateDiagionalMesh(roadDetails, node, angle, extraPer, roadWidth);
    //                     Debug.Log("Diagonal Mesh Created :" + diagMesh.bounds.center);
    //                     return diagMesh;
    //                 }
    //                 
    //                 return CreateContinueMesh(roadDetails, node, roadWidth);
    //             
    //             case RoadType.T_intersection:
    //                 return CreateTJunctionMesh(roadDetails, node, extraPer, oppositeCornerSmoothness, roadWidth, curveSmoothness);
    //             case RoadType.Corner:
    //                 return CreateCornerMesh(roadDetails, node, smoothProp, roadWidth, curveSmoothness);
    //             case RoadType.Crossroad:
    //                 return CreateCrossRoadMesh(roadDetails, node, extraPer, oppositeCornerSmoothness, roadWidth, curveSmoothness);
    //             default:
    //                 return null; // Or a default mesh if needed
    //         }
    //     }
    //
    //
    //
    // #region Road Details
    // // ReSharper disable Unity.PerformanceAnalysis
    // private RoadDetails GetRoadDetails(Node mainNode)
    // {
    //     float tolerance = 0.01f;
    //     List<Node> affectedNodes = _roadManager.GetRoadList(mainNode);
    //     
    //     switch (affectedNodes.Count)
    //     { 
    //         case 1: // Dead-end
    //             Vector2 connectedDir = (affectedNodes[0].WorldPosition - mainNode.WorldPosition).normalized;
    //             return new RoadDetails(RoadType.DeadEnd, GetOppositeDirection(connectedDir));
    //         
    //         case 2: // Connection or Corner
    //             Vector2 dir1 = (affectedNodes[0].WorldPosition - mainNode.WorldPosition).normalized;
    //             Vector2 dir2 = (affectedNodes[1].WorldPosition - mainNode.WorldPosition).normalized;
    //             float angle = GetVectorAngle(dir1, dir2);
    //
    //             if (angle >= 90 - tolerance && angle <= 90 + tolerance)
    //             {
    //                 // It's a corner
    //                 return new RoadDetails(RoadType.Corner, GetCornerDirection(dir1, dir2));
    //             }
    //             if ((angle >= 45 - tolerance && angle <= 45 + tolerance) || (angle >= 135 - tolerance && angle <= 135 + tolerance))
    //             {
    //                 // Diagonal connection
    //                 return new RoadDetails(RoadType.Connection, DirectionType.Diagonal);
    //             }
    //                 // Straight connection
    //                 return new RoadDetails(RoadType.Connection, GetDirectionFromVector(dir1, false));
    //           
    //
    //         case 3: // T-juncti onp
    //           Vector2 dir1_T = (affectedNodes[0].WorldPosition - mainNode.WorldPosition).normalized;
    //             Vector2 dir2_T = (affectedNodes[1].WorldPosition - mainNode.WorldPosition).normalized;
    //             Vector2 dir3_T= (affectedNodes[2].WorldPosition - mainNode.WorldPosition).normalized;
    //             return new RoadDetails(RoadType.T_intersection, GetTJunctionDirection(dir1_T, dir2_T, dir3_T));
    //         case 4:
    //             return new RoadDetails(RoadType.Crossroad, DirectionType.None);
    //         
    //         default:
    //             Debug.LogError("Road Details not found");
    //             return new RoadDetails(RoadType.None, DirectionType.None);
    //     }
    // }
    //
    // #region Helper road detail
    //
    // //Avoid small difference
    // private float GetVectorAngle(Vector2 dir1, Vector2 dir2, float tolerance = 0.01f)
    // {
    //   float angle = Vector2.Angle(dir1, dir2);
    //   if (Mathf.Abs(angle - 90) < tolerance)
    //   {
    //       return 90;
    //   }else if (math.abs(angle - 45) < tolerance )
    //   {
    //       return 45;
    //   }else if(Mathf.Abs(angle - 135) < tolerance)
    //   {
    //       return 135;
    //   }
    //   else
    //   {
    //       return 180;
    //   }
    // }
    // private DirectionType GetOppositeDirection(Vector2 connectedDir)
    // {
    //     float tolerance = 0.01f;
    //
    //     if (Vector2.Dot(connectedDir, Vector2.right) > 1 - tolerance) return DirectionType.Left;
    //     if (Vector2.Dot(connectedDir, Vector2.left) > 1 - tolerance) return DirectionType.Right;
    //     if (Vector2.Dot(connectedDir, Vector2.up) > 1 - tolerance) return DirectionType.Down;
    //     if (Vector2.Dot(connectedDir, Vector2.down) > 1 - tolerance) return DirectionType.Up;
    //
    //     return DirectionType.None;
    // }
    //
    // private DirectionType GetDirectionFromVector(Vector2 dir, bool isDeadEnd)
    // {
    //     float tolerance = 0.1f; // Increase tolerance for more leniency
    //
    //     // Check for Right direction
    //     if (Mathf.Abs(Vector2.Dot(dir, Vector2.right) - 1f) < tolerance)
    //     {
    //         return isDeadEnd ? DirectionType.Left : DirectionType.Right;
    //
    //     }
    //
    //     // Check for Left direction
    //     if (Mathf.Abs(Vector2.Dot(dir, Vector2.left) - 1f) < tolerance)
    //         return isDeadEnd ? DirectionType.Right : DirectionType.Left;
    //
    //     // Check for Up direction
    //     if (Mathf.Abs(Vector2.Dot(dir, Vector2.up) - 1f) < tolerance)
    //         return isDeadEnd ? DirectionType.Down : DirectionType.Up;
    //
    //     // Check for Down direction
    //     if (Mathf.Abs(Vector2.Dot(dir, Vector2.down) - 1f) < tolerance)
    //         return isDeadEnd ? DirectionType.Up : DirectionType.Down;
    //
    //     // Return None if no direction is found
    //     return DirectionType.None;
    // }
    //
    //
    // private DirectionType GetCornerDirection(Vector2 dir1, Vector2 dir2)
    // {
    //     float tolerance = 0.01f;
    //
    //     if ((IsDirection(dir1, Vector2.right, tolerance) && IsDirection(dir2, Vector2.up, tolerance)) ||
    //         (IsDirection(dir2, Vector2.right, tolerance) && IsDirection(dir1, Vector2.up, tolerance)))
    //     {
    //         return DirectionType.BottomRight;
    //     }
    //
    //     if ((IsDirection(dir1, Vector2.right, tolerance) && IsDirection(dir2, Vector2.down, tolerance)) ||
    //         (IsDirection(dir2, Vector2.right, tolerance) && IsDirection(dir1, Vector2.down, tolerance)))
    //     {
    //         return DirectionType.TopRight;
    //     }
    //
    //     if ((IsDirection(dir1, Vector2.left, tolerance) && IsDirection(dir2, Vector2.up, tolerance)) ||
    //         (IsDirection(dir2, Vector2.left, tolerance) && IsDirection(dir1, Vector2.up, tolerance)))
    //     {
    //         return DirectionType.BottomLeft;
    //     }
    //
    //     if ((IsDirection(dir1, Vector2.left, tolerance) && IsDirection(dir2, Vector2.down, tolerance)) ||
    //         (IsDirection(dir2, Vector2.left, tolerance) && IsDirection(dir1, Vector2.down, tolerance)))
    //     {
    //         return DirectionType.TopLeft;
    //     }
    //
    //     return DirectionType.None;
    // }
    //
    // private DirectionType GetTJunctionDirection(Vector2 dir1, Vector2 dir2, Vector2 dir3)
    // {
    //     Vector2 specialBranch = GetPerpendicularBranch(dir1, dir2, dir3);
    //     return GetDirectionFromVector(specialBranch, false);
    // }
    //
    // private Vector2 GetPerpendicularBranch(Vector2 dir1, Vector2 dir2, Vector2 dir3)
    // {
    //     float tolerance = 0.01f;
    //
    //     // Check if dir3 is perpendicular to both dir1 and dir2
    //     if (Mathf.Abs(Vector2.Dot(dir1, dir3)) < tolerance && Mathf.Abs(Vector2.Dot(dir2, dir3)) < tolerance)
    //         return dir3;
    //
    //     // Check if dir1 is perpendicular to both dir2 and dir3
    //     if (Mathf.Abs(Vector2.Dot(dir2, dir1)) < tolerance && Mathf.Abs(Vector2.Dot(dir3, dir1)) < tolerance)
    //         return dir1;
    //
    //     // Check if dir2 is perpendicular to both dir1 and dir3
    //     if (Mathf.Abs(Vector2.Dot(dir1, dir2)) < tolerance && Mathf.Abs(Vector2.Dot(dir3, dir2)) < tolerance)
    //         return dir2;
    //
    //     return Vector2.zero;
    // }
    //
    //
    // private bool IsDirection(Vector2 dir, Vector2 targetDir, float tolerance)
    // {
    //     return Vector2.Dot(dir, targetDir) > 1 - tolerance;
    // }
    // #endregion
    //
    // #endregion
    //
    //
    // #region Create Mesh
    //
    // private Mesh CreateContinueMesh(RoadDetails roadDetails, Node node, float roadWidth = 0.5f)
    // {
    //     Mesh continueMesh = new Mesh();
    //     // Use roadWidth to adjust the size of the mesh
    //     List<Vector3> vertices = new List<Vector3>();
    //     List<int> triangles = new List<int>();
    //     AddRectangleMesh(vertices, triangles,roadDetails.Direction, node.WorldPosition, roadWidth);
    //     // Define the triangles to form the two faces of the quad
    //
    //     // Clear and update the mesh with vertices and triangles
    //     UpdateMesh(continueMesh, vertices.ToArray(), triangles.ToArray());
    //     return continueMesh;
    // }
    //
    // /// <summary>
    // /// Dead end curve CENTER = 1/2 of node radius
    // /// </summary>
    // /// <param name="roadDetails"></param>
    // /// <param name="node"></param>
    // /// <param name="roadWidth"></param>
    // /// <param name="curveSmoothness"></param>
    // /// <returns></returns>
    // private Mesh CreateDeadEndMesh(RoadDetails roadDetails, Node node, float roadWidth = 0.5f, int curveSmoothness = 10)
    // {
    //     Mesh deadEndMesh = new Mesh();
    //     float halfWidth = roadWidth / 2;    
    //     List<Vector3> vertices = new List<Vector3>();
    //     List<int> triangles = new List<int>();
    //     
    //     Tuple<int, int> angles = GetDeadEndAngle(roadDetails);
    //     float[] rectangleScale = GetDeadEndScale(roadDetails.Direction);
    //     Vector3 curveCenter = node.WorldPosition;
    //     
    //     AddRectangleMesh(vertices, triangles, roadDetails.Direction, node.WorldPosition ,this.roadWidth, rectangleScale[0], rectangleScale[1], rectangleScale[2], rectangleScale[3]);
    //     AddCurveMesh(vertices, triangles, curveCenter, curveSmoothness, halfWidth, angles ); 
    //     UpdateMesh(deadEndMesh,vertices.ToArray(),triangles.ToArray());
    //     return deadEndMesh;
    // }
    //
    // /// <summary>
    // /// Center node = 1, small nodes = 1/4 node, radius = 1/4 road width, count -> bottom left -> bottom right -> top right -> top left
    // /// </summary>
    // /// <param name="roadDetails"></param>
    // /// <returns></returns>
    // private Mesh CreateTJunctionMesh(RoadDetails roadDetails, Node node, float extraPer, float oppositeCornerSmoothness,float roadWidth = 0.5f, int curveSmoothnes = 10)
    // {
    //     Mesh TJunctionMesh = new Mesh();
    //     float halfWidth = roadWidth / 2;    
    //     List<Vector3> vertices = new List<Vector3>();
    //     List<int> triangles = new List<int>();
    //     AddTJunctionMesh(node, vertices, triangles, roadDetails, oppositeCornerSmoothness, extraPer, curveSmoothnes, halfWidth);
    //     UpdateMesh(TJunctionMesh, vertices.ToArray(), triangles.ToArray()); 
    //     return TJunctionMesh;
    // }
    //
    // private Mesh CreateDiagionalMesh(RoadDetails roadDetails, Node node, float angle, float extraPer,float roadWidth = 0.5f)
    // {
    //     // Variables:
    //     Mesh diagionalMesh = new Mesh();
    //     float halfWidth = roadWidth / 2;    
    //     List<Vector3> vertices = new List<Vector3>();
    //     List<int> triangles = new List<int>();
    //     float smoothProportion = extraPer;
    //     
    //     //Add
    //     AddDiagonalPoints(node.WorldPosition, halfWidth, angle, vertices, triangles);
    //     //Update mesh:
    //      UpdateMesh(diagionalMesh, vertices.ToArray(), triangles.ToArray());
    //      return diagionalMesh;
    //     
    // }  
    //
    // private Mesh CreateCrossRoadMesh(RoadDetails roadDetails, Node node,float extraPer, float oppositeCornerSmoothness ,float roadWidth = 0.5f, int curveSmoothness = 20 )
    // { 
    //     //Variables:
    //     Mesh crossRoadMesh = new Mesh();
    //     float halfWidth = roadWidth / 2;    
    //     List<Vector3> vertices = new List<Vector3>();
    //     List<int> triangles = new List<int>();
    //     float extraEdge = 1 + extraPer; //Proportion
    //     
    //     //Add extension:
    //     AddRectangleMesh(vertices, triangles, DirectionType.Right, node.WorldPosition, roadWidth, extraEdge, extraEdge, 1,1); //Horizontal extension
    //     AddRectangleMesh(vertices, triangles, DirectionType.Down, node.WorldPosition, roadWidth, 1, 1, extraEdge, extraEdge); //Horizontal extension
    //     
    //     //Add 4 opposite corner
    //     AddOppositeCorner(node, DirectionType.TopLeft, vertices, triangles, curveSmoothness, halfWidth, extraPer, oppositeCornerSmoothness);
    //     AddOppositeCorner(node, DirectionType.TopRight, vertices, triangles, curveSmoothness, halfWidth, extraPer, oppositeCornerSmoothness);
    //     AddOppositeCorner(node, DirectionType.BottomRight, vertices, triangles, curveSmoothness, halfWidth, extraPer, oppositeCornerSmoothness);
    //     AddOppositeCorner(node, DirectionType.BottomLeft, vertices, triangles, curveSmoothness, halfWidth, extraPer, oppositeCornerSmoothness);
    //     
    //     UpdateMesh(crossRoadMesh, vertices.ToArray(), triangles.ToArray());
    //     return crossRoadMesh;
    // }
    //
    // /// <summary>
    // /// Curve = 1/4 circle, extended rectangle = 1/4 center, count -> bottom left -> bottom right -> top right -> top left
    // /// </summary>
    // private Mesh CreateCornerMesh(RoadDetails roadDetails, Node node,  float smoothProportion, float roadWidth = 0.5f, int curveSmoothness = 10)
    // {
    //     //Variables:
    //     Mesh cornerMesh = new Mesh();
    //     float halfWidth = roadWidth / 2;    
    //     List<Vector3> vertices = new List<Vector3>();
    //     List<int> triangles = new List<int>();
    //     float extraEdge = 1 + smoothProportion;
    //     CreateCorner(vertices, triangles, node, roadDetails, this.roadWidth,curveSmoothness, extraEdge);
    //    UpdateMesh(cornerMesh, vertices.ToArray(), triangles.ToArray());
    //    return cornerMesh;
    // }
    //
    // #endregion
    //
    //
    // #region DeadEnd Helper
    // /// <summary>
    // /// Scale: Left -> Right -> Up -> Down
    // /// Scale = direction => = 1/2f
    // /// </summary>
    // /// <param name="direction"></param>
    // /// <returns></returns>
    // private float[] GetDeadEndScale(DirectionType direction)
    // {
    //     switch (direction)
    //     {
    //         case DirectionType.Down:
    //             return new float[] { 1, 1, 1, 0f };
    //         case DirectionType.Up:
    //             return new float[] { 1, 1, 0,1};
    //         case DirectionType.Right:
    //             return new float[] { 1, 0, 1, 1 };
    //         case DirectionType.Left:
    //             return new float[] { 0, 1, 1,1};
    //     }
    //
    //     return new float[] { 1, 1, 1, 1 };
    // }
    //
    // private Tuple<int, int> GetDeadEndAngle(RoadDetails roadDetails)
    // {
    //     //Tuple<startAngle,endAngle>;
    //     switch (roadDetails.Direction)
    //     {
    //         case DirectionType.Left:
    //            return new Tuple<int, int>(90, 270);
    //         
    //         case DirectionType.Right:
    //             return new Tuple<int, int>(270, 450);
    //         
    //         case DirectionType.Up:
    //             return new Tuple<int, int>(0, 180);
    //         
    //         case DirectionType.Down:
    //             return new Tuple<int, int>(180, 360);
    //     }
    //     return new Tuple<int, int>(0, 0);
    //     
    // }
    //     
    //
    //
    // #endregion
    //
    //
    // #region Diagional Helper
    //
    // private void AddDiagonalPoints(Vector3 center, float halfWidth, float angle, List<Vector3> vertices, List<int> triangles)
    // {
    //     // Distance from the center to the corners of the node
    //     float dist = Mathf.Sqrt(Mathf.Pow(halfWidth, 2) + Mathf.Pow(_gridManager.NodeRadius, 2));
    //
    //     // Angle from the center to the top-left corner
    //     float angleOffset = Mathf.Atan(halfWidth / dist);  // Same angle offset for symmetry
    //
    //     // Calculate angles for each point based on the input angle
    //     float topLeftAngle = angle * Mathf.Deg2Rad + angleOffset;
    //     float topRightAngle = angle * Mathf.Deg2Rad - angleOffset;
    //     float botLeftAngle = angle * Mathf.Deg2Rad + (Mathf.PI - angleOffset);  // PI added to go to the bottom
    //     float botRightAngle = angle * Mathf.Deg2Rad + (Mathf.PI + angleOffset); // PI added and adjusted for bottom right
    //
    //     // Calculate positions of the four corners
    //     Vector3 topLeft = new Vector3(
    //         center.x + Mathf.Cos(topLeftAngle) * dist,
    //         center.y + Mathf.Sin(topLeftAngle) * dist,
    //         0
    //     );
    //
    //     Vector3 topRight = new Vector3(
    //         center.x + Mathf.Cos(topRightAngle) * dist,
    //         center.y + Mathf.Sin(topRightAngle) * dist,
    //         0
    //     );
    //
    //     Vector3 botLeft = new Vector3(
    //         center.x + Mathf.Cos(botLeftAngle) * dist,
    //         center.y + Mathf.Sin(botLeftAngle) * dist,
    //         0
    //     );
    //
    //     Vector3 botRight = new Vector3(
    //         center.x + Mathf.Cos(botRightAngle) * dist,
    //         center.y + Mathf.Sin(botRightAngle) * dist,
    //         0
    //     );
    //
    //     // Adding vertices + triangles
    //     int startIndex = vertices.Count; // Using vertices.Count to get the correct index
    //     vertices.AddRange(new Vector3[] { botLeft, botRight, topRight, topLeft });
    //
    //     triangles.AddRange(new int[]
    //     {
    //         startIndex, startIndex + 1, startIndex + 2, // First triangle
    //         startIndex, startIndex + 2, startIndex + 3  // Second triangle
    //     });
    // }
    //
    // #endregion
    //
    //
    // #region Corner Helper
    //
    // //Corner
    // private void CreateCorner(List<Vector3> vertices, List<int> triangles, Node node, RoadDetails roadDetails, float roadWidth, int curveSmoothness, float extraEdge)
    // {
    //     if (curveSmoothness <= 0) return;
    //     
    //     float radius = _gridManager.NodeRadius - roadWidth/2f - 0.0467f; // -0.05f convert circluar node radius to rectangle
    //     float outerRadius = roadWidth + radius;
    //     
    //     Tuple<int, int> angles = GetCornerAngle(roadDetails);
    //     float startAngle = angles.Item1 - 13f;
    //     float endAngle = angles.Item2 + 13f;
    //
    //     Vector2 center = GetInnerCenter(roadDetails, node, extraEdge);
    //     
    //     float angleStep = (endAngle - startAngle) / (curveSmoothness);
    //     
    //     // Add the curve vertices for the inner arc
    //     List<int> arcVertices = new List<int>();
    //     for (int i = 0; i <= curveSmoothness; i++)
    //     {
    //         float angle = Mathf.Deg2Rad * (endAngle - i * angleStep);
    //         float x = Mathf.Cos(angle) * radius;
    //         float y = Mathf.Sin(angle) * radius;
    //     
    //         vertices.Add(new Vector3(center.x + x, center.y + y, 0));
    //         arcVertices.Add(vertices.Count - 1); // Store indices of the arc vertices
    //     }
    //
    //     // Add vertices for the outer boundary (larger arc)
    //     List<int> outerVertices = new List<int>();
    //     for (int i = 0; i <= curveSmoothness; i++)
    //     {
    //         float angle = Mathf.Deg2Rad * (endAngle - i * angleStep);
    //         float x = Mathf.Cos(angle) * outerRadius;
    //         float y = Mathf.Sin(angle) * outerRadius;
    //     
    //         vertices.Add(new Vector3(center.x + x, center.y + y, 0));
    //         outerVertices.Add(vertices.Count - 1); // Store indices of the outer arc vertices
    //     }
    //
    //     // Create triangles between the inner arc and outer boundary
    //     for (int i = 0; i < curveSmoothness; i++)
    //     {
    //         // First triangle
    //         triangles.Add(arcVertices[i]);       // Inner arc vertex
    //         triangles.Add(outerVertices[i]);     // Corresponding outer boundary vertex
    //         triangles.Add(arcVertices[i + 1]);   // Next inner arc vertex
    //
    //         // Second triangle
    //         triangles.Add(outerVertices[i]);     // Outer boundary vertex
    //         triangles.Add(outerVertices[i + 1]); // Next outer boundary vertex
    //         triangles.Add(arcVertices[i + 1]);   // Inner arc vertex
    //     }
    // }
    // private Vector3 GetInnerCenter(RoadDetails roadDetails, Node node, float extraEdge)
    // {
    //     float halfWidth = roadWidth / 2f;
    //     
    //     
    //     switch (roadDetails.Direction)
    //     {
    //         case DirectionType.TopLeft:
    //             return new Vector3(node.WorldPosition.x - halfWidth * extraEdge, node.WorldPosition.y - halfWidth * extraEdge, 0);
    //         
    //         case DirectionType.TopRight:
    //             return new Vector3(node.WorldPosition.x + halfWidth * extraEdge, node.WorldPosition.y -halfWidth * extraEdge, 0);
    //         
    //         case DirectionType.BottomLeft:
    //             return new Vector3(node.WorldPosition.x - halfWidth  * extraEdge, node.WorldPosition.y + halfWidth * extraEdge, 0);
    //         
    //         case DirectionType.BottomRight:
    //             return new Vector3(node.WorldPosition.x + halfWidth * extraEdge, node.WorldPosition.y + halfWidth * extraEdge, 0);
    //     }
    //     return Vector3.zero;
    // }
    // private void AddOppositeCorner(Node node, DirectionType directionType,List<Vector3> vertices, List<int> triangles ,int curveSmoothness, float halfWidth, float extraPer, float oppositeCornerSmoothness)
    // {
    //     Tuple<Vector2, Vector3> centerData = GetOppositeInnerCenter(directionType, node,  halfWidth, extraPer);
    //     Tuple<int, int> angleData = GetOppositeAngle(directionType);
    //     
    //     float distance = Vector3.Distance(centerData.Item1, centerData.Item2);
    //
    //     
    //     //Center:
    //     Vector2 center = centerData.Item1;
    //     Vector3 triangleOrigin = centerData.Item2;
    //     
    //     //Angle:
    //     float startAngle = angleData.Item1; 
    //     float endAngle = angleData.Item2;
    //     
    //     float radius = distance - oppositeCornerSmoothness;
    //    float angleStep = (endAngle - startAngle) / curveSmoothness;
    //     
    //     int centerIndex = vertices.Count;
    //     vertices.Add(triangleOrigin); //This is center vertices from create opposite circular smooth
    //    
    //     int startIndex = vertices.Count;
    //     
    //     // Add the curve vertices for the inner arc
    //     for (int i = 0; i <= curveSmoothness; i++)
    //     {
    //         float angle = Mathf.Deg2Rad * (endAngle - i * angleStep);
    //         float x = Mathf.Cos(angle) * radius;
    //         float y = Mathf.Sin(angle) * radius;
    //         
    //         vertices.Add(new Vector3(center.x + x, center.y + y, 0)); 
    //     }
    //     
    //     for (int i = startIndex; i < vertices.Count -1  ; i++)
    //     {
    //         triangles.AddRange(new int[ ] { centerIndex, i, i + 1 });
    //     }
    //    
    // }
    //
    // /// <summary>
    // /// Return Vector2: center, Vector3 = origin triangle (vertices)
    // /// </summary>
    // /// <param name="direction"></param>
    // /// <param name="node"></param>
    // /// <param name="extraPer"></param>
    // /// <returns></returns>
    // private Tuple<Vector2,Vector3>GetOppositeInnerCenter(DirectionType direction, Node node,float halfWidth, float extraPer = 0.35f)
    // {
    //     float extraEdge = 1 + extraPer;
    //
    //     switch (direction)
    //     {
    //         case DirectionType.TopLeft:
    //             Vector2 topLeftCenter = new Vector2(node.WorldPosition.x - _gridManager.NodeRadius * extraEdge, node.WorldPosition.y + _gridManager.NodeRadius* extraEdge);
    //             Vector3 topLeftTriangleOr = new Vector3(node.WorldPosition.x - halfWidth, node.WorldPosition.y + halfWidth, 0);
    //             return new Tuple<Vector2, Vector3>(topLeftCenter, topLeftTriangleOr);
    //         
    //         case DirectionType.TopRight:
    //             Vector2 topRightCenter = new Vector2(node.WorldPosition.x + _gridManager.NodeRadius * extraEdge, node.WorldPosition.y + _gridManager.NodeRadius * extraEdge);
    //             Vector3 topRightTriangleOr = new Vector3(node.WorldPosition.x + halfWidth, node.WorldPosition.y + halfWidth, 0);
    //             return new Tuple<Vector2, Vector3>(topRightCenter, topRightTriangleOr);
    //         
    //         case DirectionType.BottomRight:     
    //             Vector2 bottomRightCenter = new Vector2(node.WorldPosition.x + _gridManager.NodeRadius * extraEdge, node.WorldPosition.y - _gridManager.NodeRadius * extraEdge);
    //             Vector3 bottomRightTriangleOr = new Vector3(node.WorldPosition.x + halfWidth, node.WorldPosition.y - halfWidth, 0);
    //             return new Tuple<Vector2, Vector3>(bottomRightCenter, bottomRightTriangleOr);
    //         
    //         case DirectionType.BottomLeft:
    //             Vector2 bottomLeftCenter = new Vector2(node.WorldPosition.x - _gridManager.NodeRadius * extraEdge, node.WorldPosition.y - _gridManager.NodeRadius * extraEdge);
    //             Vector3 bottomLeftTriangleOr = new Vector3(node.WorldPosition.x - halfWidth, node.WorldPosition.y - halfWidth, 0);
    //             return new Tuple<Vector2, Vector3>(bottomLeftCenter, bottomLeftTriangleOr);
    //     }
    //     return new Tuple<Vector2, Vector3>(Vector2.zero, Vector3.zero);
    // }
    //
    // /// <summary>
    // /// Return Tuple<startAngle, endAngle>();
    // /// </summary>
    // /// <param name="direction"></param>
    // /// <returns></returns>
    // private Tuple<int, int> GetOppositeAngle(DirectionType direction)
    // {
    //    
    //     switch (direction)
    //     {
    //         case DirectionType.TopLeft:
    //             return new Tuple<int, int>(270, 360);
    //         case DirectionType.TopRight:
    //             return new Tuple<int, int>(180, 270);
    //         case DirectionType.BottomLeft:
    //             return new Tuple<int, int>(0, 90);
    //         case DirectionType.BottomRight:
    //             return new Tuple<int, int>(90, 180);
    //     }
    //     return new Tuple<int, int>(0,0);
    // }
    //
    // private Tuple<int,int> GetCornerAngle(RoadDetails roadDetails)
    // {
    //     //Tuple<startAngle, endAngle>
    //     switch (roadDetails.Direction)
    //     {
    //         case DirectionType.TopLeft:
    //             return new Tuple<int,int>(0,90);
    //         
    //         case DirectionType.TopRight:
    //             return new Tuple<int,int>(90,180);
    //         
    //         case DirectionType.BottomRight:
    //             return new Tuple<int, int>(180, 270);
    //         
    //         case DirectionType.BottomLeft:
    //             return new Tuple<int,int>(270,360 );
    //     }
    //     return new Tuple<int, int>(0, 0);
    // }
    //
    // #endregion
    //
    //
    // #region TJunction Helper
    //
    // /// <summary>
    // /// The center mesh => extends , the perdencular mesh => create new
    // /// </summary>
    // private void AddTJunctionMesh(Node node, List<Vector3> vertices, List<int> triangles, RoadDetails roadDetails ,float oppositeCornerSmoothness,float extraPer, int curveSmoothness, float halfWidth)
    // {
    //     float extraEdge = 1f + extraPer;
    //         
    //     switch (roadDetails.Direction)
    //     {
    //         case DirectionType.Left:
    //             //Add rectangle:
    //             AddRectangleMesh(vertices, triangles, DirectionType.Up, node.WorldPosition,roadWidth,1,1,extraEdge,extraEdge);
    //             AddRectangleMesh(vertices, triangles , DirectionType.Left, node.WorldPosition, roadWidth,extraEdge,0,1,1);
    //             
    //             //Add 2 opposite corners:
    //             AddOppositeCorner(node, DirectionType.TopLeft, vertices, triangles, curveSmoothness, halfWidth, extraPer,oppositeCornerSmoothness);
    //             AddOppositeCorner(node, DirectionType.BottomLeft, vertices, triangles, curveSmoothness, halfWidth, extraPer,oppositeCornerSmoothness);
    //             break;
    //         
    //         case DirectionType.Right:
    //             //Add rectangle:
    //             AddRectangleMesh(vertices, triangles, DirectionType.Up, node.WorldPosition,roadWidth,1,1,extraEdge,extraEdge);
    //             AddRectangleMesh(vertices, triangles , DirectionType.Right, node.WorldPosition, roadWidth,0,extraEdge, 1, 1);
    //
    //             //Add 2 opposite corners:
    //             AddOppositeCorner(node, DirectionType.TopRight, vertices, triangles, curveSmoothness, halfWidth, extraPer, oppositeCornerSmoothness);
    //             AddOppositeCorner(node, DirectionType.BottomRight, vertices, triangles, curveSmoothness, halfWidth, extraPer, oppositeCornerSmoothness);
    //             break;
    //         
    //         case DirectionType.Up:
    //             //Add rectangle:
    //             AddRectangleMesh(vertices, triangles , DirectionType.Right, node.WorldPosition, roadWidth,extraEdge,extraEdge,1, 1);
    //             AddRectangleMesh(vertices, triangles, DirectionType.Up, node.WorldPosition,roadWidth,1,1,extraEdge,0);
    //
    //             //Add 2 opposite corners:
    //             AddOppositeCorner(node, DirectionType.TopLeft, vertices, triangles, curveSmoothness, halfWidth, extraPer, oppositeCornerSmoothness);
    //             AddOppositeCorner(node, DirectionType.TopRight, vertices, triangles, curveSmoothness, halfWidth, extraPer,oppositeCornerSmoothness);
    //             break;
    //         
    //         case DirectionType.Down:
    //             //Add rectangle:
    //             AddRectangleMesh(vertices, triangles, DirectionType.Right, node.WorldPosition,roadWidth,extraEdge,extraEdge,1,1);
    //             AddRectangleMesh(vertices, triangles , DirectionType.Down, node.WorldPosition, roadWidth,1,1,0, extraEdge);
    //             
    //             //Add 2 opposite corners:
    //             Debug.Log("Opposite smoothness " + oppositeCornerSmoothness);
    //             AddOppositeCorner(node, DirectionType.BottomRight, vertices, triangles, curveSmoothness, halfWidth, extraPer, oppositeCornerSmoothness);
    //             AddOppositeCorner(node, DirectionType.BottomLeft, vertices, triangles, curveSmoothness, halfWidth, extraPer, oppositeCornerSmoothness);
    //             break;
    //     }
    // }
    // #endregion
    //
    //
    // #region Basic Helper
    // private void UpdateMesh(Mesh mesh, Vector3[] vertices, int[] triangles)
    // {
    //     if (vertices == null || vertices.Length == 0)
    //     {
    //         return;
    //     }
    //
    //     if (triangles == null || triangles.Length == 0)
    //     {
    //         return;
    //     }
    //
    //     mesh.Clear(); // Clear the mesh before updating
    //     
    //     // Assign vertices and triangles
    //     mesh.vertices = vertices;
    //     mesh.triangles = triangles;
    //
    //     // Optional: Recalculate normals for proper lighting if needed
    //     mesh.RecalculateBounds();
    //     mesh.RecalculateNormals();
    // }
    // //Always call first, create first before any vertices
    // private void AddRectangleMesh(List<Vector3> vertices, List<int> triangles, DirectionType direction, Vector2 pivot, float roadWidth = 0.5f, float leftScale = 1f , float rightScale = 1f, float upScale = 1f, float downScale = 1f)
    // {
    //    
    //     List<Vector3> rectangleVertices = CreateRectangleVertices(direction, pivot, roadWidth, leftScale, rightScale, upScale, downScale);
    //
    //     int startIndex = vertices.Count; // Get the starting index of the new vertices
    //     
    //     vertices.AddRange(rectangleVertices);
    //
    //     // Update the triangle indices based on the starting index
    //     triangles.AddRange(new int[] { 
    //         startIndex, startIndex + 2, startIndex + 1, // Reversed order
    //         startIndex, startIndex + 3, startIndex + 2 
    //     });
    // }
    //
    // //Scale from 0->1
    // private List<Vector3> CreateRectangleVertices(DirectionType direction, Vector2 pivot, float roadWidth = 0.5f,float leftScale = 1f , float rightScale = 1f, float upScale = 1f, float downScale = 1f)
    // {
    //     List<Vector3> rectangleVertices = new List<Vector3>();
    //     float halfWidth = roadWidth / 2;
    //     //Horizontal:
    //     if (direction == DirectionType.Left || direction == DirectionType.Right)
    //     {
    //         rectangleVertices.Add(new Vector3(pivot.x - _gridManager.NodeRadius * leftScale, pivot.y - halfWidth * downScale, 0));//Bottom left
    //         rectangleVertices.Add( new Vector3(pivot.x + _gridManager.NodeRadius * rightScale, pivot.y - halfWidth * downScale, 0));//Bottom right
    //         rectangleVertices.Add(new Vector3(pivot.x + _gridManager.NodeRadius * rightScale, pivot.y + halfWidth * upScale, 0)); //Top right
    //         rectangleVertices.Add(new Vector3(pivot.x - _gridManager.NodeRadius * leftScale, pivot.y + halfWidth * upScale, 0));// Top left
    //
    //     }
    //     //Vertical:
    //     else if (direction == DirectionType.Up || direction == DirectionType.Down)
    //     {
    //         rectangleVertices.Add(new Vector3(pivot.x - halfWidth * leftScale, pivot.y - _gridManager.NodeRadius * downScale, 0));
    //         rectangleVertices.Add(new Vector3(pivot.x + halfWidth * rightScale,pivot.y - _gridManager.NodeRadius * downScale, 0));
    //         rectangleVertices.Add(new Vector3(pivot.x + halfWidth * rightScale, pivot.y + _gridManager.NodeRadius * upScale, 0));
    //         rectangleVertices.Add(new Vector3(pivot.x - halfWidth * leftScale, pivot.y + _gridManager.NodeRadius * upScale, 0)); 
    //     }
    //     return rectangleVertices;
    // }
    // private void AddCurveMesh(List<Vector3> vertices,List<int> triangles, Vector3 center, int curveSmoothness, float radius, Tuple<int,int> angles)
    // {
    //     if (curveSmoothness <= 0) return;
    //     int startAngle = angles.Item1;
    //     int endAngle = angles.Item2;
    //     float angleStep = (endAngle - startAngle) / curveSmoothness;
    //     
    //     vertices.Add(center);
    //     int centerIndex = vertices.Count - 1;
    //
    //     for (int i = 0; i <= curveSmoothness; i++)
    //     {
    //         float angle = Mathf.Deg2Rad * (endAngle - i * angleStep);
    //         float x = Mathf.Cos(angle) * radius;
    //         float y = Mathf.Sin(angle) * radius;
    //         
    //         vertices.Add(new Vector3(center.x + x, center.y + y, 0));
    //     }
    //     
    //     int startIndex = centerIndex + 1;
    //     for (int i = startIndex; i <= startIndex + curveSmoothness - 1; i++)
    //     {
    //         triangles.Add(centerIndex);  // Center point
    //         triangles.Add(i);            // Current vertex
    //         triangles.Add(i + 1);        // Next vertex
    //     }
    // }
    //
    // #endregion
    // }
    //
    //
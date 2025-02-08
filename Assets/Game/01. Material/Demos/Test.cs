// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Game._00.Script._07._Mesh_Generator;
// using Unity.Mathematics;
// using Unity.VisualScripting;
// using UnityEngine;
// using Random = UnityEngine.Random;
//
// public class Test : MonoBehaviour
// {
//     private GridManager _gridManager;
//     private Mesh mesh;
//     private MeshRenderer meshRenderer;
//     private MeshFilter meshFilter;
//
//     private List<Vector3> vertices;
//     private List<int> triangles;
//     
//     [SerializeField] public List<GameObject> buildingPrefabs = new List<GameObject>();
//     public float radius = 2f;
//
//     private List<GameObject> spawnedBuilding  = new List<GameObject>();
//     private int[] waveNumb = new int[4] { 1, 1, 1, 1 };
//
//     public float nodeBoundary = 2f;
//
//     [SerializeField]public float roadLength = 1f;
//     List<Vector3> spawnedPositiion = new List<Vector3>();
//     private void Awake()
//     {
//         mesh = new Mesh();
//         meshRenderer = GetComponent<MeshRenderer>();
//         meshFilter = GetComponent<MeshFilter>();
//
//         // Initialize the lists
//         vertices = new List<Vector3>();
//         triangles = new List<int>();
//     }
//
//     private void Start()
//     {
//         // bool isOc = IsPositionOccupied(Vector3.zero);   
//         // Debug.Log(isOc);
//         // CreateMesh();
//         // UpdateMesh();
//         // StartCoroutine(Spawn());
//         // CreateBlood(30, 10);
//         
//         mesh = CreateDiagionalMesh(_gridManager.NodeFromWorldPosition(new Vector2(-0.5f, -0.5f)), 45f, 1 / 4f, vertices, triangles,0.5f);
//         meshFilter.mesh = mesh; 
//     }
//     private Mesh CreateDiagionalMesh(OriginBuildingNode node, float angle, float extraPer,  List<Vector3> vertices, List<int> triangles, float roadWidth = 0.5f)
//     {
//         // Variables:
//         Mesh diagionalMesh = new Mesh();
//         float halfWidth = roadWidth / 2;    
//         float smoothProportion = extraPer;
//         float extraEdge = 1 + smoothProportion; //Proportion
//         float radius = smoothProportion * roadWidth;
//         
//         //Add
//         AddDiagonalPoints(node.WorldPosition, halfWidth, angle, vertices, triangles);
//         //Update mesh:
//         UpdateMesh(diagionalMesh, vertices.ToArray(), triangles.ToArray());
//         return diagionalMesh;
//         
//     }  
//     private void AddDiagonalPoints(Vector3 center, float halfWidth, float angle, List<Vector3> vertices, List<int> triangles)
//     {
//         // Distance from the center to the corners of the node
//         float dist = Mathf.Sqrt(Mathf.Pow(halfWidth, 2) + Mathf.Pow(_gridManager.NodeRadius, 2));
//
//         // Angle from the center to the top-left corner
//         float angleOffset = Mathf.Atan(halfWidth / dist);  // Same angle offset for symmetry
//
//         // Calculate angles for each point based on the input angle
//         float topLeftAngle = angle * Mathf.Deg2Rad + angleOffset;
//         float topRightAngle = angle * Mathf.Deg2Rad - angleOffset;
//         float botLeftAngle = angle * Mathf.Deg2Rad + (Mathf.PI - angleOffset);  // PI added to go to the bottom
//         float botRightAngle = angle * Mathf.Deg2Rad + (Mathf.PI + angleOffset); // PI added and adjusted for bottom right
//
//         // Calculate positions of the four corners
//         Vector3 topLeft = new Vector3(
//             center.x + Mathf.Cos(topLeftAngle) * dist,
//             center.y + Mathf.Sin(topLeftAngle) * dist,
//             0
//         );
//
//         Vector3 topRight = new Vector3(
//             center.x + Mathf.Cos(topRightAngle) * dist,
//             center.y + Mathf.Sin(topRightAngle) * dist,
//             0
//         );
//
//         Vector3 botLeft = new Vector3(
//             center.x + Mathf.Cos(botLeftAngle) * dist,
//             center.y + Mathf.Sin(botLeftAngle) * dist,
//             0
//         );
//
//         Vector3 botRight = new Vector3(
//             center.x + Mathf.Cos(botRightAngle) * dist,
//             center.y + Mathf.Sin(botRightAngle) * dist,
//             0
//         );
//
//         // Adding vertices + triangles
//         int startIndex = vertices.Count; // Using vertices.Count to get the correct index
//         vertices.AddRange(new Vector3[] { botLeft, botRight, topRight, topLeft });
//     
//         triangles.AddRange(new int[]
//         {
//             startIndex, startIndex + 1, startIndex + 2, // First triangle
//             startIndex, startIndex + 2, startIndex + 3  // Second triangle
//         });
//         Debug.Log("adsad");
//     }
//     
//     
//     private void UpdateMesh(Mesh mesh, Vector3[] vertices, int[] triangles)
//     {
//         if (vertices == null || vertices.Length == 0)
//         {
//             return;
//         }
//
//         if (triangles == null || triangles.Length == 0)
//         {
//             return;
//         }
//
//         Debug.Log("Mesh");
//         mesh.Clear(); // Clear the mesh before updating
//
//         // Assign vertices and triangles
//         mesh.vertices = vertices;
//         mesh.triangles = triangles;
//
//         // Optional: Recalculate normals for proper lighting if needed
//         mesh.RecalculateBounds();
//         mesh.RecalculateNormals();
//     }
//     
//     
//
//
//
//     private void CreateBuilding(int currentRoad, float currentZoneRadius)
//     {
//         Vector3 firstPos = Random.insideUnitCircle * currentZoneRadius;
//         SpawnObject(buildingPrefabs[0], firstPos);
//         float totalLength = currentRoad * roadLength;
//         
//         for (int i = 1; i <buildingPrefabs.Count; i++)
//         {
//             int maxAttempt = 100;
//             int attempt = 0;
//             bool isFoundPos = false;
//             Vector3 spawnedPos;
//             float maxLength = Random.Range(nodeBoundary + 1f, totalLength / 2f);
//             do
//             {
//                 spawnedPos = Random.insideUnitCircle * currentZoneRadius;
//                 float dst = Vector3.Distance(spawnedPositiion[i - 1], spawnedPos);
//                 if (dst > nodeBoundary + 1f && dst <= maxLength)
//                 {
//                     totalLength -= dst;
//                     SpawnObject(buildingPrefabs[i], spawnedPos);
//                     isFoundPos = true;
//                 }
//                 attempt++;
//             } while (!isFoundPos && attempt < maxAttempt);
//
//         }
//        
//
//     }
//
//     private void SpawnObject(GameObject gameObject, Vector3 position)
//     {
//         GameObject spawnedObject = Instantiate(gameObject);
//         spawnedObject.transform.position = position;
//         spawnedPositiion.Add(position);
//     }
//
//
//
//     private void OnDrawGizmos()
//     {
//         Gizmos.DrawWireSphere(Vector2.zero, 10f);
//     }
//
//     #region Mesh
//         private bool IsPositionOccupied(Vector2 position)
//     {
//         float radius = 0.7f + 0.5f;
//         Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius);
//         return colliders.Length > 0; 
//     }
//
//
//     private void CreateMesh()
//     {
//         // CornerSmoothBottomLeft(Vector2.zero, vertices, triangles, 20, 1, 0.5f, 1 / 4f);
//         // CreateRectangleA(Vector3.zero, vertices, triangles, 0.25f);
//         
//         // //Cross road:
//         // Vector2 corner1 = new Vector2(-0.25f, -0.25f);
//         // Vector3 center1 = new Vector3(-0.4f, -0.4f, 0);
//         // CreateCorner(corner1, center1, vertices, triangles, 20, 0.15f, 0 ,90);
//         //
//         // AddRectangleMesh(vertices, triangles, RoadMeshDirection.Down, Vector2.zero);
//         // AddRectangleMesh(vertices, triangles, RoadMeshDirection.Right, Vector2.zero);
//         //
//         // Vector2 corner2 = new Vector2(0.25f, -0.25f);
//         // Vector3 center2 = new Vector3(0.4f, -0.4f, 0);
//         // CreateCorner(corner2, center2, vertices, triangles, 20, 0.15f, 90 , 180);
//         //
//         // Vector3 corner3 = new Vector3(0.25f, 0.25f, 0);
//         // Vector2 center3 = new Vector2(0.4f, 0.4f);
//         // CreateCorner(corner3, center3, vertices, triangles, 20, 0.15f, 180 , 270);
//         //
//         // Vector3 corner4 = new Vector3(-0.25f, 0.25f, 0);
//         // Vector3 center4 = new Vector3(-0.4f, 0.4f, 0);
//         // CreateCorner(corner4, center4, vertices, triangles, 20, 0.15f, 270 , 360);
//         
//         // //T Junction:
//         // AddRectangleMesh(vertices, triangles, RoadMeshDirection.Down, new Vector2(0, 0), 0.5f, 1, 1, 0, 1);
//         // AddRectangleMesh(vertices, triangles, RoadMeshDirection.Right, Vector2.zero, 0.5f, 1, 1, 1, 1);
//         //
//         // Vector2 corner1 = new Vector2(-0.5f, -0.5f);
//         // Vector3 center1 = new Vector3(-0.64f, -0.64f, 0);
//         // CreateCorner(corner1, center1, vertices, triangles, 20, 0.175f, 0 ,90);
//         //
//         // Vector2 corner2 = new Vector2(0.5f, -0.5f);
//         // Vector3 center2 = new Vector3(0.65f, -0.64f, 0);
//         // CreateCorner(corner2, center2, vertices, triangles, 20, 0.1875f, 90, 180);
//
//         CornerSmoothBottomLeft(new Vector2(0, 0), vertices, triangles, 20, 0.5f, 0.5f, 0.1f);
//     }
//
//     private void UpdateMesh()
//     {
//         mesh.Clear();
//         mesh.vertices = vertices.ToArray();
//         mesh.triangles = triangles.ToArray();
//         mesh.RecalculateNormals();
//         mesh.RecalculateBounds();
//     }
//
//     
//     
//     private IEnumerator Spawn()
//     {
//         int currentRoad = 10;
//         int waveCount = 0;
//         float startTime = Time.time;
//         int eachRoad = currentRoad / waveNumb.Length;
//
//         // Loop through all waves
//         while (waveCount < waveNumb.Length)
//         {
//             GameObject building = Instantiate(buildingPrefabs[0]);
//             
//             spawnedBuilding.Add(building);
//             building.transform.position = GetRandomPosition(eachRoad);
//
//             yield return new WaitForSeconds(1f);
//             waveCount++;
//         }
//
//         yield return null;
//
//     }
//
//     private Vector2 GetRandomPosition(int remainRoad)
//     {
//         if (spawnedBuilding.Count < 2)
//         {
//             // Handle edge cases when there are less than two buildings
//             float xOffset = UnityEngine.Random.Range(-9f, 9f);
//             float yOffset = UnityEngine.Random.Range(-5f, 5f);
//             return new Vector2(xOffset, yOffset);
//         }
//         // else
//         // {
//         //     float dist = Vector2.Distance(spawnedBuilding[spawnedBuilding.Count - 1].transform.position, 
//         //         spawnedBuilding[spawnedBuilding.Count - 2].transform.position);
//         //     int roadDistance = Mathf.CeilToInt(dist / roadLength);
//         //     remainRoad -= roadDistance;
//         // }
//         
//         Debug.Log("Remain road: " + remainRoad);
//         float radius = GetRadius(remainRoad);
//
//         int maxAttempt = 10;
//         int curAttempt = 0;
//         Vector2 pos = Vector2.zero;
//         do
//         {
//             float angle = Random.Range(0, 2 * Mathf.PI);
//
//             float x = radius * Mathf.Cos(angle);
//             float y = radius * Mathf.Sin(angle);
//             pos = new Vector2(x + spawnedBuilding[0].transform.position.x, y + spawnedBuilding[0].transform.position.y);
//             curAttempt++;
//             
//             Debug.Log("Occupied: " + !isOccupied(pos));
//             
//         } while (!isOccupied(pos) && curAttempt < maxAttempt);
//
//         return pos;
//
//     }
//
//     private bool isOccupied(Vector2 center)
//     {
//         Collider2D[] colliders = Physics2D.OverlapCircleAll(center, nodeBoundary);
//         return colliders.Length > 0;
//     }
//
//     private float GetRadius(int remainRoad)
//     {        
//         float maxRadius = remainRoad * roadLength;
//         float randomRadius = UnityEngine.Random.Range(nodeBoundary, maxRadius);
//         return randomRadius;
//     }
//     
//     private void AddRectangleMesh(List<Vector3> vertices, List<int> triangles, RoadMeshDirection buildingDirection, Vector2 pivot, float roadWidth = 1f, float leftScale = 1f , float rightScale = 1f, float upScale = 1f, float downScale = 1f)
//     {
//         Debug.Log("Pivot: " + pivot);
//         List<Vector3> rectangleVertices = CreateRectangleVertices(buildingDirection, pivot, roadWidth, _gridManager.NodeRadius,leftScale, rightScale, upScale, downScale);
//
//         int startIndex = vertices.Count; // Get the starting index of the new vertices
//         
//         vertices.AddRange(rectangleVertices);
//
//         // Update the triangle indices based on the starting index
//         triangles.AddRange(new int[] { 
//             startIndex, startIndex + 2, startIndex + 1, // Reversed order
//             startIndex, startIndex + 3, startIndex + 2 
//         });
//     }
//     
//     //Scale from 0->1
//     private List<Vector3> CreateRectangleVertices(RoadMeshDirection buildingDirection, Vector2 pivot, float roadWidth, float nodeRadius,float leftScale = 1f , float rightScale= 1f, float upScale = 1f, float downScale = 1f)
//     {
//         
//
//         List<Vector3> rectangleVertices = new List<Vector3>();
//         float halfWidth = roadWidth / 2f;
//         Debug.Log(halfWidth);
//         
//         //Horizontal:
//         if (buildingDirection == RoadMeshDirection.Left || buildingDirection == RoadMeshDirection.Right)
//         {
//             rectangleVertices.Add(new Vector3(pivot.x - nodeRadius * leftScale, pivot.y - halfWidth * downScale, 0));//Bottom left
//             Debug.Log(new Vector3(pivot.x - nodeRadius * leftScale, pivot.y - halfWidth * downScale, 0));
//             rectangleVertices.Add( new Vector3(pivot.x + nodeRadius * rightScale, pivot.y - halfWidth * downScale, 0));//Bottom right
//             Debug.Log( new Vector3(pivot.x + nodeRadius * rightScale, pivot.y - halfWidth * downScale, 0));
//             rectangleVertices.Add(new Vector3(pivot.x + nodeRadius * rightScale, pivot.y + halfWidth * upScale, 0)); //Top right
//             Debug.Log(new Vector3(pivot.x + nodeRadius * rightScale, pivot.y + halfWidth * upScale, 0));
//             rectangleVertices.Add(new Vector3(pivot.x - nodeRadius * leftScale, pivot.y + halfWidth * upScale, 0));// Top left
//             Debug.Log(new Vector3(pivot.x - nodeRadius * leftScale, pivot.y + halfWidth * upScale, 0));
//         }//Vertical:
//         else if (buildingDirection == RoadMeshDirection.Up || buildingDirection == RoadMeshDirection.Down)
//         {
//
//             rectangleVertices.Add(new Vector3(pivot.x - halfWidth * leftScale, pivot.y - nodeRadius * downScale, 0));
//             Debug.Log(new Vector3(pivot.x - halfWidth * leftScale, pivot.y - nodeRadius * downScale, 0));
//             
//             rectangleVertices.Add(new Vector3(pivot.x + halfWidth * rightScale,pivot.y - nodeRadius * downScale, 0));
//             Debug.Log(new Vector3(pivot.x + halfWidth * rightScale,pivot.y - nodeRadius * downScale, 0));
//
//             rectangleVertices.Add(new Vector3(pivot.x + halfWidth * rightScale, pivot.y + nodeRadius * upScale, 0));
//             Debug.Log(new Vector3(pivot.x + halfWidth * rightScale, pivot.y + nodeRadius * upScale, 0));
//
//             rectangleVertices.Add(new Vector3(pivot.x - halfWidth * leftScale, pivot.y + nodeRadius * upScale, 0));
//             Debug.Log(new Vector3(pivot.x - halfWidth * leftScale, pivot.y + nodeRadius * upScale, 0));
//
//
//         }
//         return rectangleVertices;
//     }
//
//     private void CreateRectangleA(Vector2 worldPosition, List<Vector3> vertices, List<int> triangles, float halfWidth)
//     {
//         Vector3 bottomLeft = new Vector3(worldPosition.x - halfWidth, worldPosition.y - halfWidth, 0);
//         Vector3 bottomRight = new Vector3(worldPosition.x + halfWidth, worldPosition.y - halfWidth, 0);
//         Vector3 upRight = new Vector3(worldPosition.x + halfWidth, worldPosition.y + halfWidth, 0) ;
//         Vector3 upLeft = new Vector3(worldPosition.x - halfWidth, worldPosition.y + halfWidth, 0) ;
//          
//         vertices.Add(bottomLeft); //0;
//         vertices.Add(bottomRight); //1
//         vertices.Add(upRight); //2
//         vertices.Add(upLeft); //3;
//         
//         triangles.AddRange(new int[] { 0,2, 1, 0,3,2 });
//     }
//
//     //Opposite Corner
//    private void CreateCorner(Vector2 WorldPosition, Vector3 center ,List<Vector3> vertices, List<int> triangles, int smoothness, float radius, float startAngle , float endAngle)
//    {
//        float angleStep = (endAngle - startAngle) / smoothness;
//         
//        int centerIndex = vertices.Count;
//        vertices.Add(WorldPosition); //This is center vertices from create opposite circular smooth
//        Debug.Log("Center Index = 0"  + centerIndex);
//        
//        int startIndex = vertices.Count;
//        Debug.Log("Star index = 1 : " + startIndex);
//
//     
//        // Add the curve vertices for the inner arc
//        for (int i = 0; i <= smoothness; i++)
//        {
//            float angle = Mathf.Deg2Rad * (endAngle - i * angleStep);
//            float x = Mathf.Cos(angle) * radius;
//            float y = Mathf.Sin(angle) * radius;
//         
//            vertices.Add(new Vector3(center.x + x, center.y + y, 0)); 
//        }
//        
//        for (int i = startIndex; i < vertices.Count -1  ; i++)
//        {
//            Debug.Log("Triangles: " + centerIndex + " , " + i + " , " + (i + 1));
//            triangles.AddRange(new int[ ] { centerIndex, i, i + 1 });
//        }
//        
//    }
//
//     private void CornerSmoothBottomLeft(Vector2 WorldPosition, List<Vector3> vertices, List<int> triangles, int smoothness, float radius, float halfWidth, float extraEdge)
//     {
//         Vector3 centerLeft_Bot = new Vector3(WorldPosition.x - halfWidth * extraEdge, WorldPosition.y - 0.5f * extraEdge, 0); 
//         
//         Vector3 centerOutside = new Vector3(WorldPosition.x - halfWidth * extraEdge - radius, WorldPosition.y - 0.5f * extraEdge, 0);
//         
//         int centerTriangleLeftIndex_Bot = vertices.Count - 1;
//
//         CreateCurveMesh(vertices, triangles, centerLeft_Bot, radius,  0, 90, smoothness, radius + 2);
//     }
//     
//     //Corner
//     private void CreateCurveMesh(List<Vector3> vertices, List<int> triangles, Vector3 center, float radius, float startAngle, float endAngle, int smoothness, float outerRadius)
//     {
//         if (smoothness <= 0) return;
//     
//         float angleStep = (endAngle - startAngle) / smoothness;
//     
//         // Add the curve vertices for the inner arc
//         List<int> arcVertices = new List<int>();
//         for (int i = 0; i <= smoothness; i++)
//         {
//             float angle = Mathf.Deg2Rad * (endAngle - i * angleStep);
//             float x = Mathf.Cos(angle) * radius;
//             float y = Mathf.Sin(angle) * radius;
//         
//             vertices.Add(new Vector3(center.x + x, center.y + y, 0));
//             arcVertices.Add(vertices.Count - 1); // Store indices of the arc vertices
//         }
//     
//         // Add vertices for the outer boundary (larger arc)
//         List<int> outerVertices = new List<int>();
//         for (int i = 0; i <= smoothness; i++)
//         {
//             float angle = Mathf.Deg2Rad * (endAngle - i * angleStep);
//             float x = Mathf.Cos(angle) * outerRadius;
//             float y = Mathf.Sin(angle) * outerRadius;
//         
//             vertices.Add(new Vector3(center.x + x, center.y + y, 0));
//             outerVertices.Add(vertices.Count - 1); // Store indices of the outer arc vertices
//         }
//     
//         // Now, create triangles between the inner arc and outer boundary
//         for (int i = 0; i < smoothness; i++)
//         {
//             // First triangle
//             triangles.Add(arcVertices[i]);       // Inner arc vertex
//             triangles.Add(outerVertices[i]);     // Corresponding outer boundary vertex
//             triangles.Add(arcVertices[i + 1]);   // Next inner arc vertex
//
//             // Second triangle
//             triangles.Add(outerVertices[i]);     // Outer boundary vertex
//             triangles.Add(outerVertices[i + 1]); // Next outer boundary vertex
//             triangles.Add(arcVertices[i + 1]);   // Inner arc vertex
//         }
//     }
//     #endregion
//
// }
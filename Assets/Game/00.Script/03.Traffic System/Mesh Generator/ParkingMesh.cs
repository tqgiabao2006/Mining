using System.Collections.Generic;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.Mesh_Generator
{
   
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class ParkingMesh:MonoBehaviour
    {
        [SerializeField] private bool _isGizmos;
        [SerializeField] private Material _material;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        
        
        private List<Vector3> _totalVertices;
        private List<int> _totalTriangles;
        private List<Vector2> _totalUvs;
        
        private List<CombineInstance> meshCombineList = new List<CombineInstance>();
        
        private Dictionary<Node, CombineInstance> nodeCombineInstances = new Dictionary<Node, CombineInstance>();

        private void Start()
        {
            Initial();
        }
        private void Initial()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            
            _totalVertices = new List<Vector3>();
            _totalTriangles = new List<int>();
            _totalUvs = new List<Vector2>();
        }

        public void PlaceBuildingMesh(Node node, ParkingLotSize parkingSize, BuildingDirection buildingDirection)
        {
            Mesh generatedMesh =CreateBuildingMesh(node.WorldPosition,parkingSize, buildingDirection);
            _totalVertices.AddRange(generatedMesh.vertices);
            if (generatedMesh != null)
            {
                StoreMeshData(node, generatedMesh);
            }
            ApplyCombinedRoadMesh();
        }
        private void StoreMeshData(Node node, Mesh generatedMesh)
        {
            // Destroy old mesh if it exists to prevent memory leaks
            if (nodeCombineInstances.TryGetValue(node, out CombineInstance oldInstance))
            {
                DestroyImmediate(oldInstance.mesh);
            }

            CombineInstance combineInstance = new CombineInstance
            {
                mesh = generatedMesh
            };
            nodeCombineInstances[node] = combineInstance;
        }
        private void ApplyCombinedRoadMesh()
        {
            // Clear meshCombineList to prevent stacking instances
            meshCombineList.Clear();

            // Rebuild meshCombineList from updated CombineInstances in nodeCombineInstances dictionary
            foreach (var combineInstance in nodeCombineInstances.Values)
            {
                meshCombineList.Add(combineInstance);
            }

            if (meshCombineList.Count > 0)
            {
                Mesh combinedMesh = new Mesh();
                combinedMesh.CombineMeshes(meshCombineList.ToArray(), true, false);
                combinedMesh.RecalculateNormals();
                combinedMesh.RecalculateBounds();

                // Clear the old mesh to ensure it's fully replaced
                if (_meshFilter.mesh != null)
                {
                    _meshFilter.mesh.Clear();
                }

                // Assign the new combined mesh
                _meshFilter.mesh = combinedMesh;
                _meshRenderer.material = _material;
                _meshCollider.sharedMesh = combinedMesh;
            }
        }

   
        private Mesh CreateBuildingMesh(Vector2 position, ParkingLotSize parkingLotSize,BuildingDirection buildingDirection)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();


            if (parkingLotSize == ParkingLotSize._1x1)
            {
                AddSquareMesh(position, vertices, triangles, 1,1,1,1);
            }
            else
            {
                //Base on Node radius
                int maxMultipler = parkingLotSize == ParkingLotSize._2x2 ? 3 : 5;
                
                List<int> GetMultipler(BuildingDirection dir)
                {
                    return dir switch
                    {
                        BuildingDirection.Up => new List<int> { 3, 1, 1, maxMultipler},
                        BuildingDirection.Down => new List<int> { 3, 1, maxMultipler, 1 },
                        BuildingDirection.Right => new List<int> { 1, maxMultipler, 1, 3 },
                        BuildingDirection.Left => new List<int> { maxMultipler, 1, 1, 3 },
                        _ => new List<int>()
                    };
                }
                
                List<int> meshScales = GetMultipler(buildingDirection);
                AddSquareMesh(position, vertices, triangles,meshScales[0] ,meshScales[1],meshScales[2],meshScales[3]);
            }
            
            UpdateMesh(mesh,vertices.ToArray(), triangles.ToArray());
            return mesh;
        }
        
        
       
        
        private void UpdateMesh(Mesh mesh, Vector3[] vertices, int[] triangles)
        {
            if (vertices == null || vertices.Length == 0)
            {
                return;
            }

            if (triangles == null || triangles.Length == 0)
            {
                return;
            }

            mesh.Clear(); // Clear the mesh before updating
        
            // Assign vertices and triangles
            mesh.vertices = vertices;
            mesh.triangles = triangles;

            // Optional: Recalculate normals for proper lighting if needed
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }
        
        
        private void AddSquareMesh(Vector2 pivot, List<Vector3> vertices, List<int> triangles, float leftScale = 1, float rightScale = 1, float downScale = 1, float upScale = 1) 
        {
            List<Vector3> rectangleVertices = CreateSquareVertices(pivot, leftScale, rightScale, downScale, upScale);

            int startIndex = vertices.Count; // Get the starting index of the new vertices
        
            vertices.AddRange(rectangleVertices);

            // Update the triangle indices based on the starting index
            triangles.AddRange(new int[] { 
                startIndex, startIndex + 2, startIndex + 1, // Reversed order
                startIndex, startIndex + 3, startIndex + 2 
            });
        }
        
        
       /// <summary>
       /// Scales have to be positivee
       /// </summary>
       /// <param name="pivot"></param>
       /// <param name="leftScale"></param>
       /// <param name="rightScale"></param>
       /// <param name="downScale"></param>
       /// <param name="upScale"></param>
       /// <returns></returns>
        private List<Vector3> CreateSquareVertices(Vector2 pivot, float leftScale = 1, float rightScale = 1, float downScale = 1, float upScale = 1 )
        {
            List<Vector3> rectangleVertices = new List<Vector3>();
            rectangleVertices.Add(new Vector3(pivot.x -  GridManager.NodeRadius * leftScale, pivot.y - GridManager.NodeRadius*downScale, 0));//Bottom left
            rectangleVertices.Add( new Vector3(pivot.x + GridManager.NodeRadius * rightScale, pivot.y -  GridManager.NodeRadius*downScale , 0));//Bottom right
            rectangleVertices.Add(new Vector3(pivot.x + GridManager.NodeRadius * rightScale, pivot.y +  GridManager.NodeRadius * upScale, 0)); //Top right
            rectangleVertices.Add(new Vector3(pivot.x -  GridManager.NodeRadius * leftScale, pivot.y +  GridManager.NodeRadius * upScale, 0));// Top left
            return rectangleVertices;
        }

        private void OnDrawGizmos()
        {
            if (_totalVertices == null || !_isGizmos) return;
            Gizmos.color = Color.red;
            foreach (Vector2 vertex in _totalVertices)
            {
                Gizmos.DrawSphere(vertex, 0.2f);
            }
            
        }
    }
    
}
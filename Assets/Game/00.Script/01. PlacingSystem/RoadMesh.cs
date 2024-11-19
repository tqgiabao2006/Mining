using System;
using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Game._00.Script._05._Manager;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine.Serialization;


public enum DirectionType
{
    Right,
    Left,
    Up,
    Down
}

[Flags]
public enum Direction
{  
    //Rotate clockwise
    None = 1<<0,
    Up = 1<<1,
    UpRight= 1<<2,
    Right = 1<<3,
    BottomRight = 1<<4,
    Bottom = 1<<5,
    BottomLeft = 1<<6,
    Left = 1<<7,
    UpLeft = 1<<8,
}
[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class RoadMesh : MonoBehaviour
{
    #region Variables:

    [Header("Debugging")] 
    [SerializeField] private bool isGizmos = false; 
    private List<Vector3> curVertices = new List<Vector3>();
        
    [Header("Mesh Setting")]
    [SerializeField] public float roadWidth = 0.3f;
    [SerializeField] public Material roadMaterial;
    [SerializeField] public int curveSmoothness = 40;
    
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private Material _material;
    private GameManager _gameManager;
    private RoadManager _roadManager;
    private Grid _grid;
    
    private List<Vector3> _vertices;
    private List<int> _triangles; 
    private List<Vector2> _uvs;
    
    private List<CombineInstance> meshCombineList = new List<CombineInstance>();
        
    private Dictionary<Node, CombineInstance> nodeCombineInstances = new Dictionary<Node, CombineInstance>();
    

    #endregion

    private void Start()
    {
        Initial();
    }

    private void Initial()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
        
        _gameManager = GameManager.Instance;    
        _grid = _gameManager.Grid;
        _roadManager = _gameManager.RoadManager;
            
        // Initialize lists for the mesh data
        _vertices = new List<Vector3>();
        _triangles = new List<int>();
        _uvs = new List<Vector2>();
    }
    
    /// <summary>
    /// Only call in building phase
    /// </summary>
    /// <param name="node"></param>
    /// <param name="isPlacing"></param>
    public void ChangeRoadMesh(Node node)
    {
        List<Node> neighbors = _roadManager.GetRoadList(node);
        Direction bakedDirection = GetBakedDirection(node.WorldPosition, neighbors);
        Mesh generatedMesh = CreateMesh(node.WorldPosition, bakedDirection);
        curVertices.AddRange(generatedMesh.vertices);
        if (generatedMesh != null)
        {
            StoreMeshData(node, generatedMesh);
        }
    }
        
    private void StoreMeshData(Node node, Mesh generatedMesh)
    {
        node.SetRoad(true);

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

    public void ApplyCombinedRoadMesh()
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
            _meshRenderer.material = roadMaterial;
            _meshCollider.sharedMesh = combinedMesh;
        }
    }
    
    private Mesh CreateMesh(Vector2 nodePos, Direction bakedDirection)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        CreateMainMesh(nodePos,vertices, triangles, bakedDirection);
        
        //Create corner base on the different angle
        List<(int startIndex, int endIndex, int angleDst)> distances = CalculateBitwiseDistances(bakedDirection);
        for (int i = 0; i < distances.Count; i++)
        {
            float angle = 45 * distances[i].angleDst;
            CreateCorner(nodePos, vertices, triangles, angle, distances[i].startIndex, distances[i].endIndex, distances[i].angleDst);
        }
        UpdateMesh(mesh, vertices.ToArray(), triangles.ToArray());
        UpdateWalkable(nodePos);
        return mesh;
    }

    private void UpdateWalkable(Vector2 nodePos)
    {
        _grid.UpdateWalkable(nodePos);
    }

    private Direction GetBakedDirection(Vector2 nodePos, List<Node> neighbourNodes)
    {
        Direction bakedDirection = 0;
        List<Vector2> neighbours = new List<Vector2>();
        foreach (Node node in neighbourNodes)
        {
            neighbours.Add(node.WorldPosition);  
        }
        
        foreach (Vector2 neighbour in neighbours)
        {
            float xDiff = neighbour.x - nodePos.x;
            float yDiff = neighbour.y - nodePos.y;

            if (xDiff == 0 && yDiff == 0) continue;

            // Horizontal and vertical directions
            if (xDiff < 0 && yDiff == 0) bakedDirection |= Direction.Left;
            if (xDiff > 0 && yDiff == 0) bakedDirection |= Direction.Right;
            if (xDiff == 0 && yDiff < 0) bakedDirection |= Direction.Bottom;
            if (xDiff == 0 && yDiff > 0) bakedDirection |= Direction.Up;

            // Diagonal directions
            if (xDiff < 0 && yDiff < 0) bakedDirection |= Direction.BottomLeft;
            if (xDiff > 0 && yDiff < 0) bakedDirection |= Direction.BottomRight;
            if (xDiff < 0 && yDiff > 0) bakedDirection |= Direction.UpLeft;
            if (xDiff > 0 && yDiff > 0) bakedDirection |= Direction.UpRight;
        }
        return bakedDirection;
    }
  
    private List<(int startIndex, int endIndex, int angleDst)> CalculateBitwiseDistances(Direction bakedDirection)
    {
        List<(int startIndex, int endIndex, int angleDst)> results = new List<(int, int, int)>();
        int number = (int)bakedDirection;
        int startIndex = -1;
        int firstIndex = 0;
        int rawDst = 0;
        int angleDst = 0;
        int curIndex = 0;
        int enumLength = (Enum.GetValues(typeof(Direction)).Length - 1); //Avoid Direction.None

        
        for (; number > 0; number >>= 1, curIndex++)
        {
            if ((number & 1) == 1)
            {
                if (startIndex == -1)
                {
                    startIndex = curIndex;
                    firstIndex = curIndex;
                }else
                {
                    
                    angleDst = curIndex - startIndex;
                    results.Add((startIndex, curIndex, angleDst));
                    startIndex = curIndex;
                }
            }
        }
        
        //Add the angle between last startIndex to first startIndex make it a circle
        firstIndex += enumLength;
        angleDst = firstIndex - startIndex;
        results.Add((startIndex, firstIndex,  angleDst));

        return results;
    }

    #region SubMesh
    private void AddOppositeCorner(Vector2 nodePos,List<Vector3> vertices, List<int> triangles, float startAngle, float endAngle, float diagonalAngle ,int curveSmoothness)
    {
        float halfWidth = roadWidth/ 2f;
        Vector3 triangleOrigin = Vector3.zero;
        Vector3 center = Vector3.zero;
        float radius = 0;
        if (diagonalAngle == 45)
        {        
           //Vertical line => x = nodePos.x + halfWidth;
           //Diagonal line => y = x - nodePos.x + nodePos.y + halfwidth* sqrt(2)
            float midPoint = _grid.NodeRadius - halfWidth * 2.414f;
            center = new Vector3(nodePos.x  + halfWidth+ midPoint/2f, nodePos.y + _grid.NodeRadius, 0.0f);
            triangleOrigin = new Vector3(nodePos.x + halfWidth, 2.4f*halfWidth+nodePos.y); 
            radius = midPoint/2f;
        }
        else if(diagonalAngle == 90)
        {
            float extraEdge = halfWidth * 1.5f;
            center = new Vector3(nodePos.x + extraEdge, nodePos.y + extraEdge, 0.0f);
            triangleOrigin = new Vector3(nodePos.x + halfWidth, nodePos.y + halfWidth, 0);
            radius = extraEdge - halfWidth;
        }else if (diagonalAngle == 135)
        {
            //Vertical line => x = nodePos.x + halfWidth;
            //Diagonal line => y = -x + nodePos.x + nodePos.y + halfwidth * sqrt(2)
            triangleOrigin = new Vector3(nodePos.x + halfWidth, 0.4f * halfWidth + nodePos.y, 0);
            center = new Vector3(triangleOrigin.x + 0.05f, triangleOrigin.y + 0.02f,0);
            radius = 0.050f; //Sqrt(0.05^2 + 0.02^2) (dst from center and triangle origin)- 0.004f;

        }
        
        float angleStep = (endAngle - startAngle) / curveSmoothness;
        
        int centerIndex = vertices.Count;
        vertices.Add(triangleOrigin); //This is center vertices from create opposite circular smooth
       
        int startIndex = vertices.Count;
        
        // Add the curve vertices for the inner arc
        for (int i = 0; i <= curveSmoothness; i++)
        {
            float angle = Mathf.Deg2Rad * (endAngle - i * angleStep);
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            
            vertices.Add(new Vector3(center.x + x, center.y + y, 0)); 
        }
        
        for (int i = startIndex; i < vertices.Count -1  ; i++)
        {
            triangles.AddRange(new int[ ] { centerIndex, i, i + 1 });
        }
    }
    private void AddBottomCorner(Vector2 nodePos, List<Vector3> vertices, List<int> triangles, float startAngle, float endAngle,int curveSmoothness)
    {
        Vector3 center = nodePos;
        float radius = roadWidth/ 2f; //= haflWidth
        int centerIndex = vertices.Count;
        vertices.Add(center);
        
        float angleStep = (endAngle - startAngle) / curveSmoothness;
       
        int startIndex = vertices.Count;
        for (int i = 0; i <= curveSmoothness; i++)
        {
            float angle = Mathf.Deg2Rad * (endAngle - i * angleStep);
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            
            vertices.Add(new Vector3(center.x + x, center.y + y, 0)); 
        }
        for (int i = startIndex; i < vertices.Count -1  ; i++)
        {
            triangles.AddRange(new int[ ] { centerIndex, i, i + 1 });
        }
    }
    
    private void CreateCorner(Vector2 nodePos, List<Vector3> vertices, List<int> triangles, float angle, int startIndex, int endIndex, int angleDst)
    {
        //Calculate for rotation
        int startRotateIndex = vertices.Count;
        float rotateAngle = 45 * (startIndex - 1);
        if (angle > 135)
        {
            AddBottomCorner(nodePos, vertices, triangles, 0, 360, 40);
            return;
        }
        
        if (angle == 45) //Initiali pattern: Up => Up Right (1->2)
        {
            AddOppositeCorner(nodePos, vertices, triangles, 180, 300 ,45, curveSmoothness);
        }else if (angle == 90) 
        {
            AddOppositeCorner(nodePos, vertices, triangles, 180, 270 ,90, curveSmoothness);
        }else if (angle == 135) //Initiali pattern: Up => BottomRight (1-4)
        {
            AddOppositeCorner(nodePos, vertices, triangles,  180, 220, 135, curveSmoothness);
        }
        RotateVertices(nodePos,startRotateIndex, rotateAngle ,vertices);

    }
    private void CreateMainMesh(Vector2 nodePos, List<Vector3> vertices, List<int> triangles,Direction bakedDirection)
    {
        //Read direction bitwise:
        if ((bakedDirection & Direction.Up) == Direction.Up)
        {
            AddRectangleMesh(vertices, triangles, DirectionType.Up, nodePos,  roadWidth, 1,1,1,0);
        }
        if ((bakedDirection & Direction.Right) == Direction.Right)
        {
            AddRectangleMesh(vertices, triangles, DirectionType.Right, nodePos, roadWidth, 0,1,1,1);
        }
        if ((bakedDirection & Direction.Bottom) == Direction.Bottom)
        {
            AddRectangleMesh(vertices, triangles, DirectionType.Down, nodePos, roadWidth, 1,1, 0,1);
        }
        if ((bakedDirection & Direction.Left) == Direction.Left)
        {
            AddRectangleMesh(vertices, triangles, DirectionType.Left, nodePos, roadWidth, 1,0, 1,1);
        }
        
        if ((bakedDirection & Direction.BottomRight) == Direction.BottomRight)
        {
            int startIndex = vertices.Count;
            AddRectangleMesh(vertices, triangles, DirectionType.Up, nodePos,  roadWidth, 1, 1, 1.44f, 0);
            RotateVertices(nodePos, startIndex,135, vertices);
        }
        
        if ((bakedDirection & Direction.UpRight) == Direction.UpRight)
        {
            int startIndex = vertices.Count;  
            AddRectangleMesh(vertices, triangles, DirectionType.Up, nodePos,  roadWidth, 1, 1, 1.44f, 0); //Scale = sqrt(2)
            RotateVertices(nodePos, startIndex,45, vertices);
        }
        
        if ((bakedDirection & Direction.BottomLeft) == Direction.BottomLeft)
        {
            int startIndex = vertices.Count;  
            AddRectangleMesh(vertices, triangles, DirectionType.Up, nodePos,  roadWidth, 1, 1, 1.44f, 0);
            RotateVertices(nodePos,startIndex ,225, vertices);
        }
        
        if ((bakedDirection & Direction.UpLeft) == Direction.UpLeft)
        {
            int startIndex = vertices.Count;  
            AddRectangleMesh(vertices, triangles, DirectionType.Up, nodePos,  roadWidth, 1, 1, 1.44f, 0);
            RotateVertices(nodePos, startIndex,315, vertices);
        }
    }
    
    private void RotateVertices(Vector2 nodeOrigin, int startIndex,float angle, List<Vector3> vertices)
    {
        //Linear algebra
        for (int i = startIndex; i < vertices.Count; i++)
        {
            float newX = vertices[i].x - nodeOrigin.x;
            float newY = vertices[i].y - nodeOrigin.y;
            
            float x = newX * Mathf.Cos(angle * Mathf.Deg2Rad) + newY*Mathf.Sin(angle * Mathf.Deg2Rad);
            float y = -newX * Mathf.Sin(angle*Mathf.Deg2Rad) + newY*Mathf.Cos(angle * Mathf.Deg2Rad);
            
            float finalX = x + nodeOrigin.x;
            float finalY = y + nodeOrigin.y; //Slightly different
            
            vertices[i] = new Vector3(finalX, finalY, 0);
        }
    }
    
     private void AddRectangleMesh(List<Vector3> vertices, List<int> triangles, DirectionType direction, Vector2 pivot, float roadWidth, float leftScale = 1f , float rightScale = 1f, float upScale = 1f, float downScale = 1f)
    {
        List<Vector3> rectangleVertices = CreateRectangleVertices(direction, pivot, roadWidth, leftScale, rightScale, upScale, downScale);

        int startIndex = vertices.Count; // Get the starting index of the new vertices
        
        vertices.AddRange(rectangleVertices);

        // Update the triangle indices based on the starting index
        triangles.AddRange(new int[] { 
            startIndex, startIndex + 2, startIndex + 1, // Reversed order
            startIndex, startIndex + 3, startIndex + 2 
        });
    }
    
    //Scale from 0->1
    private List<Vector3> CreateRectangleVertices(DirectionType direction, Vector2 pivot, float roadWidth,float leftScale = 1f , float rightScale = 1f, float upScale = 1f, float downScale = 1f)
    {
        List<Vector3> rectangleVertices = new List<Vector3>();
        float halfWidth = roadWidth / 2;
        //Horizontal:
        if (direction == DirectionType.Left || direction == DirectionType.Right)
        {
            rectangleVertices.Add(new Vector3(pivot.x - _grid.NodeRadius * leftScale, pivot.y - halfWidth * downScale, 0));//Bottom left
            rectangleVertices.Add( new Vector3(pivot.x + _grid.NodeRadius* rightScale, pivot.y - halfWidth * downScale, 0));//Bottom right
            rectangleVertices.Add(new Vector3(pivot.x + _grid.NodeRadius* rightScale, pivot.y + halfWidth * upScale, 0)); //Top right
            rectangleVertices.Add(new Vector3(pivot.x - _grid.NodeRadius * leftScale, pivot.y + halfWidth * upScale, 0));// Top left

        }
        //Vertical:
        else if (direction == DirectionType.Up || direction == DirectionType.Down)
        {
            rectangleVertices.Add(new Vector3(pivot.x - halfWidth * leftScale, pivot.y - _grid.NodeRadius * downScale, 0));
            rectangleVertices.Add(new Vector3(pivot.x + halfWidth * rightScale,pivot.y - _grid.NodeRadius * downScale, 0));
            rectangleVertices.Add(new Vector3(pivot.x + halfWidth * rightScale, pivot.y + _grid.NodeRadius * upScale, 0));
            rectangleVertices.Add(new Vector3(pivot.x - halfWidth * leftScale, pivot.y + _grid.NodeRadius * upScale, 0)); 
        }
        return rectangleVertices;
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
    #endregion

    
    //---------------TESTING----------------//
    private void OnDrawGizmos()
    {
        if (!isGizmos || curVertices.Count == 0) return;
        foreach (Vector2 pos in curVertices)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(new Vector2(pos.x,pos.y), 0.01f);
        }
    }
    //---------------TESTING----------------//

    
}
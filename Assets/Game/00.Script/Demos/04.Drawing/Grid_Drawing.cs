// using Game._00.Script._00._Manager.Grid_setting;
// using Game._00.Script._06._Grid_setting;
// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Serialization;
//
// [RequireComponent(typeof(GridManager))] 
// public class Grid_Drawing : MonoBehaviour
// {
//     // Drawing:
//     private static readonly int Cull = Shader.PropertyToID("_Cull");
//     private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
//     private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
//     private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
//
//     // GridManager:
//     public GridManager GridManager { get; private set; }
//     [FormerlySerializedAs("_gridColor")] 
//     [SerializeField] private Color gridColor;
//     private int _gridSizeX;
//     private int _gridSizeY;
//     private float _nodeDiameter;
//     private int _rows;
//     private int _cols; 
//
//     // Material:
//     private Material _lineMaterial;
//     private SpriteRenderer _spriteRenderer;
//
//     private void Awake()
//     {
//         InitializeGrid();
//         CreateLineMaterial();  // Move material creation here
//         InitializeRenderer();
//     }
//
//     private void InitializeGrid()
//     {
//         // GridManager initialization:
//         GridManager = GetComponent<GridManager>();
//         _gridSizeX = GridManager.GridSizeX;
//         _gridSizeY = GridManager.GridSizeY;
//         _nodeDiameter = GridManager.NodeDiameter; 
//         _rows = Mathf.RoundToInt(_gridSizeY / _nodeDiameter);
//         _cols = Mathf.RoundToInt(_gridSizeX / _nodeDiameter);
//     }
//
//     private void InitializeRenderer()
//     {
//         // Get child SpriteRenderer
//         _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
//         print("Initialized SpriteRenderer: " + _spriteRenderer);
//         if (_spriteRenderer != null)
//         {
//             // Assign the line material to the SpriteRenderer
//             _spriteRenderer.material = _lineMaterial;  // Ensure _lineMaterial is created before this
//             print("Assigned Material: " + _lineMaterial);
//         }
//     }
//
//     private void CreateLineMaterial()
//     {
//         if (!_lineMaterial)
//         {
//             // Use a sprite-compatible shader instead
//             Shader shader = Shader.Find("Sprites/Default");
//             _lineMaterial = new Material(shader)
//             {
//                 hideFlags = HideFlags.HideAndDontSave
//             };
//             _lineMaterial.color = gridColor; // Set the color of the material
//         }
//     }
//
//     private void OnRenderObject()
//     {
//         // Ensure we set the pass for drawing the _gridManager lines
//         _lineMaterial.SetPass(0);
//         DrawGrid(_rows, _cols);
//     }
//
//     private void DrawGrid(int rows, int cols)
//     {
//         // Draw the squares first
//         for (int i = 0; i < rows; i++)
//         {
//             for (int j = 0; j < cols; j++)
//             {
//                 DrawSquare(j * _nodeDiameter, i * _nodeDiameter);
//             }
//         }
//
//         // Then draw the _gridManager lines
//         GL.Begin(GL.LINES);
//         GL.Color(Color.white); // Color for the _gridManager lines
//
//         // Horizontal lines
//         for (int i = 0; i <= rows; i++)
//         {
//             GL.Vertex3(0, i * _nodeDiameter, 0);
//             GL.Vertex3(cols * _nodeDiameter, i * _nodeDiameter, 0);
//         }
//
//         // Vertical lines
//         for (int j = 0; j <= cols; j++)
//         {
//             GL.Vertex3(j * _nodeDiameter, 0, 0);
//             GL.Vertex3(j * _nodeDiameter, rows * _nodeDiameter, 0);
//         }
//
//         GL.End(); // End drawing lines
//     }
//
//     private void DrawSquare(float x, float y)
//     {
//         GL.Begin(GL.QUADS); // Begin drawing quads (squares)
//         GL.Color(gridColor); // Color for the square
//
//         // Define the vertices for the square
//         GL.Vertex3(x, y, 0); // Bottom left
//         GL.Vertex3(x + _nodeDiameter, y, 0); // Bottom right
//         GL.Vertex3(x + _nodeDiameter, y + _nodeDiameter, 0); // Top right
//         GL.Vertex3(x, y + _nodeDiameter, 0); // Top left
//
//         GL.End(); // End drawing quads
//     }
//
// }

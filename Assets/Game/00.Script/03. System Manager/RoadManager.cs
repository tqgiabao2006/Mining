using System;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;

public class RoadManager : SubjectBase, IObserver
{
    [SerializeField] private bool isGizmos = false;
    private List<Node> _nodeList;
    private Dictionary<int, List<int>> _adjList;
   
    private Dictionary<int, List<Node>> _graphList; //delete set to -1;
    private int _graphCount;
        
    private RoadMesh _roadMesh;
    private GameStateManager _gameStateManager;
    private BuildingManager _buildingManager; 
    
    
    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        _roadMesh = FindObjectOfType<RoadMesh>();
        _gameStateManager = GameManager.Instance.GameStateManager;
        _buildingManager = GameManager.Instance.BuildingManager;    
        
        _nodeList = new List<Node>();
        _adjList = new Dictionary<int, List<int>>();
        _graphList = new Dictionary<int, List<Node>>();
        
        _graphCount = 0;
        ObserversSetup();
    }
    


    /// <summary>
    /// Check the placed node repersent buildingBase node => to check if buildingBase is connected by road later
    /// </summary>
    /// <param name="node"></param>
    /// <param name="buildingType"></param>
    public void PlaceNode(Node node, BuildingBase buildingBase)
    {
        if (!_nodeList.Contains(node))
        {
            SetNodeIndex(node);
            _nodeList.Add(node);
        }

        if (buildingBase != null)
        {
            //Notify buildingBase manager to check
            // Notify((CheckConnectionDelegate, buildingBase), NotificationFlags.CheckingConnection);
        }
    }

    /// <summary>
    /// Get graph list for pathfinding
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public List<Node> GetGraphList(Node node)
    {
        return _graphList[node.GraphIndex];
    }

    /// <summary>
    /// Check if a building is connected to one of its outputs
    /// Return: (bool isConnected)
    /// </summary>
    private Func<List<BuildingBase>, BuildingBase, BuildingBase> CheckConnectionDelegate = (outputBuilding, mainBuilding) =>
    {
        Node mainNode = mainBuilding.Node;
        bool isConnected = false;
        float closestDistance = float.MaxValue;
        BuildingBase closestBuilding = null;
        
        List<BuildingBase> connectedBuildings = new List<BuildingBase>();
        foreach (BuildingBase building in outputBuilding)
        {
            if (building.Node.GraphIndex == mainNode.GraphIndex)
            {
                connectedBuildings.Add(building);
            }
        }

        //Get closest building to main building
        if (connectedBuildings.Count > 0)
        {
            foreach (BuildingBase building in connectedBuildings)
            {
                float distance = Vector3.Distance(mainBuilding.transform.position, building.transform.position);
            
                // Check if this building is the closest one
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBuilding = building;
                }
            }
        }
        return closestBuilding;
    };
    
    #region Graph helper
    /// <summary>
    /// 
    /// </summary>
    /// <param name="node"></param>
    private void SetGraphIndex(ref Node node)
    {
        // Return early if the node is not in the adjacency list
        if (!_adjList.ContainsKey(node.NodeIndex)) return;

        int nodeGraphIndex = node.GraphIndex;
        foreach (int adjNodeIndex in _adjList[node.NodeIndex])
        {
            Node adjNode = _nodeList[adjNodeIndex];
            int adjGraphIndex = adjNode.GraphIndex;

            if (nodeGraphIndex == -1 && adjGraphIndex == -1)
            {
                node.GraphIndex = _graphCount;
                _graphList.Add(node.GraphIndex, new List<Node>() { node });
                _graphCount++;
            }

            if (nodeGraphIndex != adjGraphIndex)
            {
                if (nodeGraphIndex == -1 && adjGraphIndex > -1)
                {
                    node.GraphIndex = adjGraphIndex;
                    _graphList[adjGraphIndex].Add(node);
                }
                else if (nodeGraphIndex > -1 && adjGraphIndex > -1)
                {
                    // Case 2.1: Current node is unassigned or has a higher graph index
                    if ((nodeGraphIndex > adjGraphIndex))
                    {
                        AssignNodeToGraph(node, adjNode, adjGraphIndex, nodeGraphIndex);

                    }
                    // Case 2.2: Adjacent node is unassigned or has a higher graph index
                    else if ((adjGraphIndex > nodeGraphIndex))
                    {
                        AssignNodeToGraph(adjNode, node, nodeGraphIndex, adjGraphIndex);
                    }
                }
                
                BuildingBase nullBuildingBase = null;
                NotifySpecific((CheckConnectionDelegate, nullBuildingBase), NotificationFlags.CheckingConnection, _buildingManager);

            }
        }
    }

    private void AssignNodeToGraph(Node targetNode, Node sourceNode, int sourceGraphIndex, int targetGraphIndex)
    {
        targetNode.GraphIndex = sourceGraphIndex;
        _graphList[sourceGraphIndex].Add(targetNode);

        if (targetGraphIndex > sourceGraphIndex)
        {
            // Update all node references to the new graph index
            foreach (Node n in _graphList[targetGraphIndex])
            {
                n.GraphIndex = sourceGraphIndex;
            }

            // Merge and reassign reference
            _graphList[sourceGraphIndex].AddRange(_graphList[targetGraphIndex]);
            _graphList[targetGraphIndex] = new List<Node>(); // Reset the old list
        }
    }

    #endregion
    
    #region Place road helper
    
    private void EnsureNodeInAdjList(int nodeIndex)
    {
        if (!_adjList.ContainsKey(nodeIndex))
        {
            _adjList[nodeIndex] = new List<int>();
        }
    }
    
    public void SetAdjList(Node curNode, Node nextNode)
    {
    
        EnsureNodeInAdjList(curNode.NodeIndex);
        EnsureNodeInAdjList(nextNode.NodeIndex);
        
        _adjList[curNode.NodeIndex].Add(nextNode.NodeIndex);
        
        _adjList[nextNode.NodeIndex].Add(curNode.NodeIndex);
        
        SetGraphIndex(ref curNode);
        SetGraphIndex(ref nextNode);
        
        UpdateMeshForConnection(curNode);
        UpdateMeshForConnection(nextNode);
    }

    public List<Node> GetNodeInAdjList(Node node)
    {
        List<Node> adjNodes = new List<Node>();
        List<int> adjIndexes = _adjList[node.NodeIndex];
        foreach (int i in adjIndexes)
        {
            adjNodes.Add(_nodeList[i]);
        }
        return adjNodes;
        
    }

    private void SetNodeIndex(Node node)
    {
        node.NodeIndex = _nodeList.Count;
    }

    public List<Node> GetRoadList(Node node)
    {
        List<Node> affectedRoads = new List<Node>();

        foreach (int connectedNodeIndex in _adjList[node.NodeIndex])
        {
            affectedRoads.Add(_nodeList[connectedNodeIndex]);
        }

        return affectedRoads;
    }

    private void UpdateMeshForConnection(Node node)
    {
        // Change mesh for main node
        _roadMesh.ChangeRoadMesh(node);
        _roadMesh.ApplyCombinedRoadMesh();

        // Update all connected nodes' meshes
        foreach (int connectedNodeIndex in _adjList[node.NodeIndex])
        {
            Node connectedNode = _nodeList[connectedNodeIndex];
            _roadMesh.ChangeRoadMesh(connectedNode);
        }

        _roadMesh.ApplyCombinedRoadMesh();
    }

    public void CreateMesh(Node node)
    {
        UpdateMeshForConnection(node);
    }
    
    #endregion

    private void OnDrawGizmos()
    {
        if (!isGizmos || _nodeList == null) return;
        foreach (Node n in _nodeList)
        {
            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 40;
            style.normal.textColor = Color.red;
            Handles.Label(n.WorldPosition, n.GraphIndex.ToString(), style);
            Gizmos.color = Color.green;
            Gizmos.DrawCube(n.WorldPosition, Vector3.one * 0.2f);
        }
        
    }
    
    #region Observers
    
    /// <summary>
    /// Check isConnected => bool + closest building
    /// WHEN: end place Node check all|| spawn new building => check specific || merge a line => che
    /// </summary>
    /// <param name="data"></param>
    public void OnNotified(object data, string flag)
    {
        if (flag != NotificationFlags.CheckingConnection)
        {
            return;
        }
        if (data is bool && (bool)data)
        {
            //Notify buildingBase manager to check
            BuildingBase nullBuildingBase = null; //Check all buildings
            Notify((CheckConnectionDelegate, nullBuildingBase), NotificationFlags.CheckingConnection);
        }
    }
    #endregion

    public override void ObserversSetup()
    {
        _observers.Add(_buildingManager);
        Attach(_buildingManager);
    }
}

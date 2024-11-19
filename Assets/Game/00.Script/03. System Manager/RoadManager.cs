using System;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using UnityEngine;
using UnityEditor;

public class RoadManager : MonoBehaviour
{
    [SerializeField] private bool isGizmos = false;
    private List<Node> _nodeList;
    private Dictionary<int, List<int>> _adjList;
    private Dictionary<BuildingType, List<int>> _buildingDictionary;
    
    private RoadMesh _roadMesh;
    
    
    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        _roadMesh = FindObjectOfType<RoadMesh>();
        _nodeList = new List<Node>();
        _buildingDictionary = new Dictionary<BuildingType, List<int>>();
        _adjList = new Dictionary<int, List<int>>();
    }

    
    /// <summary>
    /// Check the placed node repersent building node => to check if building is connected by road later
    /// </summary>
    /// <param name="node"></param>
    /// <param name="building"></param>
    public void PlaceNode(Node node, BuildingType buildingType)
    {
        if (!_nodeList.Contains(node))
        {
            SetNodeIndex(node);
            _nodeList.Add(node);
        }

        if (buildingType != BuildingType.None)
        {
            if (_buildingDictionary.ContainsKey(buildingType))
            {
                _buildingDictionary[buildingType].Add(node.NodeIndex);
            }
            else
            {
                _buildingDictionary.Add(buildingType, new List<int>());
            }
        }
    }

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

        UpdateMeshForConnection(curNode);
        UpdateMeshForConnection(nextNode);
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
    
    
    /// <summary>
    /// Check if 2 building connected
    /// </summary>
    /// <param name="buildingList"></param> output list of a particular building
    /// <returns> bool isConnected, int startBuilding, to building</returns>
    public bool AreBuildingConnected(List<int> buildingList, Dictionary<int, List<int>> adjList)
    {
        var uf = new UnionFind();

        // Initialize Union-Find for all buildings in the list
        foreach (var building in buildingList)
        {
            uf.AddBuilding(building);
        }

        // Perform Union operations based on the adjacency list (connections)
        List <Tuple<int,int>> processedConnections = new List<Tuple<int,int>>();
       
        foreach (var building in adjList)
        {
            int building1 = building.Key;
            foreach (int building2 in building.Value)
            {
                //Avoid (1-2), (2-1) is set while it is the same
                var connection = building1 < building2
                    ? Tuple.Create(building1, building2)
                    : Tuple.Create(building2, building1);
                if (!processedConnections.Contains(connection))
                {
                    uf.Union(building1, building2);
                    processedConnections.Add(connection);
                }
            }
        }

        // Check if the buildings are connected
        int firstBuilding = buildingList[0];
        int secondBuilding = buildingList[1];

        return uf.Find(firstBuilding) == uf.Find(secondBuilding);
    }
    
    private void OnDrawGizmos()
    {
        if (!isGizmos) return;
        if (_nodeList != null && _nodeList.Count > 0)
        {
            foreach (Node n in _nodeList)
            {
                GUIStyle style = new GUIStyle();
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 40;
                style.normal.textColor = Color.red;
                Handles.Label(n.WorldPosition, n.NodeIndex.ToString(), style);
                Gizmos.color = Color.green;
                Gizmos.DrawCube(n.WorldPosition, Vector3.one * 0.2f);
            }
        }
    }
}

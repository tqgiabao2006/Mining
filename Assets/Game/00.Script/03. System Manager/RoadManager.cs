using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RoadManager : MonoBehaviour
{
    [SerializeField] private bool isGizmos = false;
    public List<Node> NodeList { get; private set; }
    private RoadMesh _roadMesh;
    public Dictionary<int, List<int>> AdjList { get; private set; }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        _roadMesh = FindObjectOfType<RoadMesh>();
        NodeList = new List<Node>();
        AdjList = new Dictionary<int, List<int>>();
    }

    public void PlaceNode(Node node)
    { 
        if (!NodeList.Contains(node))
        {
            SetNodeIndex(node);
            NodeList.Add(node);
        }
    }

    private void EnsureNodeInAdjList(int nodeIndex)
    {
        if (!AdjList.ContainsKey(nodeIndex))
        {
            AdjList[nodeIndex] = new List<int>();
        }
    }

    public void SetAdjList(Node curNode, Node nextNode)
    {
        EnsureNodeInAdjList(curNode.NodeIndex);
        EnsureNodeInAdjList(nextNode.NodeIndex);

        AdjList[curNode.NodeIndex].Add(nextNode.NodeIndex);
        AdjList[nextNode.NodeIndex].Add(curNode.NodeIndex);

        UpdateMeshForConnection(curNode);
        UpdateMeshForConnection(nextNode);
    } 

    private void SetNodeIndex(Node node)
    {
        node.NodeIndex = NodeList.Count;
    }

    public List<Node> GetRoadList(Node node)
    {
        List<Node> affectedRoads = new List<Node>();

        foreach (int connectedNodeIndex in AdjList[node.NodeIndex])
        {
            affectedRoads.Add(NodeList[connectedNodeIndex]);
        }
        return affectedRoads;
    }

    private void UpdateMeshForConnection(Node node)
    {
        // Change mesh for main node
        _roadMesh.ChangeRoadMesh(node);
        _roadMesh.ApplyCombinedRoadMesh();

        // Update all connected nodes' meshes
        foreach (int connectedNodeIndex in AdjList[node.NodeIndex])
        {
            Node connectedNode = NodeList[connectedNodeIndex];
            _roadMesh.ChangeRoadMesh(connectedNode);
        }

        _roadMesh.ApplyCombinedRoadMesh();
    }

    public void CreateMesh(Node node)
    {
        UpdateMeshForConnection(node);
    }
    
    private void OnDrawGizmos()
    {
        if (!isGizmos) return;
        if (NodeList != null && NodeList.Count > 0)
        {
            foreach (Node n in NodeList)
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

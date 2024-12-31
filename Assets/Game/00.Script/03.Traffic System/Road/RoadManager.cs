using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._03.Traffic_System.Building;
using Game._00.Script._03.Traffic_System.Mesh_Generator;
using Game._00.Script._02.Grid_setting;
using UnityEditor;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.Road
{
    public class RoadManager : SubjectBase, IObserver
    {
        public static readonly float RoadWidth = 0.4f;
        [SerializeField] private bool isGizmos = false;
        private List<Node> _nodeList;
        private Dictionary<int, List<int>> _adjList;
   
        private Dictionary<int, List<Node>> _graphList; //delete set to -1;
        private int _graphCount;
        
        private RoadMesh _roadMesh;
        private BuildingManager _buildingManager; 
    
        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _roadMesh = FindObjectOfType<RoadMesh>();
            _buildingManager = FindObjectOfType<BuildingManager>();    
        
            _nodeList = new List<Node>();
            _adjList = new Dictionary<int, List<int>>();
            _graphList = new Dictionary<int, List<Node>>();
        
            _graphCount = 0;
            ObserversSetup();
        }
    


        /// <summary>
        /// Check the placed node repersent building node => to check if building is connected by road later
        /// </summary>
        /// <param name="node"></param>
        /// <param name="buildingType"></param>
        public void PlaceNode(Node node)
        { 
            if (!_nodeList.Contains(node))
            {
                SetNodeIndex(node);
                _nodeList.Add(node);
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
        private readonly Func<List<Node>, Node, Node> CheckConnectionDelegate = (outputNode, startNode) =>
        {
            float closestDistance = float.MaxValue;
            Node closestOutputNode = null;
        
            List<Node> connectedNodes = new List<Node>();
            foreach (Node node in outputNode)
            {
                if (node.GraphIndex == startNode.GraphIndex && node.WorldPosition != startNode.WorldPosition)
                {
                    connectedNodes.Add(node);
                }
            }

            if (connectedNodes.Count == 1)
            {
                return connectedNodes[0];
            }
       
            //Get closest building to main building
            if (connectedNodes.Count > 1)
            {
                foreach (Node building in connectedNodes)
                {
                    float distance = Vector3.Distance(startNode.WorldPosition, building.WorldPosition);
            
                    // Check if this building is the closest one
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestOutputNode = building;
                    }
                }
            }
            return closestOutputNode;
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

            if (curNode.CanDraw)
            {
                UpdateMeshForConnection(curNode);
            }

            if (nextNode.CanDraw)
            {
                UpdateMeshForConnection(nextNode);
            }
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
            // Update all connected nodes' meshes
            foreach (int connectedNodeIndex in _adjList[node.NodeIndex])
            {
                Node connectedNode = _nodeList[connectedNodeIndex];
                if (connectedNode.CanDraw)
                {
                    _roadMesh.ChangeRoadMesh(connectedNode);
                }
            }
        }

        public void CreateMesh(Node node)
        {
            UpdateMeshForConnection(node);
        }

        /// <summary>
        /// Used if need to spawn special road, no matter surrounding objects
        /// </summary>
        /// <param name="node"></param>
        public void CreateMesh(Node node, BitwiseDirection bitwiseDirection)
        {
            _roadMesh.ChangeRoadMesh(node, bitwiseDirection);
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
        /// WHEN: end place OriginBuildingNode check all|| spawn new building => check specific || merge a line => che
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
                Notify(CheckConnectionDelegate, NotificationFlags.CheckingConnection);
            }
        }
        #endregion

        public override void ObserversSetup()
        {
            _observers.Add(_buildingManager);
            Attach(_buildingManager);
        }
    }
}

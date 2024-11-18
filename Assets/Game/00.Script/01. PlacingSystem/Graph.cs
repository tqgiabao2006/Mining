using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    private List<Node> nodesList;
    public List<Node> NodesList
    {
        get { return nodesList; }
        set { nodesList = value; }
    }
    
    private int graphIndex = -1;
    public int GraphIndex
    {
        get { return graphIndex; }
        set { graphIndex = value; }
    }
    
    public Graph(int graphIndex)
    {
        this.nodesList = new List<Node>();
        this.graphIndex = graphIndex;
    }
}

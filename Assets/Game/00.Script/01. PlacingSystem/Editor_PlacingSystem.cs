using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlacingSystem))]
public class Editor_PlacingSystem : Editor
{
    PlacingSystem placingSystem;

    private void OnEnable()
    {
        placingSystem = (PlacingSystem)target;

    }
    private void OnSceneGUI()
    {
        
        Draw();
    }

    private void Draw()
    {
        
        // if (placingSystem == null || placingSystem.NodeList == null || placingSystem.NodeDictionary == null || !placingSystem.showHandles) return;
        // List<Node> nodes = placingSystem.NodeList;
        // Dictionary<int, List<Node>> dictNodes = placingSystem.NodeDictionary;
        // for (int i = 0; i < nodes.Count; i++)
        // {
        //     Handles.color = placingSystem.handleColor;
        //     Handles.SphereHandleCap(0, nodes[i].worldPosition, Quaternion.identity, placingSystem.handlesSize, EventType.Repaint);
        // }
    }
}
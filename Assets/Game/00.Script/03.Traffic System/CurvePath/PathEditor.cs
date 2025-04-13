using UnityEditor;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.CurvePath
{
    [CustomEditor(typeof(PathCreator))]
    public class PathEditor: Editor
    {
        private PathCreator _creator;
        
        private CurvePath Path
        {
            get
            {
                return _creator.Path;
            }
        }

        // private const float _selectThreshold = .1f;
        private int _selectedSegmentIndex = -1;

        private void OnEnable()
        {
            _creator = (PathCreator)target;
        
            if (Path == null)
            {
                _creator.CreatePath();
            }
        }

        // public override void OnInspectorGUI()
        // {
        //     base.OnInspectorGUI();
        //     
        //   
        //
        //     bool isClosed = GUILayout.Toggle(Path.IsClosed, "Closed");
        //     if (isClosed != Path.IsClosed)
        //     {
        //         Undo.RecordObject(_creator, "Close Path");
        //         Path.IsClosed = isClosed;
        //     }
        //     
        //     bool autoset = GUILayout.Toggle(Path.AutoSet, "Auto Set");
        //     if (autoset != Path.AutoSet)
        //     {
        //         Undo.RecordObject(_creator, "Auto Set");
        //         Path.AutoSet = autoset;
        //     }
        //
        //     if (EditorGUI.EndChangeCheck())
        //     { 
        //         SceneView.RepaintAll();
        //     }
        // }

        private void OnSceneGUI()
        {
            if (Path == null)
            {
                return;
            }
            Draw();
            // Input();
        }

        private void Draw()
        {
            if (Path == null)
            {
                return;
            }

            for (int i = 0; i < Path.NumbPoints; i++)
            {
                Handles.Label(Path[i] + Vector2.one * 0.2f, i.ToString() );
            }
            
            Handles.color = Color.yellow;
            for (int i = 0; i < Path.NumbSegs; i++)
            {
                Vector2[] points = Path.GetPointOnSegment(i);

                if (_creator.displayControl)
                {
                    Handles.DrawLine(points[0], points[1], 2);
                    Handles.DrawLine(points[2], points[3],2);
                }
                Color color = i == _selectedSegmentIndex && Event.current.shift ? _creator.selectedCol : _creator.segmentCol;
                Handles.DrawBezier(points[0], points[3], points[1], points[2], color, null, _creator.segmentWidth);
            }
            
            
            //Point
            for (int i = 0; i < Path.NumbPoints; i++)
            {
                Handles.color = i % 3 == 0 ? _creator.anchorCol : _creator.controlCol;
                float size = i % 3 == 0 ? _creator.anchorSize : _creator.controlSize;
                // Handles.DrawSolidDisc(Path[i], Vector3.forward, size);
                Vector2 newPos = Handles.FreeMoveHandle(Path[i], size, Vector3.zero, Handles.CylinderHandleCap);
                
                if (newPos != Path[i])
                {
                    Undo.RecordObject(_creator, "Move point");
                    Path.MovePoint(i, newPos);
                }
            }
        }

        // private void Input()
        // {
        //     Event guiEvent = Event.current;
        //     Vector2 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;
        //
        //     if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
        //     {
        //         if (_selectedSegmentIndex != -1)
        //         {
        //             Undo.RecordObject(_creator, "Split segment");
        //             Path.SplitSegment(mousePos,_selectedSegmentIndex);
        //         }
        //         else if(!Path.IsClosed)
        //         {
        //             Undo.RecordObject(_creator, "Add Segment");
        //             Path.AddSegment(mousePos);
        //         }
        //     }
        //
        //     if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
        //     {
        //         float minDst = float.MaxValue;
        //         int closestAnchorIndex = -1;
        //         
        //         for (int i = 0; i < Path.NumbPoints; i+=3)
        //         {
        //             float dst = Vector2.Distance(Path[i], mousePos);
        //             if (dst < minDst)
        //             {
        //                 minDst = dst;
        //                 closestAnchorIndex = i;
        //             }
        //         }
        //
        //         if (closestAnchorIndex != -1)
        //         {
        //             Undo.RecordObject(_creator, "Delete Segment");
        //             Path.DeletePoint(closestAnchorIndex);
        //         }
        //     }
        //     
        //     float minThrehold = _selectThreshold;
        //     int newSegmentIndex = -1;
        //     for (int i = 0; i < Path.NumbSegs; i++)
        //     {
        //          Vector2[]  points = Path.GetPointOnSegment(i);
        //          float distance = HandleUtility.DistancePointBezier(mousePos, points[0], points[3], points[1], points[2]);
        //          if (distance < minThrehold)
        //          {
        //              newSegmentIndex = i;
        //              minThrehold = distance;
        //          }
        //     }
        //
        //     if (newSegmentIndex != _selectedSegmentIndex)
        //     {
        //         _selectedSegmentIndex = newSegmentIndex;
        //         HandleUtility.Repaint();
        //     }
        //     
        //     HandleUtility.AddDefaultControl(0);
        // }
    }
}
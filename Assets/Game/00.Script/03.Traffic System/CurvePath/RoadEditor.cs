using UnityEditor;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.CurvePath
{
    [CustomEditor(typeof(RoadCreator))]
    public class RoadEditor:Editor
    {
        private RoadCreator _roadCreator;

        private void OnEnable()
        {
            _roadCreator = (RoadCreator)target;
        }

        private void OnSceneGUI()
        {
            if (_roadCreator.autoUpdate && Event.current.type == EventType.Repaint)
            {
                _roadCreator.UpdateRoad();
            }
        }
        
    }
}
using UnityEngine;
using UnityEditor;
namespace Game._00.Script._06._Custom_Editor
{
    public class CustomReadOnlyAttribute: PropertyAttribute
    {
        
    }
    [CustomPropertyDrawer(typeof(CustomReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false; // Disable editing in the Inspector
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true; // Re-enable editing for other fields
        }
    }

}
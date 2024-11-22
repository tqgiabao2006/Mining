using UnityEngine;
using UnityEngine.Serialization;

namespace Game._03._Scriptable_Object
{
    [CreateAssetMenu(fileName = "PrefabManager", menuName = "Managers/PrefabManager")]
    public class PrefabManager : ScriptableObject
    { 
        public GameObject redBlood;
        public GameObject blueBlood;
    }

}
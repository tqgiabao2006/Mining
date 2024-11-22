using UnityEngine;

namespace Game._03._Scriptable_Object
{
    [CreateAssetMenu(fileName = "PrefabManager", menuName = "Managers/PrefabManager")]
    public class PrefabManager : ScriptableObject
    {
        public GameObject prefabA;
        public GameObject prefabB;
        public GameObject prefabC;
    }

}
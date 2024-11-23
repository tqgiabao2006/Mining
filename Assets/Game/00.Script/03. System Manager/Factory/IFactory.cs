
using UnityEngine;

namespace Game._00.Script._05._Manager.Factory
{
    public interface IFactory
    {
        ObjectPooling ObjectPooling { get; set; }
        public GameObject CreateBlood(GameObject prefab);
        public GameObject CreateOxygen(GameObject prefab);
        public GameObject CreateNutrients(GameObject prefab);
    }
}
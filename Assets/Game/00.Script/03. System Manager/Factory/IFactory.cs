
using UnityEngine;

namespace Game._00.Script._05._Manager.Factory
{
    public interface IFactory
    {
        ObjectPooling ObjectPooling { get; set; }
        public GameObject CreateBlood(string objectFlags);
        public GameObject CreateOxygen(string objectFlags);
        public GameObject CreateNutrients(string objectFlags);
    }
}
using System;
using UnityEngine;

namespace Game._00.Script._05._Manager.Factory
{
    public class BloodFactory:IFactory
    {
        private readonly float _speed = 0.2f;
        private readonly float _maxSpeed = 0.5f;
        private GameObject _redBloodPrefab;
        private GameObject _blueBloodPrefab;

        public ObjectPooling ObjectPooling { get; set; }

        public BloodFactory(GameObject redBloodPrefab, GameObject blueBloodPrefab)
        {
            ObjectPooling = GameManager.Instance.ObjectPooling;
            this._redBloodPrefab = redBloodPrefab;
            this._blueBloodPrefab = blueBloodPrefab;
        }

        public GameObject CreateBlood(string objectFlags)
        {
            if (_redBloodPrefab == null || _blueBloodPrefab == null)
            {
                Debug.LogError("Blood prefab is null");
                return null;
            }

            if (objectFlags == ObjectFlags.RedBlood)
            {
                GameObject redBlood = ObjectPooling.GetObj(_redBloodPrefab);
                redBlood.SetActive(false);
                return redBlood;
            }
            else
            {
                GameObject blueBlood = ObjectPooling.GetObj(_blueBloodPrefab);
                blueBlood.SetActive(false);
                return blueBlood;
            }
        }
        public GameObject CreateOxygen(string objectFlags) => null;
        public GameObject CreateNutrients(string objectFlags) => null;
    }
}
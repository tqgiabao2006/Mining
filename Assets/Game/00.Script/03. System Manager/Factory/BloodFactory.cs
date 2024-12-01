using System;
using UnityEngine;

namespace Game._00.Script._05._Manager.Factory
{
    public class BloodFactory: IFactory
    {
        private readonly float _speed = 0.2f;
        private readonly float _maxSpeed = 0.5f;
        private GameObject _redBloodPrefab;
        private GameObject _blueBloodPrefab;

        private BuildingBase _startBuilding;
        private BuildingBase _endBuilding;

        public ObjectPooling ObjectPooling { get; set; }

        public BloodFactory(GameObject redBloodPrefab, GameObject blueBloodPrefab, BuildingBase startBuilding, BuildingBase endBuilding)
        {
            ObjectPooling = GameManager.Instance.ObjectPooling;
            this._redBloodPrefab = redBloodPrefab;
            this._blueBloodPrefab = blueBloodPrefab;
            this._startBuilding = startBuilding;
            this._endBuilding = endBuilding;
        }

        public GameObject CreateBlood(GameObject prefab)
        {
            if (prefab == null || _startBuilding == null || _endBuilding == null)
            {
                Debug.LogError("Blood prefab is null");
                return null;
            }

            if (prefab == _redBloodPrefab)
            {
                GameObject redBlood = ObjectPooling.GetObj(_redBloodPrefab);
                redBlood.SetActive(false);
                return redBlood;
            }
            else
            {
                GameObject blueBlood = ObjectPooling.GetObj(_blueBloodPrefab);
                RedBlood blueBloodScript = blueBlood.GetComponent<RedBlood>();
                blueBloodScript.Intialize(_speed, _maxSpeed, _startBuilding, _endBuilding);
                blueBlood.SetActive(false);
                return blueBlood;
            }
        }
        
        public GameObject CreateOxygen(GameObject prefab) => null;
        public GameObject CreateNutrients(GameObject prefab) => null;

    }
}
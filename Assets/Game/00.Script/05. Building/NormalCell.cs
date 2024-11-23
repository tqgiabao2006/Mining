using System;
using System.Collections;
using Game._00.Script._05._Manager;
using Game._00.Script._05._Manager.Factory;
using UnityEngine;

namespace Game._00.Script._05._Building
{
    public class NormalCell:BuildingBase, IObserver
    {
        public void OnNotified(object data, string flag)
        {
            if (data is ValueTuple<BuildingBase, BuildingBase> && flag == NotificationFlags.SpawnCar)
            {
                ValueTuple<BuildingBase, BuildingBase> startEndBuildings = (ValueTuple<BuildingBase, BuildingBase>)data;
                StartCoroutine(SpawnCar(startEndBuildings));
            }
        }
        /// <summary>
        /// Start Building -> End Building
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override IEnumerator SpawnCar(ValueTuple<BuildingBase, BuildingBase> startEndBuildings)
        {
            bloodFactory = new BloodFactory(prefabManager.redBlood, prefabManager.blueBlood, startEndBuildings.Item1, startEndBuildings.Item2);
            GameObject blueBlood = bloodFactory.CreateBlood(prefabManager.blueBlood);
            if (!blueBlood) yield return null;
            
            blueBlood.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -0.1f);
            blueBlood.SetActive(true);
            yield return null;
        }

        

    }
}
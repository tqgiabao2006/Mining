using System;
using System.Collections;
using Game._00.Script._05._Manager;
using Game._00.Script._05._Manager.Factory;
using Game._03._Scriptable_Object;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game._00.Script._05._Building
{
    public class Heart: BuildingBase, IObserver
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
            
            GameObject redBlood = bloodFactory.CreateBlood(prefabManager.redBlood);
            if (!redBlood) yield return null;
            redBlood.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -0.1f);
            redBlood.SetActive(true);
            redBlood.GetComponent<RedBlood>().StartUpdatePath(startEndBuildings.Item2.transform);
            yield return null;
        }
        
    }
}
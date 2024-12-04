using System;
using System.Collections;
using Game._00.Script._05._Manager;
using UnityEngine;

namespace Game._00.Script._05._Building
{
    public class Lung: BuildingBase, IObserver
    {
        public void OnNotified(object data, string flag)
        {
            if (data is ValueTuple<BuildingBase, BuildingBase> && flag == NotificationFlags.SpawnCar)
            {
                ValueTuple<BuildingBase, BuildingBase> startEndBuildings = (ValueTuple<BuildingBase, BuildingBase>)data;
            }
        }
       
    }
}
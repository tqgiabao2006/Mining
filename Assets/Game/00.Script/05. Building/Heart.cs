using System;
using System.Collections;
using Game._00.Script._05._Manager;
using Game._00.Script.ECS_Test.FactoryECS;
using Game._00.Script.NewPathFinding;
using Game._03._Scriptable_Object;
using Unity.Entities;
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
            }
        }
    }
}
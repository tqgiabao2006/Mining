using System.Collections;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._03.Traffic_System.Building;
using Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS;
using Mono.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Business : BuildingBase 
{
    [Header("Business settings")]
    [SerializeField] private int demands;

    private bool DemandCar
    {
        get { return demands > 0; }
    }

    private void Update()
    {
        // while (demands > 0)
        // {
        // //     List<Home> homes = RoadManager.GetHomes(this);
        // //     Debug.Log(homes.Count);
        // //     if (homes.Count == 0)
        // //     {
        // //         return;
        // //     }
        // //     
        // //     //Filter to get only same-color buiilding
        // //     int i = 0;
        // //     while (homes.Count > 0)
        // //     {
        // //         if (!BuildingManager.IsOutput(this.BuildingType, homes[i].BuildingType))
        // //         {
        // //             homes.RemoveAt(i);
        // //         }
        // //         else
        // //         {
        // //             i++;
        // //         }
        // //     }
        // // }
        // }
    }

    /// <summary>
    /// Calling by cars when transition from parking to follow park
    /// </summary>
    public void CarLeave()
    {
        demands--;
    }
}

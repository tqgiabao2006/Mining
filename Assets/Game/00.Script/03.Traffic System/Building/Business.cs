using System;
using System.Collections.Generic;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class Business : BuildingBase
{
    [Header("Business settings")]
    [SerializeField] private int demands;

    private List<Home> _connectedHomes;

    public int Demands
    {
        get
        {
            return demands;
        }
    }

    private bool RequestCar
    {
        get { return demands > 0; }
    }
    
    public override void Initialize(BuildingManager buildingManager,Node node, BuildingType buildingType, BuildingDirection direction,
        Vector2 worldPosition)
    {
        base.Initialize(buildingManager ,node, buildingType, direction, worldPosition);
        BuildingManager.RegisterBuilding(this);
        
        _connectedHomes = new List<Home>();

    }

    public void AddHome(Home home)
    {
        _connectedHomes.Add(home);
    }

    public void RemoveHome(Home home)
    {
        _connectedHomes.Remove(home);
    }

    private void Update()
    {
        if (IsConnected)
        {
            while (RequestCar)
            {
                //RIGHT NOW: Get random connected home
                Home home = _connectedHomes[Random.Range(0, _connectedHomes.Count)];
                
                Entity carEntity = home.GetCar();
                if (carEntity == Entity.Null)
                {
                    continue;
                }
                else
                {
                    BuildingManager.DemandCars(carEntity, home, this);
                    demands--;
                }
                
            }
        }
    }


    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (!isGizmos)
        {
            return;
        }
        Handles.Label(new Vector3(transform.position.x, transform.position.y  + 0.5f, transform.position.z), 
            demands.ToString(), new GUIStyle { fontSize = 24, normal = { textColor = Color.yellow } });
    }

    /// <summary>
    /// Calling by cars when transition from parking to follow park
    /// </summary>
    public void CarLeave()
    {
        demands++;
    }
}
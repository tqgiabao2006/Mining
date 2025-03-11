using System.Collections.Generic;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using UnityEngine;
using Random = UnityEngine.Random;

public class Business : BuildingBase
{
    [Header("Business settings")]
    [SerializeField] private int demands;

    private bool RequestCar
    {
        get { return demands > 0; }
    }

    private List<Home> _connectedHomes;

    public override void Initialize(Node node, BuildingType buildingType, BuildingDirection direction,
        Vector2 worldPosition)
    {
        base.Initialize(node, buildingType, direction, worldPosition);
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
                BuildingManager.DemandCars(home.GetCar, home, this);
                demands--;
            }
        }
    }

    /// <summary>
    /// Calling by cars when transition from parking to follow park
    /// </summary>
    public void CarLeave()
    {
        demands++;
    }
}

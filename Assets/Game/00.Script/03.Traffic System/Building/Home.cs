using System.Collections;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using UnityEngine;

public class Home: BuildingBase
{
    [Header("Home settings")]
    [SerializeField] private int numbCars;

    public override void Initialize(Node node, BuildingType buildingType, BuildingDirection direction,
        Vector2 worldPosition)
    {
        base.Initialize(node, buildingType, direction, worldPosition);
        
        SpawnCars();
    }

    private string GetCarFlag()
    {
        switch (this.BuildingType)
        {
            case BuildingType.HomeBlue:
                return ObjectFlags.BlueCar;
            case BuildingType.HomeRed:
                return ObjectFlags.RedCar;
            case BuildingType.HomeYellow: 
                return ObjectFlags.YellowCar;
        }

        //Default car
        return ObjectFlags.BlueCar;
    }

    /// <summary>
    /// Get rotation, becuase the building prefab only use differnt sprite for differnt direction, not the real rotation
    /// </summary>
    /// <returns></returns>
    private Quaternion GetRotation()
    {
        switch (this.BuildingDirection)
        {
            case BuildingDirection.Up:
                return Quaternion.Euler(0, 0, 0);
            case BuildingDirection.Down:
                return Quaternion.Euler(0, 0, 180);
            case BuildingDirection.Left:
                return Quaternion.Euler(0, 0, 90);
            case BuildingDirection.Right:
                return Quaternion.Euler(0, 0, -90);
        }
        return Quaternion.Euler(0, 0, 0);
    }

    /// <summary>
    /// Amount = 2
    /// Default cars for small house
    /// Calculate car positions
    /// </summary>
    private void SpawnCars()
    {
        Vector2 direction = this.BuildingDirection switch
        {
            BuildingDirection.Up => Vector2.up,
            BuildingDirection.Down => Vector2.down,
            BuildingDirection.Left => Vector2.left,
            BuildingDirection.Right => Vector2.right,
            _ => Vector2.zero
        };
        
        //Use to differentiate between 2 cars
        Vector2 difVector = this.BuildingDirection == BuildingDirection.Up || this.BuildingDirection == BuildingDirection.Down ? Vector2.right : Vector2.up;
        
        //Spawn first car
        BuildingManager.SpawnCarWaves(
            this.WorldPosition + GridManager.NodeRadius*2/3f*direction + difVector * Game._00.Script._03.Traffic_System.Road.RoadManager.RoadWidth/4f,
            GetRotation(),
                GetCarFlag());
        
        BuildingManager.SpawnCarWaves(
            this.WorldPosition + GridManager.NodeRadius*2/3f*direction - difVector * Game._00.Script._03.Traffic_System.Road.RoadManager.RoadWidth/4f,
            GetRotation(),
            GetCarFlag()); 
        
        
        
        
 
    }
}

using System.Collections;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public class Home: BuildingBase
{
    [Header("Home settings")]
    [SerializeField] private int numbCars;

    private Queue<Entity> _cars;

    public int NumbCars
    {
        get
        {
            return numbCars;
        }
    }
    
    public override void Initialize(BuildingManager buildingManager,Node node, BuildingType buildingType, BuildingDirection direction,
        Vector2 worldPosition)
    {
        base.Initialize(buildingManager,node, buildingType, direction, worldPosition);

        _cars = new Queue<Entity>();
        
        SpawnCars();
       
        BuildingManager.RegisterBuilding(this);

    }

    public Entity GetCar()
    {
        if (_cars.Count > 0)
        {
            return _cars.Dequeue();
        }
        return Entity.Null;
    }

    public void CarReturn(Entity car)
    {
        _cars.Enqueue(car);
    }

    public void AddCarEntity(Entity entity)
    {
        _cars.Enqueue(entity);
    }
    private string GetCarFlag()
    {
        switch (this.BuildingColor)
        {
            case BuildingColor.Blue:
                return ObjectFlags.BLUE_CAR;
            case BuildingColor.Red:
                return ObjectFlags.RED_CAR;
            // case BuildingColor.Yellow: 
            //     return ObjectFlags.YELLOW_CAR;
        }

        //Default car
        return ObjectFlags.BLUE_CAR;
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
            this,
            this.WorldPosition + GridManager.NodeRadius*2/3f*direction + difVector * Game._00.Script._03.Traffic_System.Road.RoadManager.RoadWidth/4f,
            GetRotation(),
            GetCarFlag());
        
        BuildingManager.SpawnCarWaves(
            this,
            this.WorldPosition + GridManager.NodeRadius*2/3f*direction - difVector * Game._00.Script._03.Traffic_System.Road.RoadManager.RoadWidth/4f,
            GetRotation(),
            GetCarFlag());
        
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (!isGizmos)
        {
            return;
        }
        Handles.Label(new Vector3(transform.position.x, transform.position.y  + 0.5f, transform.position.z), 
           _cars.Count + " cars", new GUIStyle { fontSize = 24, normal = { textColor = Color.black } });
        
    }
}

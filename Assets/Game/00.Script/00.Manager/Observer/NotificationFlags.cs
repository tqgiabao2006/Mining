using UnityEngine;

namespace Game._00.Script._00.Manager.Observer
{
    public static class NotificationFlags
    {
        public const string PLACING_STATE = "Placing"; //FROM placingSystem TO GameStateManager FOR set game state to placing, trigger some shader for visualization
        public const string CHECK_CONNECTION = "Checking Connection"; //FROM placingSystem TO roadManager FOR Checking if building is connected to its output buildings
        public const string SPAWN_CAR = "Spawn Car"; //FROM buildingManger TO building FOR start coroutine spawning car
        public const string REGISTER_BUILDING = "Spawn Building";
        public const string UPDATE_LEVEL = "Update Level";
        public const string DEMAND_CAR = "Demand Car";
        public const string DEMAND_BUILDING = "Demand Building";
        public const string WEEK_PASS = "Week pass";
        public const string PLACING = "Placing";
        public const string NOT_PLACING = "Not Placing"; 
    }

    public static class ObjectFlags
    {
        public const string RED_CAR = "Red Car";
        public const string BLUE_CAR = "Blue Car";
        public const string YELLOW_CAR = "Yellow Car";
    }

    public abstract class DirectoryFlags
    {
        public static string PARKING_WAYPOINT = Application.dataPath + System.IO.Path.AltDirectorySeparatorChar + "Game" + System.IO.Path.AltDirectorySeparatorChar + 
                                               "Tests" + System.IO.Path.AltDirectorySeparatorChar + "Json Data" + System.IO.Path.AltDirectorySeparatorChar + 
                                               "Parking Waypoints";
    }

    public static class LayerTag
    {
        public const string SUPPLY = "Supply";
        public const string DEMAND = "Demand";
        public const string UNSPAWNABLE = "Unspawnable";
    }
}
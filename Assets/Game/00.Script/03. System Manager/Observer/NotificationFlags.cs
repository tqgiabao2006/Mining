namespace Game._00.Script._05._Manager
{
    public static class NotificationFlags
    {
        public const string PlacingState = "Placing"; //FROM placingSystem TO GameStateManager FOR set game state to placing, trigger some shader for visualization
        public const string CheckingConnection = "Checking Connection"; //FROM placingSystem TO roadManager FOR Checking if building is connected to its output buildings
        public const string SpawnCar = "Spawn Car"; //FROM buildingManger TO building FOR start coroutine spawning car
    }

    public static class ObjectFlags
    {
        public const string RedBlood = "Red Blood";
        public const string BlueBlood = "Blue Blood";

        public const string Car = "Car";
    }
}
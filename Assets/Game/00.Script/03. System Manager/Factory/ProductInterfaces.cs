namespace Game._00.Script._05._Manager.Factory
{
    public interface IBlood
    {
        float Speed { get; set; }
        float MaxSpeed { get; set; }
        bool IsFinishedPath { get; set; }
        public void Intialize(float speed, float maxSpeed, BuildingBase startBuilding, BuildingBase endBuilding);
    }

    public interface INutrients
    {
        void SpawnNutrients();
    }

    public interface IOxygen
    {
        void SpawnOxygen();
    }
}
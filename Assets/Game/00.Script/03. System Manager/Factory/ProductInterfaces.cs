namespace Game._00.Script._05._Manager.Factory
{
    public interface IBlood
    {
        float Speed { get; set; }
        float MaxSpeed { get; set; }
        bool IsFinishedPath { get; set; }
        void Intialize(float speed, float maxSpeed);
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
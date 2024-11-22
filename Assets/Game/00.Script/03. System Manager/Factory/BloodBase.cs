namespace Game._00.Script._05._Manager.Factory
{
    public class BlueBlood: UnitBase, IBlood
    {
        public float Speed { get; set; }
        public float MaxSpeed { get; set; }
        public bool IsFinishedPath { get; set; }
        public void Intialize(float speed, float maxSpeed)
        {
            this.Speed = speed;
            this.MaxSpeed = maxSpeed;
        }
    }
    
    public class RedBlood: UnitBase, IBlood
    {
        public float Speed { get; set; }
        public float MaxSpeed { get; set; }
        public bool IsFinishedPath { get; set; }
        public void Intialize(float speed, float maxSpeed)
        {
            this.Speed = speed;
            this.MaxSpeed = maxSpeed;
        }
    }
}
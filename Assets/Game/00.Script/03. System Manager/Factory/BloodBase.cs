namespace Game._00.Script._05._Manager.Factory
{
    public abstract class BloodBase:UnitBase, IBlood
    {
        public void SpawnBlood()
        {
            
        }
        public abstract IBlood CreateBlood();

    }
}
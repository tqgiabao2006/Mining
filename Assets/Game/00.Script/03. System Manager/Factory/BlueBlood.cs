namespace Game._00.Script._05._Manager.Factory
{
    public class BlueBlood:BloodBase
    {
        public override IBlood CreateBlood() => new BlueBlood();

    }
}
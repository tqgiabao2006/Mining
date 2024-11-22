namespace Game._00.Script._05._Manager.Factory
{
    public class RedBlood: BloodBase
    {
        public override IBlood CreateBlood() => new RedBlood();
       
    }
}
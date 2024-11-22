using System.Collections;
using Game._00.Script._05._Manager;

namespace Game._00.Script._05._Building
{
    public class Lung: BuildingBase, IObserver
    {
        public void OnNotified(object data, string flag)
        {
            if (data is bool b && flag == NotificationFlags.SpawnCar)
            {
                if (!b) return;
                StartCoroutine(SpawnCar(b));
            }
        }

        protected override IEnumerator SpawnCar(bool canSpawn)
        {
            throw new System.NotImplementedException();
        }
    }
}
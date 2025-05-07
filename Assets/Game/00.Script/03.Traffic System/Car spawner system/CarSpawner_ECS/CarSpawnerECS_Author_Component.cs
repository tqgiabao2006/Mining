using Game._00.Script._03.Traffic_System.PathFinding;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace  Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS
{
  public class CarSpawnerEcsAuthorComponent: MonoBehaviour
    {
        private PathRequestManager _pathRequestManager;
         public GameObject redCar;
         public GameObject blueCar;
        
        private void Start()
        {
            _pathRequestManager = FindObjectOfType<PathRequestManager>();
        }
        
        private class Baker: Baker<CarSpawnerEcsAuthorComponent>
        {
            public override void Bake(CarSpawnerEcsAuthorComponent author)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                DependsOn(author.transform);
                if (author.redCar == null || author.blueCar == null)
                {
                    return;
                }
                AddComponent(entity, new SpawnGameObjectHolder()
                {
                    RedCar = GetEntity(author.redCar, TransformUsageFlags.Dynamic),
                    BlueCar = GetEntity(author.blueCar, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
    public struct SpawnGameObjectHolder : IComponentData
    {
        public Entity RedCar;
        public Entity BlueCar;
    }

}
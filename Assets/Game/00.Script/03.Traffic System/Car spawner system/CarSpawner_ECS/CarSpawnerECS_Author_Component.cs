using Game._00.Script._03.Traffic_System.PathFinding;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace  Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS
{
  public class CarSpawnerECS_Author_Component: MonoBehaviour
    {
        private PathRequestManager _pathRequestManager;
        //Testing only
        public GameObject redBlood;
        public GameObject blueBlood;
        
        private void Start()
        {
            _pathRequestManager = FindObjectOfType<PathRequestManager>();
        }
        
        private class Baker: Baker<CarSpawnerECS_Author_Component>
        {
            public override void Bake(CarSpawnerECS_Author_Component author)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                DependsOn(author.transform);
                if (author.redBlood == null || author.blueBlood == null)
                {
                    return;
                }
                AddComponent(entity, new SpawnGameObjectHolder()
                {
                    RedBlood = GetEntity(author.redBlood, TransformUsageFlags.Dynamic),
                    BlueBlood = GetEntity(author.blueBlood, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
    public struct SpawnGameObjectHolder : IComponentData
    {
        public Entity RedBlood;
        public Entity BlueBlood;
    }

    public struct SpawnData
    {
        public float3 StartPos;
        public float3 EndPos;
        public BlobAssetReference<BlobArray<float3>> Waypoints;
    }
}
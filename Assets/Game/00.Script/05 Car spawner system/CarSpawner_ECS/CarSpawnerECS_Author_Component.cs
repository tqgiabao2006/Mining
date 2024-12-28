using Game._00.Script._00._Core_Assembly_Def;
using Game._00.Script._05._Manager;
using Game._00.Script.NewPathFinding;
using Game._03._Scriptable_Object;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game._00.Script._05_Car_spawner_system.CarSpawner_ECS
{
  public class CarSpawnerECS_Author_Component: MonoBehaviour
    {
        private PathRequestManager _pathRequestManager;
        //Testing only
        public GameObject redBlood;
        public GameObject blueBlood;
        
        private void Start()
        {
            _pathRequestManager = GameManager.Instance.PathRequestManager;
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
        public BlobAssetReference<BlobArray<float3>>  Waypoints;
    }


  
  
}
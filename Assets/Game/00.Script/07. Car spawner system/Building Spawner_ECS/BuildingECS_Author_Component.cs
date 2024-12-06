using Game._00.Script._05._Manager;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;


namespace Game._00.Script._07._Car_spawner_system.Building_Spawner_ECS
{
    public class BuildingECS_Author_Component : MonoBehaviour
    {
        [SerializeField] private BuildingType _buildingType;

        [SerializeField] private float _size;

        [SerializeField] private float _lifeTIme;
        // private RoadManager _roadManager;
        // private GridManager _gridManager;
        //
        // private void Start()
        // {
        //     _gridManager = GameManager.Instance.GridManager;
        //     _roadManager = GameManager.Instance.RoadManager;    
        // }

        private class Baker : Baker<BuildingECS_Author_Component>
        {
            public override void Bake(BuildingECS_Author_Component author)
            {
                if (author._size == 0 || author._lifeTIme == 0)
                {
                    return;
                }
                Entity entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<BuildingData>(entity);
                AddComponent<NodeData>(entity);
                AddComponent(entity, new Size()
                {
                    Value = author._size
                });
                AddComponent(entity, new LifeTime()
                {
                  Value  = author._lifeTIme
                });
            }
        }
    }

    public struct BuildingData : IComponentData
    {
        public BuildingType Value;
    }

    public struct NodeData : IComponentData
    {
        public Node Value;
    }

    public struct Size : IComponentData
    {
        public float Value;
    }

    public struct LifeTime : IComponentData
    {
        public float Value;
    }
    

}
using Unity.VisualScripting;
using UnityEngine;

namespace Game._00.Script._05._Manager.Factory
{
    public class RedBlood: UnitBase, IBlood
    {
        public float Speed { get; set; }
        public float MaxSpeed { get; set; }
        public bool IsFinishedPath { get; set; }

        private BuildingBase _startBuilding;
        private BuildingBase _endBuilding;
        public void Intialize(float speed, float maxSpeed, BuildingBase startBuilding, BuildingBase endBuilding)
        {
            this.Speed = speed;
            this.MaxSpeed = maxSpeed;
            this._startBuilding = startBuilding;
            this._endBuilding = endBuilding;
        }
        
    }
}
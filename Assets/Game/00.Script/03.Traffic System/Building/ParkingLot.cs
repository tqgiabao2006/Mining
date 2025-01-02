using Unity.Mathematics;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.Building
{
    public struct ParkingLot
    {
        public float3 Position;
        public bool IsEmpty;
        
        public ParkingLot(float3 position, bool isEmpty)
        {
            Position = position;
            IsEmpty = isEmpty;
        }
    }
}
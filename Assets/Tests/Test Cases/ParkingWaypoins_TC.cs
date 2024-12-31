using Game._00.Script._03.Traffic_System.Building;
using Unity.Mathematics;
using UnityEngine;

namespace Tests.Test_Cases
{
    [System.Serializable]
    public class ParkingWaypointsTestCase
    {
        [System.Serializable]
        public class ParkingPointsData
        {
            public Vector2 BuildingPos;
            public Vector2 RoadPos;
            public Vector3 CenterPoint;
            public Vector3 ParkingPos;
            public BuildingDirection Direction;
            public ParkingLotSize Size;
        }

        public ParkingPointsData Input;
        public float3[] Output;

        public ParkingWaypointsTestCase(Vector3 buildingPos, Vector2 roadPos, Vector3 parkingPos,
            Vector3 centerPoint,BuildingDirection direction, ParkingLotSize size, float3[] wayPoints)
        {
            Input = new ParkingPointsData
            {
                BuildingPos = buildingPos,
                RoadPos = roadPos,
                CenterPoint = centerPoint,
                ParkingPos = parkingPos,
                Direction = direction,
                Size = size
            };
            Output = wayPoints;
        }
    }

}
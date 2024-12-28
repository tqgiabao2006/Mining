using System;
using System.Collections.Generic;
using Game._00.Script._00._Core_Assembly_Def;
using Game._00.Script._02._System_Manager;
using Game._00.Script._03._Building;
using Game._00.Script._07._Mesh_Generator;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game._04.Tests.Test_Cases
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._02.Grid_setting;
using Game._00.Script._03.Traffic_System.Building;

namespace Game.Tests.PlayMode
{
    public class ParkingWaypoints_Test
    {
        [UnityTest]
        public IEnumerator TestPlayMode_ParkingWaypoints()
        {
            string path = DirectoryFlags.ParkingWaypoint;
            if (!Directory.Exists(path))
            {
                DebugUtility.LogError("Parking Waypoints folder does not exist: ", this.ToString());
                Assert.Fail("Parking Waypoints folder does not exist.");
            }
            
            string[] testCaseFiles = Directory.GetFiles(path, "*.json");
            Assert.IsNotEmpty(testCaseFiles, "No JSON files found in the directory!");

            bool testFailed = false;

            foreach (string filePath in testCaseFiles)
            {
                string jsonData = File.ReadAllText(filePath);
                ParkingWaypointsTestCase testCase = JsonUtility.FromJson<ParkingWaypointsTestCase>(jsonData);
                ParkingWaypointsTestCase.ParkingPointsData inputData = testCase.Input;
                float3[] expectedResult = testCase.Output;
                
                GameObject go = new GameObject();
                Business building = go.AddComponent<Business>();
                
                float3[] testResult = building.GetParkingWaypoints(inputData.BuildingPos, inputData.Direction,
                    inputData.Size, inputData.ParkingPos, inputData.CenterPoint, inputData.RoadPos);
                
                bool testCaseFailed = CompareTwoList(testResult, expectedResult, filePath, inputData.Size,  inputData.Direction,  inputData.RoadPos);
                if (testCaseFailed)
                {
                    testFailed = true;
                }
            }

            if (testFailed)
            {
                Assert.Fail("One or more test cases failed.");
            }
            else
            {
                Assert.Pass();
            }
            yield break;
        }

        /// <summary>
        /// Return true if case is fail
        /// </summary>
        /// <param name="testResult"></param>
        /// <param name="expectedResult"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool CompareTwoList(float3[] testResult, float3[] expectedResult, string filePath, ParkingLotSize size, BuildingDirection direction, Vector2 roadPos )
        {
            float threshold = 0.01f;
            if (testResult.Length != expectedResult.Length)
            { 
                DebugUtility.LogError($"Test case failed {size}|{direction}|{roadPos}: Length mismatch. {testResult.Length} != {expectedResult.Length}", this.ToString());
                return true; 
            }

            for (int i = 0; i < testResult.Length; i++)
            {
                if ((testResult[i].x > expectedResult[i].x + threshold || testResult[i].x < expectedResult[i].x - threshold)
                    || (testResult[i].y > expectedResult[i].y + threshold || testResult[i].y < expectedResult[i].y - threshold))
                {
                    DebugUtility.LogError($"Test case failed for {size}|{direction}|{roadPos}: Mismatch at index {i}. {testResult[i]} != {expectedResult[i]}", this.ToString());
                    return true;
                }
            }
            return false; 
        }
    }

}
    
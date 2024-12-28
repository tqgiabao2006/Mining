using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Game._04.Tests.EditorMode
{
    public class ParkingWaypoints_Test
    {
        [Test]
        public void TestEditor_ParkingWaypoints()
        { 
            string path = Application.dataPath + System.IO.Path.AltDirectorySeparatorChar + "Game" + System.IO.Path.AltDirectorySeparatorChar + "05. Json Data" +  System.IO.Path.AltDirectorySeparatorChar + "Parking Waypoints";
            if (!Directory.Exists(path))
            {
                Debug.LogError("Parking Waypoints folder does not exist: ");
                Assert.Fail();
            }
        
            string[] testCaseFiles = Directory.GetFiles( path,"*.json");
            Assert.IsNotEmpty(testCaseFiles, "No JSON files found in the directory!");
        
            foreach (string filePath in testCaseFiles)
            {
                string jsonData = File.ReadAllText(filePath);
            }
        }
    }
}
    
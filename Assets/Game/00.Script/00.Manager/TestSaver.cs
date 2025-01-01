using System;
using System.Collections.Generic;
using System.IO;
using Game._00.Script._00.Manager.Custom_Editor;
using UnityEngine;

namespace Game._00.Script._00.Manager
{
    public enum TestCase
    {
        ParkingWaypoints,
    }
    public class TestSaver : MonoBehaviour
    {
        private Dictionary<TestCase, Delegate> _testFuncs;
        public Dictionary<TestCase, Delegate> TestFuncs {get{return _testFuncs;}}
        private void Start()
        {
            _testFuncs = new Dictionary<TestCase, Delegate>();
        }

        public void SaveData<T>(T data, string fileName, string  directoryPath) where T : class
        {
            string json = JsonUtility.ToJson(data);

            if (!Directory.Exists(directoryPath))
            {
                DebugUtility.LogError("Directory doesn't exist!", this.ToString());
                return;
            }

            string filePath = directoryPath + System.IO.Path.AltDirectorySeparatorChar + fileName + ".json";
            using (StreamWriter writer = new StreamWriter( filePath, false))
            {
                writer.Write(json);
            }
            Debug.Log(json);
        }

        public void SaveTestFunc<T>(T funcion, TestCase testCase) where T : Delegate
        {
            if (!_testFuncs.ContainsKey(testCase))
            {
                _testFuncs.Add(testCase, funcion);
            }
            else
            {
                _testFuncs[testCase] = funcion;
            }
        }

        

    }
}


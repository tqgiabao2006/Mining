using System.IO;
using Game._00.Script._00.Manager.Custom_Editor;
using UnityEngine;

namespace Game._00.Script._00.Manager
{
    public class DataSaver : MonoBehaviour
    {
        public static void SaveData<T>(T data, string fileName, string  directoryPath) where T : class
        {
            string json = JsonUtility.ToJson(data);

            if (!Directory.Exists(directoryPath))
            {
                DebugUtility.LogError("Directory doesn't exist!");
                return;
            }

            string filePath = directoryPath + System.IO.Path.AltDirectorySeparatorChar + fileName + ".json";
            using (StreamWriter writer = new StreamWriter( filePath, false))
            {
                writer.Write(json);
            }
            Debug.Log(json);
        }

    }
}


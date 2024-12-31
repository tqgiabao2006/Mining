using Game._00.Script._00.Manager.Custom_Editor;
using UnityEngine;

namespace Game._00.Script._00.Manager
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        DebugUtility.LogError(typeof(T) + " is needed in the scene but is missing!");
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject); // Optional if you want the singleton to persist between scenes
            }
            else if (_instance != this)
            {
                Destroy(gameObject); // Destroy the entire GameObject, not just the component
            }
        }
    }
}
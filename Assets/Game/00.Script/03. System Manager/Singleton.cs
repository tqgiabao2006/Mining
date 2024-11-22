using Game._00.Script._05._Manager;
using UnityEngine;

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
                    Debug.LogError(typeof(T) + " is needed in the scene but is missing!");
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
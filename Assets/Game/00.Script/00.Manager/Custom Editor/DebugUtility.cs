namespace Game._00.Script._00.Manager.Custom_Editor
{
    //Use to avoid having Debug.Log when build
    public static class DebugUtility
    {
        public static void Log(string message)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(message);
#endif
        }

        public static void LogWarning(string message)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogWarning(message);
#endif
        }

        public static void LogError(string message)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogError(message);
#endif
        }
    }

}
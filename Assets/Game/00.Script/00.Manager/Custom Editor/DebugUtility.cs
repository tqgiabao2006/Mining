namespace Game._00.Script._00.Manager.Custom_Editor
{
    //Use to avoid having Debug.Log when build
    public static class DebugUtility
    {
        public static void Log(string message, string callerName)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(callerName+ ": " + message);
#endif
        }

        public static void LogWarning(string message, string callerName)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogWarning(callerName+ ": " + message);
#endif
        }

        public static void LogError(string message, string callerName)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogError(callerName+ ": " + message);
#endif
        }
    }

}
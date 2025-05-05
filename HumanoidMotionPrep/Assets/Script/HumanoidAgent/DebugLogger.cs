using UnityEngine;

#if UNITY_EDITOR
public static class DebugLogger
{
    public static bool EnableDebug = false;
    public static void Log(string message, string category = "General", LogType type = LogType.Log, string color = "white")
    {
        if (!EnableDebug) return;

        string logMessage = $"<b><color={color}> [{category}] </color></b> {message}";

        switch (type)
        {
            case LogType.Warning:
                Debug.LogWarning(logMessage);
                break;
            case LogType.Error:
                Debug.LogError(logMessage);
                break;
            default:
                Debug.Log(logMessage);
                break;
        }
    }
}
#endif
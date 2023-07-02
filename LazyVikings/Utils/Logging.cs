namespace LazyVikings.Utils;

public static class Logging
{
    public static void LogDebug(string debug)
    {
        Plugin.LVLogger.LogDebug(debug);
    }

    public static void LogInfo(string info)
    {
        Plugin.LVLogger.LogInfo(info);
    }

    public static void LogWarning(string warning)
    {
        Plugin.LVLogger.LogWarning(warning);
    }

    public static void LogError(string error)
    {
        Plugin.LVLogger.LogError(error);
    }
}
namespace HomeManager.Services;

internal static partial class Log
{
    // EventBus

    [LoggerMessage(Level = LogLevel.Error, Message = "Event filter exception. eventType=[{eventType}]")]
    public static partial void ErrorEventFilterException(this ILogger logger, string eventType, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Event handler exception. eventType=[{eventType}]")]
    public static partial void ErrorEventHandlerException(this ILogger logger, string eventType, Exception ex);

    // Setting

    [LoggerMessage(Level = LogLevel.Warning, Message = "Setting value is invalid, default value is used. key=[{key}]")]
    public static partial void WarnSettingInvalid(this ILogger logger, string key, Exception ex);

    // SystemInfoService

    [LoggerMessage(Level = LogLevel.Warning, Message = "Database information query failed.")]
    public static partial void WarnDatabaseInfoFailed(this ILogger logger, Exception ex);
}

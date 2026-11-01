namespace HomeManager.Services;

internal static partial class Log
{
    // EventBus

    [LoggerMessage(Level = LogLevel.Error, Message = "Event filter exception. eventType=[{eventType}]")]
    public static partial void ErrorEventFilterException(this ILogger logger, string eventType, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Event handler exception. eventType=[{eventType}]")]
    public static partial void ErrorEventHandlerException(this ILogger logger, string eventType, Exception ex);
}

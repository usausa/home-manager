namespace HomeManager.Host.Workers;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Worker start. worker=[{worker}]")]
    public static partial void InfoWorkerStart(this ILogger logger, string worker);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker stop. worker=[{worker}]")]
    public static partial void InfoWorkerStop(this ILogger logger, string worker);
}

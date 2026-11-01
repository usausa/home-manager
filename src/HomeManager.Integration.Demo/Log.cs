namespace HomeManager.Integration.Demo;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Demo devices ready.")]
    public static partial void InfoDemoSetup(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo button pressed. entityId=[{entityId}]")]
    public static partial void InfoDemoButtonPressed(this ILogger logger, string entityId);
}

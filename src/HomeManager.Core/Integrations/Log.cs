namespace HomeManager.Integrations;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Integration loaded. domain=[{domain}]")]
    public static partial void InfoIntegrationLoaded(this ILogger logger, string domain);

    [LoggerMessage(Level = LogLevel.Information, Message = "Integration unloaded. domain=[{domain}]")]
    public static partial void InfoIntegrationUnloaded(this ILogger logger, string domain);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Integration attribute missing. type=[{type}]")]
    public static partial void WarnIntegrationMetadataMissing(this ILogger logger, string type);

    [LoggerMessage(Level = LogLevel.Error, Message = "Integration setup failed. domain=[{domain}]")]
    public static partial void ErrorIntegrationSetup(this ILogger logger, string domain, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Integration unload failed. domain=[{domain}]")]
    public static partial void ErrorIntegrationUnload(this ILogger logger, string domain, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Integration background task failed. domain=[{domain}]")]
    public static partial void ErrorIntegrationBackground(this ILogger logger, string domain, Exception ex);
}

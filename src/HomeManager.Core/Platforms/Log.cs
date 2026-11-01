namespace HomeManager.Platforms;

internal static partial class Log
{
    // Entity

    [LoggerMessage(Level = LogLevel.Warning, Message = "Entity already added. platform=[{platform}], entityId=[{entityId}]")]
    public static partial void WarnEntityAlreadyAdded(this ILogger logger, string platform, string entityId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Entity unique id duplicated. platform=[{platform}], uniqueId=[{uniqueId}]")]
    public static partial void WarnEntityDuplicateUniqueId(this ILogger logger, string platform, string uniqueId);

    // Service

    [LoggerMessage(Level = LogLevel.Warning, Message = "Service target not found or unavailable. service=[{domain}.{service}], entityId=[{entityId}]")]
    public static partial void WarnServiceTargetNotFound(this ILogger logger, string domain, string service, string entityId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Service not supported for domain. service=[{domain}.{service}], entityDomain=[{entityDomain}]")]
    public static partial void WarnServiceNotSupported(this ILogger logger, string domain, string service, string entityDomain);
}

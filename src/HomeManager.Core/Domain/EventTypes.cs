namespace HomeManager.Domain;

// イベントの種別 (HA と同じ名前にする)
public static class EventTypes
{
    public const int MaxLength = 64;

    public const string StateChanged = "state_changed";

    public const string StateReported = "state_reported";

    public const string CallService = "call_service";

    public const string ServiceRegistered = "service_registered";

    public const string ServiceRemoved = "service_removed";

    public const string ComponentLoaded = "component_loaded";

    public const string HomeManagerStart = "homemanager_start";

    public const string HomeManagerStarted = "homemanager_started";

    public const string HomeManagerStop = "homemanager_stop";

    public const string CoreConfigUpdated = "core_config_updated";

    // 画面で選べる既知の種別
    public static IReadOnlyList<string> Known { get; } =
    [
        StateChanged,
        StateReported,
        CallService,
        ServiceRegistered,
        ServiceRemoved,
        ComponentLoaded,
        HomeManagerStart,
        HomeManagerStarted,
        HomeManagerStop,
        CoreConfigUpdated
    ];
}

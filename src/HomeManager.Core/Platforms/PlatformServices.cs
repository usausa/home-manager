namespace HomeManager.Platforms;

// ドメインごとのサービス (HA の各 EntityComponent が登録する light.turn_on など)
public static class PlatformServices
{
    public const string TurnOn = "turn_on";

    public const string TurnOff = "turn_off";

    public const string Toggle = "toggle";

    public const string Press = "press";

    public const string SetValue = "set_value";

    public const string SelectOption = "select_option";

    public const string SelectNext = "select_next";

    public const string SelectPrevious = "select_previous";

    public const string SelectFirst = "select_first";

    public const string SelectLast = "select_last";

    public static void RegisterAll(IServiceRegistry registry, EntityManager entities, ILogger log)
    {
        HomeManagerServices.Register(registry, log);
        LightServices.Register(registry, entities);
        SwitchServices.Register(registry, entities);
        ButtonServices.Register(registry, entities);
        NumberServices.Register(registry, entities);
        SelectServices.Register(registry, entities);
    }

    // ON / OFFのサービスのドメイン (概要のタイルでアイコンを押したときに切り替えるもの)
    public static bool IsToggleable(string domain) =>
        domain is Domains.Light or Domains.Switch;
}

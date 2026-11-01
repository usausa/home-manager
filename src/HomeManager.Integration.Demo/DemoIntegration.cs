namespace HomeManager.Integration.Demo;

// 仮想デバイス (HA の demo)。実機なしで画面・自動化を試せる照明・スイッチ・センサー
[Integration(Domain, Name = "デモ (仮想デバイス)", IotClass = IotClass.Calculated)]
public sealed class DemoIntegration : IIntegration
{
    public const string Domain = "demo";

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private readonly ILogger<DemoIntegration> log;

    private readonly TimeProvider timeProvider;

    public DemoIntegration(ILogger<DemoIntegration> log, TimeProvider timeProvider)
    {
        this.log = log;
        this.timeProvider = timeProvider;
    }

    public ValueTask SetupAsync(IIntegrationContext context, CancellationToken cancel)
    {
        var temperature = new DemoSensor("living_temperature", "リビングの温度", DeviceClasses.Temperature, "°C", 23.5, new DemoWalk(0.1, 18, 30, 1));
        var humidity = new DemoSensor("living_humidity", "リビングの湿度", DeviceClasses.Humidity, "%", 48, new DemoWalk(1, 30, 70, 0));
        var co2 = new DemoSensor("living_co2", "リビングのCO2", DeviceClasses.CarbonDioxide, "ppm", 650, new DemoWalk(20, 400, 2000, 0));
        var washer = new DemoSwitch("washer", "洗濯機", DeviceClasses.Outlet, false);
        var washerPower = new DemoSensor("washer_power", "洗濯機の電力", DeviceClasses.Power, "W", 0.5, null);
        var motion = new DemoBinarySensor("hallway_motion", "廊下の人感センサー", DeviceClasses.Motion, false);
        var door = new DemoBinarySensor("front_door", "玄関のドア", DeviceClasses.Door, false);

        context.AddEntities(
        [
            new DemoLight("living_ceiling", "リビングの天井照明", true, true, 204),
            new DemoLight("bedroom_stand", "寝室のスタンド", true, false, 128),
            new DemoLight("hallway", "廊下の照明", false, false, null),
            new DemoSwitch("tv_power", "テレビの電源", DeviceClasses.Outlet, true),
            washer,
            temperature,
            humidity,
            co2,
            washerPower,
            motion,
            door,
            new DemoButton("chime", "チャイム", log),
            new DemoNumber("speaker_volume", "スピーカーの音量", 30, 0, 100, 1, "%"),
            new DemoSelect("fan_speed", "扇風機の風量", ["弱", "中", "強"], "中")
        ]);

        // センサーの値を一定の間隔で動かす
        context.RunBackground(c => SimulateAsync([temperature, humidity, co2], washer, washerPower, motion, door, c));

        log.InfoDemoSetup();
        return ValueTask.CompletedTask;
    }

    private async Task SimulateAsync(
        IReadOnlyList<DemoSensor> sensors,
        DemoSwitch washer,
        DemoSensor washerPower,
        DemoBinarySensor motion,
        DemoBinarySensor door,
        CancellationToken cancel)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        while (await timer.WaitForNextTickAsync(cancel))
        {
            foreach (var sensor in sensors)
            {
                sensor.Step();
            }

            // 洗濯機は動いている間だけ電力が上下する
            washerPower.Update(washer.IsOn == true ? 300 + RandomNumberGenerator.GetInt32(0, 200) : 0.5);

            // 人感は時々、ドアはまれに切り替わる
            if (RandomNumberGenerator.GetInt32(0, 5) == 0)
            {
                motion.Update(!(motion.IsOn ?? false));
            }

            if (RandomNumberGenerator.GetInt32(0, 20) == 0)
            {
                door.Update(!(door.IsOn ?? false));
            }
        }
    }
}

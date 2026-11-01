namespace HomeManager.Platforms;

// ボタン (HA の ButtonEntity)。状態は最後に押した時刻
public abstract class ButtonEntity : HomeEntity
{
    private DateTimeOffset? lastPressed;

    public sealed override string Domain => Domains.Button;

    public sealed override string? State => lastPressed?.ToString("O", CultureInfo.InvariantCulture);

    public abstract ValueTask PressAsync(CancellationToken cancel);

    // 押した時刻を先に書いてから押す (HA の _async_press_action)
    internal async ValueTask HandlePressAsync(DateTimeOffset now, CancellationToken cancel)
    {
        lastPressed = now;
        WriteState();
        await PressAsync(cancel);
    }
}

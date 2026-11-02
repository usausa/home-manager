namespace HomeManager.Services;

public interface IOnboardingService
{
    bool IsDone(string stepName);

    // DBから済んだ手順を読む (起動時)
    ValueTask LoadAsync(CancellationToken cancellationToken = default);

    ValueTask CompleteAsync(string stepName, CancellationToken cancellationToken = default);
}

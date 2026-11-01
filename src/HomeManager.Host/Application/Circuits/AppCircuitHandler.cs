namespace HomeManager.Host.Application.Circuits;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

public sealed class AppCircuitHandler : CircuitHandler
{
    private readonly ILogger<AppCircuitHandler> log;

    private readonly TimeProvider timeProvider;

    private readonly AuthenticationStateProvider authenticationStateProvider;

    private readonly CircuitTracker tracker;

    public AppCircuitHandler(
        ILogger<AppCircuitHandler> log,
        TimeProvider timeProvider,
        AuthenticationStateProvider authenticationStateProvider,
        CircuitTracker tracker)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.authenticationStateProvider = authenticationStateProvider;
        this.tracker = tracker;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = state.User.Identity?.Name;
        tracker.Add(new CircuitInfo(circuit.Id, user, timeProvider.GetLocalNow(), true));
        log.InfoCircuitOpened(circuit.Id, user, tracker.Count);
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        tracker.SetConnected(circuit.Id, true);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        tracker.SetConnected(circuit.Id, false);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        tracker.Remove(circuit.Id);
        log.InfoCircuitClosed(circuit.Id, tracker.Count);
        return Task.CompletedTask;
    }
}

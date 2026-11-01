namespace HomeManager.Host.Components.Pages;

using HomeManager.Host.Application;
using HomeManager.Host.Application.Circuits;
using HomeManager.Host.Infrastructure.Notifications;

using Microsoft.AspNetCore.Components;
using Microsoft.FeatureManagement;

using MudBlazor;

public sealed partial class Home
{
    private int circuitCount;

    private string? lastNotification;

    private bool HasNotification => !String.IsNullOrEmpty(lastNotification);

    private bool featureEnabled;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required NotificationBus NotificationBus { get; set; }

    [Inject]
    public required CircuitTracker CircuitTracker { get; set; }

    [Inject]
    public required IFeatureManager FeatureManager { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        NotificationBus.Received += OnNotificationReceived;
        CircuitTracker.Changed += OnCircuitChanged;
        circuitCount = CircuitTracker.Count;

        featureEnabled = await FeatureManager.IsEnabledAsync(FeatureFlags.CustomOption);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NotificationBus.Received -= OnNotificationReceived;
            CircuitTracker.Changed -= OnCircuitChanged;
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnCircuitChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(() =>
        {
            circuitCount = CircuitTracker.Count;
            StateHasChanged();
        });
    }

    private void OnNotificationReceived(object? sender, NotificationEventArgs e)
    {
        _ = InvokeAsync(() =>
        {
            lastNotification = e.Message;
            Snackbar.AddInfo(e.Message);
            StateHasChanged();
        });
    }
}

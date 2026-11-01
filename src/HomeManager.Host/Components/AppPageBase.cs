namespace HomeManager.Host.Components;

using HomeManager.Host.Application.Context;

using Microsoft.AspNetCore.Components;

public abstract class AppPageBase : AppComponentBase, IHandleEvent
{
    [Inject]
    private BlazorServiceScope ServiceScope { get; set; } = default!;

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => HandleEventAsync(callback, arg);

    protected async Task HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        using var scope = ServiceScope.Begin(await ServiceScope.PrepareAsync());

        var task = callback.InvokeAsync(arg);
        var shouldAwait = task.Status != TaskStatus.RanToCompletion && task.Status != TaskStatus.Canceled;
        StateHasChanged();
        if (shouldAwait)
        {
            try
            {
                await task;
            }
            catch
            {
                if (task.IsCanceled)
                {
                    return;
                }

                throw;
            }

            StateHasChanged();
        }
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        using var scope = ServiceScope.Begin(await ServiceScope.PrepareAsync());
        await base.SetParametersAsync(parameters);
    }
}

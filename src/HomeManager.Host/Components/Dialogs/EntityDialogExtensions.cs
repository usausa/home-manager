namespace HomeManager.Host.Components.Dialogs;

using MudBlazor;

public static class EntityDialogExtensions
{
    public static async ValueTask ShowEntityDetail(this IDialogService dialog, string entityId)
    {
        var reference = await dialog.ShowAsync<EntityDetailDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(EntityDetailDialog.EntityId), entityId }
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            });
        await reference.Result;
    }
}

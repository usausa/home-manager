namespace HomeManager.Host.Components.Shared;

using MudBlazor;

public static class DialogServiceExtensions
{
    public static async ValueTask ShowInformation(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<MessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(MessageBox.Type), MessageBoxType.Information },
                { nameof(MessageBox.Title), title },
                { nameof(MessageBox.Message), message }
            },
            null);
        await reference.Result;
    }

    public static async ValueTask<bool> ShowConfirm(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<MessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(MessageBox.Type), MessageBoxType.Confirm },
                { nameof(MessageBox.Title), title },
                { nameof(MessageBox.Message), message }
            },
            null);
        var result = await reference.Result;
        return (bool?)result!.Data == true;
    }
}

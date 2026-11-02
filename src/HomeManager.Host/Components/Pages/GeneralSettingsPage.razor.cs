namespace HomeManager.Host.Components.Pages;

using HomeManager.Host.Components.Entities;
using HomeManager.Host.Components.Settings;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 全体設定 (SCR-361)。家の名前・位置・タイムゾーン・単位系・通貨。保存は楽観ロック (ほかの画面が先に保存していたら最新の値を読み直す)
public sealed partial class GeneralSettingsPage
{
    private CoreConfigForm? form;

    private CoreConfigModel model = new();

    private bool busy;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required ICoreConfigService CoreConfig { get; set; }

    [Inject]
    public required ServiceContextProvider ContextProvider { get; set; }

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    private string NowText => EntityFormatter.FormatTime(TimeProvider.GetUtcNow(), CoreConfig.TimeZone);

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        model = CoreConfigModel.From(CoreConfig.Config);
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private async Task OnSaveAsync()
    {
        if ((form is null) || !await form.ValidateAsync())
        {
            return;
        }

        busy = true;
        try
        {
            var status = await CoreConfig.UpdateAsync(model.ToConfig(), ContextProvider.CreateEventContext());
            model = CoreConfigModel.From(CoreConfig.Config);
            if (status == DataWriteStatus.Success)
            {
                Snackbar.AddSuccess("全体設定を保存しました。");
            }
            else
            {
                Snackbar.AddWarning("ほかの画面で全体設定が変わったため、最新の値を読み込みました。確かめてからもう一度保存してください。");
            }
        }
        catch (ServiceValidationException ex)
        {
            Snackbar.AddError(ex.Message);
        }
        finally
        {
            busy = false;
        }
    }

    private void OnResetClick() => model = CoreConfigModel.From(CoreConfig.Config);
}

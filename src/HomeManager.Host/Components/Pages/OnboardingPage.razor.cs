namespace HomeManager.Host.Components.Pages;

using HomeManager.Host.Components.Settings;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 初回セットアップ (SCR-002)。初期パスワードの変更 → 家の情報 → 完了。必要な手順だけを出し、終わるまでほかの画面には進めない (MainLayoutがここへ戻す)
public sealed partial class OnboardingPage
{
    private readonly List<OnboardingStep> steps = [];

    private OnboardingStep step;

    private MudForm? passwordForm;

    private CoreConfigForm? configForm;

    private CoreConfigModel configModel = new();

    private string? currentPassword;

    private string? newPassword;

    private string? confirmPassword;

    private bool busy;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required UserSession Session { get; set; }

    [Inject]
    public required IAccountService AccountService { get; set; }

    [Inject]
    public required ICoreConfigService CoreConfig { get; set; }

    [Inject]
    public required IOnboardingService Onboarding { get; set; }

    [Inject]
    public required ServiceContextProvider ContextProvider { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    private bool IsPasswordStep => step == OnboardingStep.Password;

    private bool IsCoreConfigStep => step == OnboardingStep.CoreConfig;

    private bool IsDoneStep => step == OnboardingStep.Done;

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        await Session.LoadAsync();
        if (!Session.IsOnboardingRequired)
        {
            Navigation.NavigateTo(string.Empty, replace: true);
            return;
        }

        if (Session.Profile?.MustChangePassword ?? false)
        {
            steps.Add(OnboardingStep.Password);
        }

        if (Session.IsAdministrator && !Onboarding.IsDone(OnboardingSteps.CoreConfig))
        {
            steps.Add(OnboardingStep.CoreConfig);
        }

        steps.Add(OnboardingStep.Done);
        step = steps[0];
        configModel = CoreConfigModel.From(CoreConfig.Config);
    }

    //--------------------------------------------------------------------------------
    // Step
    //--------------------------------------------------------------------------------

    private string GetStepTitle(OnboardingStep value)
    {
        var name = value switch
        {
            OnboardingStep.Password => "パスワードの変更",
            OnboardingStep.CoreConfig => "家の情報",
            _ => "完了"
        };
        return String.Create(CultureInfo.InvariantCulture, $"{steps.IndexOf(value) + 1}. {name}");
    }

    private Color GetStepColor(OnboardingStep value)
    {
        var index = steps.IndexOf(value);
        var current = steps.IndexOf(step);
        if (index < current)
        {
            return Color.Success;
        }

        return index == current ? Color.Primary : Color.Default;
    }

    private Variant GetStepVariant(OnboardingStep value) =>
        value == step ? Variant.Filled : Variant.Outlined;

    private void MoveNext() => step = steps[steps.IndexOf(step) + 1];

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private string? ValidateConfirm(string? value) => AccountMessages.ValidateConfirm(value, newPassword);

    private async Task OnPasswordSubmitAsync()
    {
        if (passwordForm is null)
        {
            return;
        }

        await passwordForm.ValidateAsync();
        if (!passwordForm.IsValid || (Session.Profile is null))
        {
            return;
        }

        busy = true;
        try
        {
            var status = await AccountService.ChangePasswordAsync(Session.Profile.Name, currentPassword ?? string.Empty, newPassword ?? string.Empty);
            if (status != PasswordChangeStatus.Success)
            {
                Snackbar.AddError(AccountMessages.GetPasswordError(status));
                return;
            }

            await Session.ReloadAsync();
            currentPassword = null;
            newPassword = null;
            confirmPassword = null;
            MoveNext();
        }
        finally
        {
            busy = false;
        }
    }

    private async Task OnCoreConfigSubmitAsync()
    {
        if ((configForm is null) || !await configForm.ValidateAsync())
        {
            return;
        }

        busy = true;
        try
        {
            var status = await CoreConfig.UpdateAsync(configModel.ToConfig(), ContextProvider.CreateEventContext());
            if (status != DataWriteStatus.Success)
            {
                configModel = CoreConfigModel.From(CoreConfig.Config);
                Snackbar.AddWarning("ほかの画面で全体設定が変わったため、最新の値を読み込みました。確かめてからもう一度保存してください。");
                return;
            }

            await Onboarding.CompleteAsync(OnboardingSteps.CoreConfig);
            MoveNext();
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

    private void OnStartClick() => Navigation.NavigateTo(string.Empty);

    //--------------------------------------------------------------------------------
    // Model
    //--------------------------------------------------------------------------------

    private enum OnboardingStep
    {
        Password,
        CoreConfig,
        Done
    }
}

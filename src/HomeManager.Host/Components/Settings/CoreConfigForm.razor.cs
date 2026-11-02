namespace HomeManager.Host.Components.Settings;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 全体設定の入力欄 (初回セットアップと全体設定の画面で使う)。保存は使う側が行う
public sealed partial class CoreConfigForm
{
    private const int MaxTimeZoneItems = 30;

    private MudForm? form;

    [Parameter]
    [EditorRequired]
    public CoreConfigModel Model { get; set; } = default!;

    // すべての入力欄を確かめる (正しければtrue)
    public async Task<bool> ValidateAsync()
    {
        if (form is null)
        {
            return false;
        }

        await form.ValidateAsync();
        return form.IsValid;
    }

    private static Task<IEnumerable<string>> SearchTimeZoneAsync(string? value, CancellationToken cancellationToken) =>
        Task.FromResult(String.IsNullOrWhiteSpace(value)
            ? TimeZones.All.AsEnumerable()
            : TimeZones.All.Where(x => x.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase)));

    // 小文字で入れても保存時に大文字にそろえるため、大文字にしてから確かめる
    private static string? ValidateCurrency(string? value) =>
        CoreConfigRules.ValidateCurrency(value?.Trim().ToUpperInvariant());

    private static string? ValidateCountry(string? value) =>
        CoreConfigRules.ValidateCountry(value?.Trim().ToUpperInvariant());
}

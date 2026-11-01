namespace HomeManager.Host.Components.Pages;

using System.Text.Json.Nodes;

using HomeManager.Host.Components.Entities;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 開発者ツールのアクション (SCR-202)。サービスを選び、ターゲットとデータを入れて実行し、結果を表示する
public sealed partial class ActionToolPage
{
    private List<ServiceDefinition> services = [];

    private ServiceDefinition? definition;

    private string? selectedService;

    private List<EntityState> candidates = [];

    private List<string> selectedEntities = [];

    private string? dataText;

    private string? resultMessage;

    private Severity resultSeverity = Severity.Success;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required IServiceRegistry ServiceRegistry { get; set; }

    [Inject]
    public required IStateMachine StateMachine { get; set; }

    [Inject]
    public required ServiceContextProvider ContextProvider { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    [SupplyParameterFromQuery(Name = "service")]
    public string? ServiceQuery { get; set; }

    private bool HasTarget => definition?.HasTarget ?? false;

    private IReadOnlyList<ServiceField> Fields => definition?.Fields ?? [];

    private bool HasFields => Fields.Count > 0;

    private bool CanExecute => definition is not null;

    private bool HasResult => resultMessage is not null;

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        services = [.. ServiceRegistry.ListServices()];
        SelectService(ServiceQuery);
    }

    private void SelectService(string? fullName)
    {
        selectedService = fullName;
        definition = services.FirstOrDefault(x => x.FullName == fullName);
        candidates = definition is { HasTarget: true }
            ? [.. StateMachine.GetStates(definition.TargetDomain).OrderBy(static x => x.EntityId, StringComparer.Ordinal)]
            : [];
        selectedEntities = [.. selectedEntities.Where(x => candidates.Any(y => y.EntityId == x))];
        resultMessage = null;
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnServiceChanged(string? value)
    {
        SelectService(value);
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("service", value), replace: true);
    }

    private void OnEntitiesChanged(IEnumerable<string?>? values)
    {
        selectedEntities = [.. values?.OfType<string>() ?? []];
    }

    private void OnExampleClick()
    {
        var json = new JsonObject();
        foreach (var item in Fields)
        {
            if (item.Example is not null)
            {
                json[item.Name] = JsonFormatter.ParseExample(item.Example);
            }
        }

        dataText = JsonFormatter.FormatObject(json);
    }

    private async Task OnExecuteClick()
    {
        if (definition is null)
        {
            return;
        }

        if (!JsonFormatter.TryParseObject(dataText, out var data, out var error))
        {
            ShowResult(Severity.Error, error);
            return;
        }

        try
        {
            var target = HasTarget ? new ServiceTarget([.. selectedEntities]) : null;
            var response = await ServiceRegistry.CallAsync(definition.Domain, definition.Service, data, target, ContextProvider.CreateEventContext());
            ShowResult(Severity.Success, response is null ? $"{definition.FullName} を実行しました。" : $"{definition.FullName} を実行しました。応答: {JsonFormatter.Format(response)}");
        }
        catch (Exception ex) when (ex is ServiceValidationException or ServiceNotFoundException)
        {
            ShowResult(Severity.Error, ex.Message);
        }
    }

    private void ShowResult(Severity severity, string message)
    {
        resultSeverity = severity;
        resultMessage = message;
    }
}

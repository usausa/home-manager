namespace HomeManager.Host.Endpoints;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class StatusResponse
{
    public string Message { get; set; } = default!;

    public string Version { get; set; } = default!;
}

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class StatusEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapStatusEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.V1)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", HandleGet)
            .WithName("StatusGet")
            .Produces<StatusResponse>();
    }

    //--------------------------------------------------------------------------------
    // Get
    //--------------------------------------------------------------------------------

    private static Ok<StatusResponse> HandleGet() =>
        TypedResults.Ok(new StatusResponse
        {
            Message = "API running.",
            Version = typeof(StatusEndpoints).Assembly.GetName().Version?.ToString() ?? string.Empty
        });
}

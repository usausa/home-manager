namespace HomeManager.Host.Application;

using HomeManager.Host.Application.Context;
using HomeManager.Host.Application.Telemetry;

public static class EndpointExtensions
{
    public static RouteGroupBuilder MapApiGroup(this IEndpointRouteBuilder endpoints, string prefix) =>
        endpoints.MapGroup(prefix)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<RequestMetricsEndpointFilter>()
            .AddEndpointFilter<ServiceContextEndpointFilter>();
}

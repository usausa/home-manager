namespace HomeManager.Services;

using System.Text.Json.Nodes;

public sealed class ServiceRegistry : IServiceRegistry
{
    private readonly Lock sync = new();

    private readonly Dictionary<(string Domain, string Service), Registration> services = [];

    private readonly IEventBus bus;

    private readonly TimeProvider timeProvider;

    public ServiceRegistry(IEventBus bus, TimeProvider timeProvider)
    {
        this.bus = bus;
        this.timeProvider = timeProvider;
    }

    public void Register(ServiceDefinition definition, Func<ServiceCall, CancellationToken, ValueTask<object?>> handler)
    {
        if (!EntityIds.IsValidSlug(definition.Domain) || !EntityIds.IsValidSlug(definition.Service))
        {
            throw new ArgumentException($"Invalid service name. service=[{definition.FullName}]", nameof(definition));
        }

        lock (sync)
        {
            services[(definition.Domain, definition.Service)] = new Registration(definition, handler);
        }

        bus.Publish(new ServiceRegisteredEvent(definition.Domain, definition.Service, EventContext.Create(), timeProvider.GetUtcNow()));
    }

    public bool Remove(string domain, string service)
    {
        lock (sync)
        {
            if (!services.Remove((domain, service)))
            {
                return false;
            }
        }

        bus.Publish(new ServiceRemovedEvent(domain, service, EventContext.Create(), timeProvider.GetUtcNow()));
        return true;
    }

    public bool Contains(string domain, string service)
    {
        lock (sync)
        {
            return services.ContainsKey((domain, service));
        }
    }

    public ServiceDefinition? FindDefinition(string domain, string service)
    {
        lock (sync)
        {
            return services.TryGetValue((domain, service), out var registration) ? registration.Definition : null;
        }
    }

    public IReadOnlyList<ServiceDefinition> ListServices()
    {
        lock (sync)
        {
            return [.. services.Values.Select(static x => x.Definition).OrderBy(static x => x.Domain, StringComparer.Ordinal).ThenBy(static x => x.Service, StringComparer.Ordinal)];
        }
    }

    public async ValueTask<object?> CallAsync(
        string domain,
        string service,
        JsonObject? data,
        ServiceTarget? target,
        EventContext context,
        CancellationToken cancel = default)
    {
        Registration? registration;
        lock (sync)
        {
            services.TryGetValue((domain, service), out registration);
        }

        if (registration is null)
        {
            throw new ServiceNotFoundException(domain, service);
        }

        bus.Publish(new ServiceCalledEvent(domain, service, data, target, context, timeProvider.GetUtcNow()));

        var call = new ServiceCall(domain, service, data ?? new JsonObject(), target ?? ServiceTarget.Empty, context);
        return await registration.Handler(call, cancel);
    }

    private sealed record Registration(ServiceDefinition Definition, Func<ServiceCall, CancellationToken, ValueTask<object?>> Handler);
}

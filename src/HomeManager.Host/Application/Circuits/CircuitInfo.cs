namespace HomeManager.Host.Application.Circuits;

public sealed record CircuitInfo(string Id, string? User, DateTimeOffset OpenedAt, bool Connected);

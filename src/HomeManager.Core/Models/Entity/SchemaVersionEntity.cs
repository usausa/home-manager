namespace HomeManager.Models.Entity;

public sealed class SchemaVersionEntity
{
    public int Version { get; set; }

    public string Name { get; set; } = default!;

    public string Checksum { get; set; } = default!;

    public DateTime AppliedAt { get; set; }
}

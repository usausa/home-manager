namespace HomeManager;

using System.Runtime.CompilerServices;

// コンテナランタイムが無いときはスキップするFact
[AttributeUsage(AttributeTargets.Method)]
public sealed class ContainerFactAttribute : FactAttribute
{
    public ContainerFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = ContainerRuntime.Reason;
        SkipUnless = nameof(ContainerRuntime.IsAvailable);
        SkipType = typeof(ContainerRuntime);
    }
}

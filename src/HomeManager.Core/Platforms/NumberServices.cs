namespace HomeManager.Platforms;

internal static class NumberServices
{
    private const string Value = "value";

    public static void Register(IServiceRegistry registry, EntityManager entities)
    {
        registry.Register(
            new ServiceDefinition(Domains.Number, PlatformServices.SetValue)
            {
                Description = "数値を設定する",
                HasTarget = true,
                TargetDomain = Domains.Number,
                Fields = [new(Value, "設定する値") { Required = true, Example = "50" }]
            },
            (call, cancel) =>
            {
                var value = call.GetNumber(Value) ?? throw new ServiceValidationException($"{Value} を指定してください。");
                return entities.CallEntitiesAsync<NumberEntity>(call, (entity, c) => SetValueAsync(entity, value, c), cancel);
            });
    }

    private static ValueTask SetValueAsync(NumberEntity entity, double value, CancellationToken cancel)
    {
        if ((value < entity.NativeMinValue) || (value > entity.NativeMaxValue))
        {
            throw new ServiceValidationException(String.Create(CultureInfo.InvariantCulture, $"{entity.EntityId} は {entity.NativeMinValue}〜{entity.NativeMaxValue} で指定してください。"));
        }

        return entity.SetNativeValueAsync(value, cancel);
    }
}

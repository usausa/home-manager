namespace HomeManager.Platforms;

internal static class SelectServices
{
    private const string Option = "option";

    private const string Cycle = "cycle";

    public static void Register(IServiceRegistry registry, EntityManager entities)
    {
        registry.Register(
            new ServiceDefinition(Domains.Select, PlatformServices.SelectOption)
            {
                Description = "選択肢を選ぶ",
                HasTarget = true,
                TargetDomain = Domains.Select,
                Fields = [new(Option, "選ぶ選択肢") { Required = true }]
            },
            (call, cancel) =>
            {
                var option = call.GetText(Option) ?? throw new ServiceValidationException($"{Option} を指定してください。");
                return entities.CallEntitiesAsync<SelectEntity>(call, (entity, c) => SelectAsync(entity, option, c), cancel);
            });

        registry.Register(
            new ServiceDefinition(Domains.Select, PlatformServices.SelectNext)
            {
                Description = "次の選択肢を選ぶ",
                HasTarget = true,
                TargetDomain = Domains.Select,
                Fields = [new(Cycle, "最後の次は最初に戻る (既定は true)") { Example = "true" }]
            },
            (call, cancel) =>
            {
                var cycle = call.GetFlag(Cycle) ?? true;
                return entities.CallEntitiesAsync<SelectEntity>(call, (entity, c) => MoveAsync(entity, 1, cycle, c), cancel);
            });

        registry.Register(
            new ServiceDefinition(Domains.Select, PlatformServices.SelectPrevious)
            {
                Description = "前の選択肢を選ぶ",
                HasTarget = true,
                TargetDomain = Domains.Select,
                Fields = [new(Cycle, "最初の前は最後に戻る (既定は true)") { Example = "true" }]
            },
            (call, cancel) =>
            {
                var cycle = call.GetFlag(Cycle) ?? true;
                return entities.CallEntitiesAsync<SelectEntity>(call, (entity, c) => MoveAsync(entity, -1, cycle, c), cancel);
            });

        registry.Register(
            new ServiceDefinition(Domains.Select, PlatformServices.SelectFirst)
            {
                Description = "最初の選択肢を選ぶ",
                HasTarget = true,
                TargetDomain = Domains.Select
            },
            (call, cancel) => entities.CallEntitiesAsync<SelectEntity>(call, static (entity, c) => entity.Options.Count > 0 ? entity.SelectOptionAsync(entity.Options[0], c) : ValueTask.CompletedTask, cancel));

        registry.Register(
            new ServiceDefinition(Domains.Select, PlatformServices.SelectLast)
            {
                Description = "最後の選択肢を選ぶ",
                HasTarget = true,
                TargetDomain = Domains.Select
            },
            (call, cancel) => entities.CallEntitiesAsync<SelectEntity>(call, static (entity, c) => entity.Options.Count > 0 ? entity.SelectOptionAsync(entity.Options[^1], c) : ValueTask.CompletedTask, cancel));
    }

    private static ValueTask SelectAsync(SelectEntity entity, string option, CancellationToken cancel)
    {
        if (!entity.Options.Contains(option))
        {
            throw new ServiceValidationException($"{option} は {entity.EntityId} の選択肢にありません。");
        }

        return entity.SelectOptionAsync(option, cancel);
    }

    // 今の選択肢が無いときは、次なら最初・前なら最後にする (HAと同じ)
    private static ValueTask MoveAsync(SelectEntity entity, int offset, bool cycle, CancellationToken cancel)
    {
        var options = entity.Options;
        if (options.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        var index = entity.CurrentOption is { } current ? IndexOf(options, current) : -1;
        int next;
        if (index < 0)
        {
            next = offset > 0 ? 0 : options.Count - 1;
        }
        else
        {
            next = index + offset;
            next = cycle ? (next + options.Count) % options.Count : Math.Clamp(next, 0, options.Count - 1);
        }

        return entity.SelectOptionAsync(options[next], cancel);
    }

    private static int IndexOf(IReadOnlyList<string> options, string value)
    {
        for (var i = 0; i < options.Count; i++)
        {
            if (options[i] == value)
            {
                return i;
            }
        }

        return -1;
    }
}

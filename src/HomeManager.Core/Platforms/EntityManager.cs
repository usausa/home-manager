namespace HomeManager.Platforms;

// エンティティの管理 (HA の EntityPlatform / EntityComponent)。entity_idを決め、状態をステートマシンに書く
public sealed class EntityManager
{
    private readonly Lock sync = new();

    private readonly Dictionary<string, HomeEntity> entities = new(StringComparer.Ordinal);

    private readonly HashSet<string> uniqueIds = new(StringComparer.Ordinal);

    private readonly ILogger<EntityManager> log;

    private readonly IStateMachine stateMachine;

    private readonly TimeProvider timeProvider;

    public EntityManager(
        ILogger<EntityManager> log,
        IStateMachine stateMachine,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.stateMachine = stateMachine;
        this.timeProvider = timeProvider;
    }

    public int Count
    {
        get
        {
            lock (sync)
            {
                return entities.Count;
            }
        }
    }

    internal DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    //--------------------------------------------------------------------------------
    // Query
    //--------------------------------------------------------------------------------

    public HomeEntity? FindEntity(string entityId)
    {
        lock (sync)
        {
            return entities.GetValueOrDefault(entityId);
        }
    }

    public IReadOnlyList<HomeEntity> ListEntities(string? platform = null)
    {
        lock (sync)
        {
            return platform is null ? [.. entities.Values] : [.. entities.Values.Where(x => x.Platform == platform)];
        }
    }

    //--------------------------------------------------------------------------------
    // Add / Remove
    //--------------------------------------------------------------------------------

    // エンティティを追加して最初の状態を書く (HA の async_add_entities)
    public IReadOnlyList<string> AddEntities(string platform, IEnumerable<HomeEntity> values)
    {
        var added = new List<HomeEntity>();
        lock (sync)
        {
            foreach (var entity in values)
            {
                if (entity.Manager is not null)
                {
                    log.WarnEntityAlreadyAdded(platform, entity.EntityId);
                    continue;
                }

                var uniqueId = entity.UniqueId;
                if ((uniqueId is not null) && !uniqueIds.Add(MakeUniqueKey(platform, entity.Domain, uniqueId)))
                {
                    log.WarnEntityDuplicateUniqueId(platform, uniqueId);
                    continue;
                }

                var objectId = EntityIds.Slugify(entity.SuggestedObjectId ?? entity.Name);
                entity.Platform = platform;
                entity.EntityId = GenerateEntityId(entity.Domain, objectId.Length > 0 ? objectId : platform);
                entity.Manager = this;
                entities[entity.EntityId] = entity;
                added.Add(entity);
            }
        }

        foreach (var entity in added)
        {
            WriteState(entity);
        }

        return [.. added.Select(static x => x.EntityId)];
    }

    public bool RemoveEntity(string entityId, EventContext? context = null)
    {
        lock (sync)
        {
            if (!entities.Remove(entityId, out var entity))
            {
                return false;
            }

            Detach(entity, context ?? EventContext.Create());
            return true;
        }
    }

    // インテグレーションの停止で、そのインテグレーションのエンティティをまとめて外す
    public int RemovePlatform(string platform)
    {
        lock (sync)
        {
            var targets = entities.Values.Where(x => x.Platform == platform).ToList();
            var context = EventContext.Create();
            foreach (var entity in targets)
            {
                entities.Remove(entity.EntityId);
                Detach(entity, context);
            }

            return targets.Count;
        }
    }

    private void Detach(HomeEntity entity, EventContext context)
    {
        if (entity.UniqueId is { } uniqueId)
        {
            uniqueIds.Remove(MakeUniqueKey(entity.Platform, entity.Domain, uniqueId));
        }

        entity.Manager = null;
        stateMachine.RemoveState(entity.EntityId, context);
    }

    private static string MakeUniqueKey(string platform, string domain, string uniqueId) =>
        $"{platform}:{domain}:{uniqueId}";

    // 同じentity_idがあれば _2, _3 … を付ける (HAと同じ)
    private string GenerateEntityId(string domain, string objectId)
    {
        var baseId = $"{domain}.{objectId}";
        var candidate = baseId;
        var suffix = 2;
        while (entities.ContainsKey(candidate) || (stateMachine.GetState(candidate) is not null))
        {
            candidate = String.Create(CultureInfo.InvariantCulture, $"{baseId}_{suffix}");
            suffix++;
        }

        if (!EntityIds.IsValid(candidate))
        {
            throw new InvalidOperationException($"Invalid entity id. entityId=[{candidate}]");
        }

        return candidate;
    }

    //--------------------------------------------------------------------------------
    // State
    //--------------------------------------------------------------------------------

    internal void WriteState(HomeEntity entity)
    {
        // エンティティの値はロックの外で集める
        var now = timeProvider.GetUtcNow();
        var available = entity.Available;
        var state = available ? entity.State ?? StateValues.Unknown : StateValues.Unavailable;
        var attributes = entity.BuildAttributes(available);
        var context = entity.GetRecentContext(now) ?? EventContext.Create();

        lock (sync)
        {
            // 外した後に書かれた状態は捨てる
            if (!entities.TryGetValue(entity.EntityId, out var current) || !ReferenceEquals(current, entity))
            {
                return;
            }

            stateMachine.SetState(entity.EntityId, state, attributes, context);
        }
    }

    //--------------------------------------------------------------------------------
    // Service
    //--------------------------------------------------------------------------------

    // 対象のうち型が合い、利用できるエンティティごとに処理を呼ぶ (HA の entity_service_call)
    public async ValueTask CallEntitiesAsync<TEntity>(ServiceCall call, Func<TEntity, CancellationToken, ValueTask> action, CancellationToken cancel)
        where TEntity : HomeEntity
    {
        var targets = new List<TEntity>();
        lock (sync)
        {
            foreach (var entityId in call.Target.EntityIds)
            {
                if (entities.TryGetValue(entityId, out var entity) && (entity is TEntity typed) && entity.Available)
                {
                    targets.Add(typed);
                }
                else
                {
                    log.WarnServiceTargetNotFound(call.Domain, call.Service, entityId);
                }
            }
        }

        var now = timeProvider.GetUtcNow();
        foreach (var entity in targets)
        {
            entity.SetContext(call.Context, now);
            await action(entity, cancel);
        }
    }
}

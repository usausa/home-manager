namespace HomeManager.Host.Application;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using HomeManager.Accessors;
using HomeManager.Host.Application.Context;
using HomeManager.Host.Application.ExceptionHandling;
using HomeManager.Host.Application.HealthChecks;
using HomeManager.Host.Application.Telemetry;
using HomeManager.Host.Components;
using HomeManager.Host.Endpoints;
using HomeManager.Host.Infrastructure.Logging;
using HomeManager.Host.Infrastructure.Security;
using HomeManager.Infrastructure.Security;
using HomeManager.Integration.Demo;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using MiniDataProfiler;
using MiniDataProfiler.Listener.Logging;
using MiniDataProfiler.Listener.OpenTelemetry;

using MudBlazor;
using MudBlazor.Services;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

using Smart.Data;

public static class ApplicationExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string MigrationPath = "Assets/Data/Migrations";

    //--------------------------------------------------------------------------------
    // System
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureSystem(this WebApplicationBuilder builder)
    {
        // Path
        builder.Configuration.SetBasePath(AppContext.BaseDirectory);

        // Encoding
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Host
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHost(this WebApplicationBuilder builder)
    {
        // Service
        builder.Services
            .AddWindowsService()
            .AddSystemd();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Logging
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureLogging(this IHostApplicationBuilder builder)
    {
        var setting = builder.Configuration.GetSection("Log").Get<LogSetting>()!;
        var useOtlpExporter = builder.Configuration.IsOtelExporterEnabled();

        // Application log
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(
            (provider, options) =>
            {
                var accessor = provider.GetRequiredService<IHttpContextAccessor>();
                options.ReadFrom.Configuration(builder.Configuration);
                options.Enrich.With(new CallbackEnricher("RemoteIpAddress", () => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString()));
                options.Enrich.With(new CallbackEnricher("UserId", () => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)));
            },
            writeToProviders: useOtlpExporter);

        // HTTP log
        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod |
                                    HttpLoggingFields.RequestPath |
                                    HttpLoggingFields.ResponseStatusCode |
                                    HttpLoggingFields.Duration;
            if (setting.HttpDump)
            {
                options.LoggingFields |= HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody;
                options.CombineLogs = true;
                options.RequestBodyLogLimit = setting.HttpDumpLimit;
                options.ResponseBodyLogLimit = setting.HttpDumpLimit;
                options.MediaTypeOptions.Clear();
                options.MediaTypeOptions.AddText("application/json");
                options.MediaTypeOptions.AddText("application/*+json");
                options.MediaTypeOptions.AddText("application/xml");
                options.MediaTypeOptions.AddText("application/*+xml");
            }
        });

        // Access log (W3C)
        if (setting.W3CLog.Enable)
        {
            builder.Services.AddW3CLogging(options =>
            {
                options.LogDirectory = setting.W3CLog.Directory;
                options.FileName = setting.W3CLog.FileName;
                options.RetainedFileCountLimit = setting.W3CLog.RetainedFileCount;
            });
        }

        return builder;
    }

    public static WebApplication UseW3CLog(this WebApplication app)
    {
        var setting = app.Services.GetRequiredService<LogSetting>();
        if (setting.W3CLog.Enable)
        {
            app.UseW3CLogging();
        }

        return app;
    }

    public static WebApplication UseHttpLog(this WebApplication app)
    {
        var setting = app.Services.GetRequiredService<LogSetting>();
        if (setting.HttpLog)
        {
            app.UseWhen(
                static context => context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase),
                static b => b.UseHttpLogging());
        }

        return app;
    }

    //--------------------------------------------------------------------------------
    // Http
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHttp(this IHostApplicationBuilder builder)
    {
        // Add services to the container
        builder.Services.AddHttpContextAccessor();

        // CSP nonce
        builder.Services.AddScoped<CspNonce>();

        // XForward
        builder.Services.Configure<ForwardedHeadersOptions>(static options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Do not restrict to local network/proxy
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return builder;
    }

    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        // HSTS
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        // Headers. The nonce admits the import map that Blazor renders inline, MudBlazor needs inline styles,
        // dotnet watch / Browser Link load their script from another localhost port and connect back to it
        var development = app.Environment.IsDevelopment();
        var scriptSources = development ? "'self' http://localhost:*" : "'self'";
        var connectSources = development ? "'self' http://localhost:* ws://localhost:* wss://localhost:*" : "'self'";
        app.UseMiddleware<SecurityHeadersMiddleware>(new SecurityHeadersOption
        {
            ReportOnly = app.Services.GetRequiredService<CspSetting>().ReportOnly,
            ContentSecurityPolicy = $"default-src 'self'; base-uri 'self'; object-src 'none'; form-action 'self'; frame-ancestors 'none'; img-src 'self' data:; font-src 'self'; style-src 'self' 'unsafe-inline'; script-src {scriptSources} 'nonce-{{nonce}}'; connect-src {connectSources}"
        });

        return app;
    }

    //--------------------------------------------------------------------------------
    // API
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureApi(this IHostApplicationBuilder builder)
    {
        // JSON
        builder.Services.ConfigureHttpJsonOptions(static options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = NamingPolicy.JsonPropertyNaming;
            options.SerializerOptions.DictionaryKeyPolicy = NamingPolicy.JsonDictionaryKeyNaming;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
            options.SerializerOptions.AllowDuplicateProperties = false;
            options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        });

        // Validation
        builder.Services.AddValidation();

        // Error handler
        builder.Services.AddProblemDetails(static options =>
        {
            options.CustomizeProblemDetails = static context =>
            {
                context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            };
        });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }

    public static WebApplication UseErrorHandler(this WebApplication app)
    {
        // API: ProblemDetails
        app.UseWhen(
            static context => context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase),
            static b =>
            {
                b.UseExceptionHandler();
                b.UseStatusCodePages();
            });

        // Page: error page
        app.UseWhen(
            static context => !context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase),
            static b =>
            {
                b.UseExceptionHandler("/error", createScopeForErrors: true);
            });

        return app;
    }

    //--------------------------------------------------------------------------------
    // Authentication
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureAuthentication(this IHostApplicationBuilder builder)
    {
        var setting = builder.Configuration.GetSection("Auth").Get<AuthSetting>()!;
        var isDevelopment = builder.Environment.IsDevelopment();

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(setting.ExpireMinutes);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

                // API returns status code instead of redirect
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = static context =>
                    {
                        if (context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        }
                        else
                        {
                            context.Response.Redirect(context.RedirectUri);
                        }

                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = static context =>
                    {
                        if (context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        }
                        else
                        {
                            context.Response.Redirect(context.RedirectUri);
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(static options =>
        {
            options.AddPolicy(Policies.Administrator, static policy => policy.RequireRole(Roles.Administrator));
        });

        builder.Services.AddCascadingAuthenticationState();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Compress
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureCompression(this IHostApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(static options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        builder.Services.AddRequestDecompression();

        return builder;
    }

    public static WebApplication UseCompression(this WebApplication app)
    {
        var setting = app.Services.GetRequiredService<CompressionSetting>();
        if (setting.Response || setting.Request)
        {
            app.UseWhen(
                static context => context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase),
                b =>
                {
                    if (setting.Response)
                    {
                        b.UseResponseCompression();
                    }

                    if (setting.Request)
                    {
                        b.UseRequestDecompression();
                    }
                });
        }

        return app;
    }

    //--------------------------------------------------------------------------------
    // OpenApi
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureOpenApi(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(static options =>
        {
            options.AddDocumentTransformer(static (document, _, _) =>
            {
                document.Info.Title = "HomeManager API";
                document.Info.Version = "v1";
                document.Info.Description = "HomeManager API.";
                return Task.CompletedTask;
            });
        });

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Blazor
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureBlazor(this IHostApplicationBuilder builder)
    {
        // Razor components
        builder.Services
            .AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                options.DetailedErrors = builder.Environment.IsDevelopment();
            });

        // Error boundary logging
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.Web.IErrorBoundaryLogger, ErrorBoundaryLogger>();

        // Circuit tracking
        builder.Services.AddSingleton<Circuits.CircuitTracker>();
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, Circuits.AppCircuitHandler>();

        // Entity operation (画面からの機器の操作)
        builder.Services.AddScoped<Components.Entities.EntityOperations>();

        // User session (回線ごとのログイン中のユーザー)
        builder.Services.AddScoped<Components.UserSession>();

        // MudBlazor
        builder.Services.AddMudServices(static options =>
        {
            options.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            options.SnackbarConfiguration.PreventDuplicates = true;
            options.SnackbarConfiguration.NewestOnTop = false;
            options.SnackbarConfiguration.ShowCloseIcon = true;
            options.SnackbarConfiguration.VisibleStateDuration = 5000;
            options.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
        });

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Health
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHealth(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", static () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck<DatabaseHealthCheck>("database");

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Telemetry
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureTelemetry(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = builder.Configuration.IsOtelExporterEnabled();

        var prometheusSection = builder.Configuration.GetSection("Prometheus");
        var prometheusUri = prometheusSection.GetValue<string>("Uri")!;
        var usePrometheusExporter = !String.IsNullOrEmpty(prometheusUri);

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(config =>
            {
                config.AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
                    serviceInstanceId: Environment.MachineName);
            });

        // Log
        if (useOtlpExporter)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
            builder.Services.Configure<OpenTelemetryLoggerOptions>(static logging =>
            {
                logging.AddOtlpExporter();
            });
        }

        // Metrics
        if (useOtlpExporter || usePrometheusExporter)
        {
            telemetry
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddApplicationInstrumentation();

                    if (useOtlpExporter)
                    {
                        metrics.AddOtlpExporter();
                    }

                    if (usePrometheusExporter)
                    {
                        var prometheusEndpoint = new Uri(prometheusUri);
                        metrics.AddPrometheusHttpListener(config =>
                        {
                            config.Host = prometheusEndpoint.Host;
                            config.Port = prometheusEndpoint.Port;
                        });
                    }
                });
        }

        // Trace
        if (useOtlpExporter)
        {
            telemetry
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation(static options =>
                        {
                            options.Filter = static context =>
                            {
                                var path = context.Request.Path;
                                return !path.StartsWithSegments(AlivenessEndpointPath, StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments(HealthEndpointPath, StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/redoc", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase);
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .AddMiniDataProfilerInstrumentation()
                        .AddApplicationInstrumentation();

                    tracing.AddOtlpExporter();
                });
        }

        // Custom instrument
        builder.Services.AddApplicationInstrument();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Components
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureComponents(this IHostApplicationBuilder builder)
    {
        // System
        builder.Services.AddSingleton(TimeProvider.System);

        // Data
        builder.Services.AddSingleton<IDbProvider>(static p =>
        {
            var configuration = p.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Default");

            var listener = CreateProfileListener(p, p.GetRequiredService<ProfilerSetting>());
            if (listener is not null)
            {
                return new DelegateDbProvider(() => new ProfileDbConnection(listener, new SqliteConnection(connectionString)));
            }

            return new DelegateDbProvider(() => new SqliteConnection(connectionString));
        });
        builder.Services.AddSingleton<IDialect>(new DelegateDialect(
            static ex => ex is SqliteException { SqliteExtendedErrorCode: 1555 or 2067 },
            static x => Regex.Replace(x, @"[%_\\]", @"\$0")));
        builder.Services.AddDataAccessors(typeof(AccountAccessor).Assembly);

        // Security
        builder.Services.AddSingleton(new DefaultPasswordProviderOptions());
        builder.Services.AddSingleton<IPasswordProvider, DefaultPasswordProvider>();

        // Service
        builder.Services.AddSingleton<ApplicationServiceContextProvider>();
        builder.Services.AddSingleton<ServiceContextProvider>(static p => p.GetRequiredService<ApplicationServiceContextProvider>());
        builder.Services.AddScoped<BlazorServiceScope>();

        builder.Services.AddCoreServices();
        builder.Services.AddCoreServiceInterfaces();

        // Core (イベントバス・ステートマシン・サービス・エンティティ・インテグレーション)
        builder.Services.AddCoreRuntime();

        // Integration (1インテグレーション = 1プロジェクト。P1は仮想デバイスだけ)
        builder.Services.AddDemoIntegration();
        builder.Services.AddHostedService<Workers.IntegrationWorker>();

        // Setting
        builder.Services.AddOptions<ProfilerSetting>().BindConfiguration("Profiler").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<ProfilerSetting>>().Value);
        builder.Services.AddOptions<LogSetting>().BindConfiguration("Log").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<LogSetting>>().Value);
        builder.Services.AddOptions<CompressionSetting>().BindConfiguration("Compression").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<CompressionSetting>>().Value);
        builder.Services.AddOptions<CspSetting>().BindConfiguration("Csp").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<CspSetting>>().Value);
        builder.Services.AddOptions<AuthSetting>().BindConfiguration("Auth").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<AuthSetting>>().Value);
        builder.Services.AddOptions<TelemetrySetting>().BindConfiguration("Telemetry").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<TelemetrySetting>>().Value);

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Information
    //--------------------------------------------------------------------------------

    public static void LogStartupInformation(this WebApplication app)
    {
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);

        var prometheusSection = app.Configuration.GetSection("Prometheus");
        var prometheusUri = prometheusSection.GetValue("Uri", string.Empty);

        app.Logger.InfoServiceStart();
        app.Logger.InfoServiceSettingsRuntime(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
        app.Logger.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.CurrentDirectory);
        app.Logger.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
        app.Logger.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);
        app.Logger.InfoServiceSettingsTelemetry(app.Configuration.GetOtelExporterEndpoint(), prometheusUri);
    }

    //--------------------------------------------------------------------------------
    // End point
    //--------------------------------------------------------------------------------

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // Develop
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            // [MEMO] Add yaml support
            app.MapOpenApi("/openapi/{documentName}.yaml");

            // NSwag UI (SwaggerUI / ReDoc) using MapOpenApi generated specification
            app.UseSwaggerUi(static options =>
            {
                options.DocumentPath = "/openapi/v1.json";
            });
            app.UseReDoc(static options =>
            {
                options.Path = "/redoc";
                options.DocumentPath = "/openapi/v1.json";
            });
        }

        // Static assets
        app.MapStaticAssets();

        // Blazor
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode(static options =>
            {
                options.ContentSecurityFrameAncestorsPolicy = "'none'";
            });

        // Auth
        app.MapAuthEndpoints();

        // API
        app.MapStatusEndpoints();

        // Health
        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = static r => r.Tags.Contains("live")
        });

        return app;
    }

    //--------------------------------------------------------------------------------
    // Startup
    //--------------------------------------------------------------------------------

    public static async ValueTask InitializeApplicationAsync(this WebApplication app)
    {
        // Prepare instrument
        app.Services.GetRequiredService<ApplicationInstrument>();

        // Prepare database (create the data directory and apply numbered migration files)
        PrepareDatabaseDirectory(app.Configuration);
        await app.Services.GetRequiredService<DatabaseService>().MigrateAsync(MigrationPath, CancellationToken.None);

        // Seed initial account
        var setting = app.Services.GetRequiredService<AuthSetting>();
        await app.Services.GetRequiredService<AccountService>().InitializeAsync(setting.InitialAccount, Roles.Administrator, CancellationToken.None);

        // Load core config and onboarding progress (the default core config is saved on first startup)
        await app.Services.GetRequiredService<ICoreConfigService>().LoadAsync(EventContext.Create(), CancellationToken.None);
        await app.Services.GetRequiredService<IOnboardingService>().LoadAsync(CancellationToken.None);

        // Prepare system information (uptime starts here)
        app.Services.GetRequiredService<ISystemInfoService>();
    }

    // SQLiteはDBファイルのフォルダーを作らないため、先に作っておく
    private static void PrepareDatabaseDirectory(IConfiguration configuration)
    {
        var builder = new SqliteConnectionStringBuilder(configuration.GetConnectionString("Default"));
        if ((builder.Mode == SqliteOpenMode.Memory) || String.IsNullOrEmpty(builder.DataSource) || (builder.DataSource == ":memory:"))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
        if (!String.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    //--------------------------------------------------------------------------------
    // Profiler
    //--------------------------------------------------------------------------------

    private static IProfileListener? CreateProfileListener(IServiceProvider provider, ProfilerSetting setting)
    {
        var listeners = new List<IProfileListener>();
        if (setting.SqlLog.Enable)
        {
            var option = new LoggingListenerOption
            {
                OutputParameter = setting.SqlLog.OutputParameter,
                ElapsedThreshold = TimeSpan.FromMilliseconds(setting.SqlLog.ElapsedThresholdMilliseconds)
            };
            listeners.Add(new LoggingListener(provider.GetRequiredService<ILogger<LoggingListener>>(), option));
        }

        if (setting.SqlTelemetry.Enable)
        {
            listeners.Add(new OpenTelemetryListener(new OpenTelemetryListenerOption()));
        }

        return listeners.Count switch
        {
            0 => null,
            1 => listeners[0],
            _ => new ChainListener(listeners)
        };
    }

    //--------------------------------------------------------------------------------
    // Configuration
    //--------------------------------------------------------------------------------

    private static bool IsOtelExporterEnabled(this IConfiguration configuration) =>
        !String.IsNullOrWhiteSpace(configuration.GetOtelExporterEndpoint());

    private static string GetOtelExporterEndpoint(this IConfiguration configuration) =>
        configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? string.Empty;
}

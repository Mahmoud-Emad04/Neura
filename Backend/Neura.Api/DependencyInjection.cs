using CloudinaryDotNet;
using Ganss.Xss;
using Hangfire;
using HashidsNet;
using Infrastructure.Services.Community;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Neura.Api.Errors;
using Neura.Api.Infrastructure;
using Neura.Api.OpenApiTransformers;
using Neura.Core.Authentication;
using Neura.Core.Settings;
using Neura.Repository.Persistence;
using Neura.Services.Authentication;
using Neura.Services.Extensions;
using Neura.Services.Helpers;
using Neura.Services.Jobs;
using Neura.Services.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Reflection;
using System.Text;

namespace Neura.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddControllers();

        services.AddEndpoints(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            Assembly.GetExecutingAssembly(),
            typeof(GradingService).Assembly));

        services.AddHybridCache();

        services.AddHttpClient();

        services.AddCors(options =>
            options.AddDefaultPolicy(builder =>
                    builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .SetIsOriginAllowed(origin => true)
                        .AllowCredentials()
            // .WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>()!)
            ));
        services.AddAuth(configuration);

        services.AddDatabase(configuration);

        services.AddFluentValidation();

        services.AddMapster(Assembly.GetExecutingAssembly(), typeof(Course).Assembly, typeof(GradingService).Assembly);

        services.AddProblemDetails();

        services.AddHangfire(configuration);

        services.AddOpenApiServices();

        services.AddOpenTelemetryServices(configuration);

        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHttpContextAccessor();

        services.AddDataProtection().SetApplicationName(nameof(Neura));

        services.AddSingleton<IHashids>(_ => new Hashids(configuration["Hashids:Course"], 11));

        services.AddOptions<MailSettings>()
            .BindConfiguration(nameof(MailSettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CloudinarySettings>()
            .BindConfiguration(CloudinarySettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<StripeSettings>()
            .BindConfiguration(StripeSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSignalR(configuration, environment);

        #region AddInjection

        services.AddScoped<ExamTimeoutJob>();
        services.AddScoped<NotifyVideoProcessorJob>();


        services.AddScoped<IFileService, FileService>();

        services.AddScoped<IEmailSender, EmailService>();




        services.AddScoped<IGradingService, GradingService>();

        services.AddScoped<IServiceHelpers, ServiceHelpers>();



        services.AddScoped<IExamTimeoutService, ExamTimeoutService>();

        services.AddScoped<IStripeService, StripeService>();

        services.AddScoped<ICoursePermissionService, CoursePermissionService>();


        services.AddSingleton<HtmlSanitizer>(sp =>
        {
            var sanitizer = new HtmlSanitizer();
            // Customize allowed tags/attributes if needed
            // sanitizer.AllowedTags.Add("img");
            return sanitizer;
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddOptions<ExternalVideoProcessorSettings>()
            .BindConfiguration(ExternalVideoProcessorSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IExternalVideoProcessor, ExternalVideoProcessorService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalVideoProcessorSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
        });

        services.AddOptions<ChatbotSettings>()
            .BindConfiguration("Chatbot")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IChatbotService, ChatbotService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChatbotSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
        });


        #endregion

        // Register Cloudinary
        var cloudinarySettings = new CloudinarySettings();
        configuration.GetSection(CloudinarySettings.SectionName).Bind(cloudinarySettings);

        if (!cloudinarySettings.IsValid())
            throw new InvalidOperationException("Cloudinary settings are not properly configured in appsettings.json");

        var cloudinaryAccount = new Account(
            cloudinarySettings.CloudName,
            cloudinarySettings.ApiKey,
            cloudinarySettings.ApiSecret);

        var cloudinary = new Cloudinary(cloudinaryAccount);
        services.AddSingleton(cloudinary);
        services.AddSingleton(cloudinarySettings);


        services.Configure<KestrelServerOptions>(options =>
        {
            // Remove limit on body size (for Uploads)
            options.Limits.MaxRequestBodySize = 25 * 1024 * 1024;
        });

        services.AddNeuraAuthorization();

        return services;
    }

    private static IServiceCollection AddOpenApiServices(this IServiceCollection services)
    {
        services.AddOpenApi(options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Neura API", Version = "v1" });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
        });


        return services;
    }


    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        return services;
    }

    private static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    private static IServiceCollection AddMapster(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        var mappingConfiguration = TypeAdapterConfig.GlobalSettings;
        mappingConfiguration.Scan(assembliesToScan);
        services.AddSingleton<IMapper>(new Mapper(mappingConfiguration));
        return services;
    }


    private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        //services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();
        //services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();


        services.AddScoped<IJwtProvider, JwtProvider>();

        //services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtSettings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.SaveToken = true;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.Key!)),
                    ValidIssuer = jwtSettings?.Issuer,
                    ValidAudience = jwtSettings?.Audience
                };
                o.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Only activate for SignalR hub paths
                        var path = context.HttpContext.Request.Path;

                        if (!path.StartsWithSegments("/hubs"))
                            return Task.CompletedTask;

                        // Pull token from query string (SignalR JS client convention)
                        var accessToken = context.Request.Query["access_token"];

                        if (!string.IsNullOrWhiteSpace(accessToken))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();

                        logger.LogWarning(
                            "JWT authentication failed: {Error}",
                            context.Exception.Message);

                        return Task.CompletedTask;
                    }
                };
            })

            .AddGoogle(options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
                options.CallbackPath = "/signin-google";
                options.ClaimActions.MapJsonKey("picture", "picture", "url");
            }).AddGitHub(options =>
            {
                options.ClientId = configuration["Authentication:GitHub:ClientId"]!;
                options.ClientSecret = configuration["Authentication:GitHub:ClientSecret"]!;

                // This must match the URL you put in GitHub Developer Settings
                options.CallbackPath = "/signin-github";

                // Required to get the user's email address
                options.Scope.Add("user:email");
            });

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = true;
        });

        return services;
    }

    private static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration Configuration)
    {
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection")));

        services.AddHangfireServer();

        return services;
    }
    private static IServiceCollection AddSignalR(this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var signalRBuilder = services.AddSignalR(options =>
        {
            // Ping every 15s — client must respond within 30s.
            // If the client misses 2 pings, OnDisconnectedAsync fires.
            // This is how we detect silent disconnects (phone screen off,
            // network drop, etc.) rather than waiting for a TCP RST.
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);

            // Reject payloads > 32 KB.
            // A message with 4,000 chars + sender metadata is ~6 KB.
            // 32 KB gives comfortable headroom while preventing abuse.
            options.MaximumReceiveMessageSize = 32 * 1024;

            // Enable detailed errors in Development only.
            // In Production, Hub exceptions return a generic message to
            // the client — stack traces never leave the server.
            options.EnableDetailedErrors = environment.IsDevelopment();
        });

        // ── Redis Backplane (Production / Staging only) ───────────────────────
        var redisConnection = configuration.GetConnectionString("Redis");

        if (!environment.IsDevelopment() && !string.IsNullOrWhiteSpace(redisConnection))
        {
            signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
            {
                options.Configuration.ChannelPrefix =
                    RedisChannel.Literal("community-hub");

                options.Configuration.ReconnectRetryPolicy =
                    new ExponentialRetry(deltaBackOffMilliseconds: 1_000);
            });
        }

        // ── Presence Tracker DI ───────────────────────────────────────────────
        // Phase 1 (Development + single server): in-memory
        // Phase 2 (Production + multi-server):   Redis
        //
        // MUST be Singleton — presence state must survive across HTTP requests.
        // A Scoped registration would create a fresh empty dictionary per request,
        // destroying all tracked connections instantly.
        if (environment.IsDevelopment())
        {
            services.AddSingleton<IPresenceTracker, InMemoryPresenceTracker>();
        }
        else
        {
            // Phase 2: swap this line when RedisPresenceTracker is ready
            services.AddSingleton<IPresenceTracker, InMemoryPresenceTracker>();
            // services.AddSingleton<IPresenceTracker, RedisPresenceTracker>();
        }


        // IPresenceTracker — MUST be Singleton.
        // A Scoped or Transient registration would give each request a
        // fresh empty dictionary, destroying all connection state.
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IVoiceChannelService, VoiceChannelService>();
        services.AddSingleton<IPresenceTracker, InMemoryPresenceTracker>();
        return services;
    }
    private static IServiceCollection AddRedis(this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConnection));
        }
        return services;
    }

    private static IServiceCollection AddOpenTelemetryServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "Neura.Api";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("Neura.Api")
                .AddPrometheusExporter());

        return services;
    }
}
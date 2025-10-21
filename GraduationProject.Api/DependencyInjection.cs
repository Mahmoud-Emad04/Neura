using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using GraduationProject.Api.Errors;
using GraduationProject.Api.Mapping;
using GraduationProject.Core.Authentication;
using GraduationProject.Core.Service;
using GraduationProject.Repository.Persistence;
using GraduationProject.Services.Authentication;
using GraduationProject.Services.Services;
using HashidsNet;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace GraduationProject.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();

        services.AddHybridCache();

        services.AddCors(options =>
            options.AddDefaultPolicy(builder =>
                    builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowAnyOrigin()
                // .WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>()!)
            ));

        services.AddAuth(configuration);

        services.AddDatabase(configuration);

        services.AddSwagger();

        services.AddFluentValidation();

        services.AddMapster();

        services.AddProblemDetails();

        services.AddHangfire(configuration);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        
        services.AddDataProtection().SetApplicationName(nameof(GraduationProject));
        services.AddSingleton<IHashids>(_ => new Hashids("f1nd1ngn3m0", minHashLength: 11));

        #region AddInjection

        services.AddScoped<IAuthService , AuthService>();
        services.AddScoped<ICourseService,CourseService>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        #endregion

        
        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    private static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    private static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    private static IServiceCollection AddMapster(this IServiceCollection services)
    {
        var mappingConfiguration = TypeAdapterConfig.GlobalSettings;
        mappingConfiguration.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton<IMapper>(new Mapper(mappingConfiguration));
        return services;
    }


    private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        
        
        services.AddSingleton<IJwtProvider, JwtProvider>();
        
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
            });
        
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 8;
            //options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = true;
        });
        return services;
    }

    private static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration Configuration)
    {
        // services.AddHangfire(configuration => configuration
        //     .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        //     .UseSimpleAssemblyNameTypeSerializer()
        //     .UseRecommendedSerializerSettings()
        //     .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection")));
        //
        // services.AddHangfireServer();

        return services;
    }
}
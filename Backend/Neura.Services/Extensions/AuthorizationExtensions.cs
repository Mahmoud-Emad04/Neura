using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Neura.Core.Abstractions.Consts;
using Neura.Services.Authorization.Handlers;
using Neura.Services.Authorization.Providers;

namespace Neura.Services.Extensions;

public static class AuthorizationExtensions
{
    /// <summary>
    ///     Adds Neura permission-based authorization services
    /// </summary>
    public static IServiceCollection AddNeuraAuthorization(this IServiceCollection services)
    {
        // IMPORTANT: Register the custom policy provider as singleton FIRST
        // This replaces the default provider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, GlobalRoleAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, CoursePermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, LessonPermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, SectionPermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ExamPermissionAuthorizationHandler>();

        // Add authorization with some predefined policies (optional, for direct use)
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireMember", policy =>
                policy.RequireRole(DefaultRoles.Member, DefaultRoles.Instructor, DefaultRoles.Admin,
                    DefaultRoles.SuperAdmin))
            .AddPolicy("RequireInstructor", policy =>
                policy.RequireRole(DefaultRoles.Instructor, DefaultRoles.Admin, DefaultRoles.SuperAdmin))
            .AddPolicy("RequireAdmin", policy =>
                policy.RequireRole(DefaultRoles.Admin, DefaultRoles.SuperAdmin))
            .AddPolicy("RequireSuperAdmin", policy =>
                policy.RequireRole(DefaultRoles.SuperAdmin));

        return services;
    }
}
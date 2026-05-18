using System.Reflection;

namespace Neura.Api.Infrastructure;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var type in endpointTypes)
        {
            services.AddTransient(typeof(IEndpoint), type);
        }

        return services;
    }

    public static IApplicationBuilder MapEndpoints(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();

        var routeGroup = app.MapGroup("").WithOpenApi()
                            .AddEndpointFilter<ValidationFilter>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(routeGroup);
        }

        return app;
    }
}

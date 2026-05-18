using Microsoft.AspNetCore.Identity;
using Neura.Api.Infrastructure;

namespace Neura.Api.Features.Auth.ExternalLogin;

public sealed class ExternalLoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("auth/external-login/{provider}", (
            string provider,
            SignInManager<ApplicationUser> signInManager,
            HttpRequest request) =>
        {
            var allowed = new[] { "Google", "GitHub" };
            if (!allowed.Contains(provider, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "unsupported_provider" });

            var redirectUrl = $"{request.Scheme}://{request.Host}/auth/external-callback";
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return Results.Challenge(properties, new[] { provider });
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("ExternalLogin");
    }
}

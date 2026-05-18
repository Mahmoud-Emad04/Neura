using MediatR;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.RevokeToken;

public sealed class RevokeTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/revoke", async (
            RefreshTokenRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new RevokeTokenCommand(request);
            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok()
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("RevokeToken");
    }
}

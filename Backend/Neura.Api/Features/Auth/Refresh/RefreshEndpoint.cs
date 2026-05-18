using MediatR;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.Refresh;

public sealed class RefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/refresh", async (
            RefreshTokenRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new RefreshCommand(request);
            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("Refresh");
    }
}

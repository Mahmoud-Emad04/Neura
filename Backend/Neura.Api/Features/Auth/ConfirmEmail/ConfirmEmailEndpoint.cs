using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/confirm-email", async (
            ConfirmEmailRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new ConfirmEmailCommand(request);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("ConfirmEmail");
    }
}

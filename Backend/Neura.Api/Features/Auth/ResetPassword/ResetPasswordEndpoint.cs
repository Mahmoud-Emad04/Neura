using MediatR;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.ResetPassword;

public sealed class ResetPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/reset-password", async (
            ResetPasswordRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new ResetPasswordCommand(request);
            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok()
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("ResetPassword");
    }
}

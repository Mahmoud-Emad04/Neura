using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Extensions;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Users;

namespace Neura.Api.Features.Account.ChangePassword;

public sealed class ChangePasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("me/password", async (
            [FromBody] ChangePasswordRequest request,
            HttpContext context,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = context.User.GetUserId()!;

            var command = new ChangePasswordCommand(userId, request);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.NoContent() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Account")
        .WithName("ChangePassword");
    }
}

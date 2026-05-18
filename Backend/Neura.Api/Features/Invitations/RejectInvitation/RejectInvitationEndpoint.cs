using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using System.Security.Claims;

namespace Neura.Api.Features.Invitations.RejectInvitation;

public sealed class RejectInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/invitations/{token}/reject", async (
            string token,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            var command = new RejectInvitationCommand(token, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(new { message = "Invitation rejected successfully" }) 
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .WithTags("Invitations")
        .WithName("RejectInvitation");
    }
}

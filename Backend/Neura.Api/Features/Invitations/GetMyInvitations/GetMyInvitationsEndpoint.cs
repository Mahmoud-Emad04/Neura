using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.CourseTeam;
using System.Security.Claims;

namespace Neura.Api.Features.Invitations.GetMyInvitations;

public sealed class GetMyInvitationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/invitations/my", async (
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userEmail = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail)) return Results.Ok(new MyInvitationsResponse());

            var query = new GetMyInvitationsQuery(userEmail);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Invitations")
        .WithName("GetMyInvitations");
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Enums;
using System.Security.Claims;

namespace Neura.Api.Features.CourseTeam.CancelInvitation;

public sealed class CancelInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("courses/{courseId:int}/team/invitations/{invitationId:int}", async (
            int courseId,
            int invitationId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var requesterId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new CancelInvitationCommand(courseId, invitationId, requesterId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.NoContent() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.ManageTeam))
        .WithTags("CourseTeam")
        .WithName("CancelInvitation");
    }
}

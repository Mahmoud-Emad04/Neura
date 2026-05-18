using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;
using System.Security.Claims;

namespace Neura.Api.Features.CourseTeam.InviteTeamMember;

public sealed class InviteTeamMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/courses/{courseId:int}/team/invite", async (
            int courseId,
            [FromBody] InviteTeamMemberRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var inviterId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new InviteTeamMemberCommand(courseId, request, inviterId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.CreatedAtRoute("GetPendingInvitations", new { courseId }, result.Value)
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.ManageTeam))
        .WithTags("CourseTeam")
        .WithName("InviteTeamMember");
    }
}

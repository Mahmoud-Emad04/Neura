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

namespace Neura.Api.Features.CourseTeam.ChangeTeamRole;

public sealed class ChangeTeamRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("api/courses/{courseId:int}/team/members/{userId}/role", async (
            int courseId,
            string userId,
            [FromBody] ChangeTeamRoleRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var requesterId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new ChangeTeamRoleCommand(courseId, userId, request, requesterId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.ManageTeam))
        .WithTags("CourseTeam")
        .WithName("ChangeTeamRole");
    }
}

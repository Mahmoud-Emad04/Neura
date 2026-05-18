using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Enums;

namespace Neura.Api.Features.CourseTeam.GetPendingInvitations;

public sealed class GetPendingInvitationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/{courseId:int}/team/invitations", async (
            int courseId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetPendingInvitationsQuery(courseId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.ManageTeam))
        .WithTags("CourseTeam")
        .WithName("GetPendingInvitations");
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Enums;

namespace Neura.Api.Features.CourseTeam.GetTeamMember;

public sealed class GetTeamMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/{courseId:int}/team/members/{userId}", async (
            int courseId,
            string userId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetTeamMemberQuery(courseId, userId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.ViewAnalytics))
        .WithTags("CourseTeam")
        .WithName("GetTeamMember");
    }
}

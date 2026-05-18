using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Enums;
using System.Security.Claims;

namespace Neura.Api.Features.CourseTeam.GetTeamOverview;

public sealed class GetTeamOverviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/{courseId:int}/team", async (
            int courseId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var query = new GetTeamOverviewQuery(courseId, userId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.ViewAnalytics))
        .WithTags("CourseTeam")
        .WithName("GetTeamOverview");
    }
}

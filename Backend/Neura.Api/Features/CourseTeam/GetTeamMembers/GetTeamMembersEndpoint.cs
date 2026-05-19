// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;
//using Neura.Core.Authorization.Attributes;
//using Neura.Core.Enums;

//namespace Neura.Api.Features.CourseTeam.GetTeamMembers;

//public sealed class GetTeamMembersEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/courses/{courseId:int}/team/members", async (
//            int courseId,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var query = new GetTeamMembersQuery(courseId);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.ViewContent))
//        .WithTags("CourseTeam")
//        .WithName("GetTeamMembers");
//    }
//}

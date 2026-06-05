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
//using System.Security.Claims;

//namespace Neura.Api.Features.CourseTeam.RemoveTeamMember;

//public sealed class RemoveTeamMemberEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapDelete("api/courses/{courseId:int}/team/members/{userId}", async (
//            int courseId,
//            string userId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var requesterId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new RemoveTeamMemberCommand(courseId, userId, requesterId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.NoContent() 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.ManageTeam))
//        .WithTags("CourseTeam")
//        .WithName("RemoveTeamMember");
//    }
//}

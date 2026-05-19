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
//using System.Security.Claims;

//namespace Neura.Api.Features.Community.GetCourseMembers;

//public sealed class GetCourseMembersEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/community/courses/{courseId:int}/members", async (
//            int courseId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            try
//            {
//                var query = new GetCourseMembersQuery(courseId, userId);
//                var result = await sender.Send(query, ct);
//                return Results.Ok(result);
//            }
//            catch (UnauthorizedAccessException ex)
//            {
//                return Results.Forbid();
//            }
//        })
//        .RequireAuthorization()
//        .WithTags("Community")
//        .WithName("GetCourseMembers");
//    }
//}

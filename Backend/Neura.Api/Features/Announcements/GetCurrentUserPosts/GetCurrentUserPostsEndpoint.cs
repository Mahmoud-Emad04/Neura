// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;
//using System.Security.Claims;

//namespace Neura.Api.Features.Announcements.GetCurrentUserPosts;

//public sealed class GetCurrentUserPostsEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/announcements/posts/my", async (
//            [FromQuery] bool? isPublic,
//            [FromQuery] int? pageNumber, 
//            [FromQuery] int? pageSize,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var query = new GetCurrentUserPostsQuery(userId, isPublic, pageNumber ?? 1, pageSize ?? 10);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("Announcements")
//        .WithName("GetCurrentUserPosts");
//    }
//}

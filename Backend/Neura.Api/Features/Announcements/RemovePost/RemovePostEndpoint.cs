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

//namespace Neura.Api.Features.Announcements.RemovePost;

//public sealed class RemovePostEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapDelete("api/announcements/posts/{postId:int}", async (
//            int postId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new RemovePostCommand(postId, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.NoContent() 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("Announcements")
//        .WithName("RemovePost");
//    }
//}

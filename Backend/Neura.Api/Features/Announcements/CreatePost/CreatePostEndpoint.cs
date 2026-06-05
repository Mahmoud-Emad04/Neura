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
//using Neura.Core.Contracts.Announcement;
//using System.Security.Claims;

//namespace Neura.Api.Features.Announcements.CreatePost;

//public sealed class CreatePostEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPost("api/announcements/posts", async (
//            [FromForm] PostRequest request,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new CreatePostCommand(request, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.CreatedAtRoute("GetPostById", new { postId = result.Value.Id }, result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .DisableAntiforgery()
//        .WithTags("Announcements")
//        .WithName("CreatePost");
//    }
//}

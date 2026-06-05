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
//using Neura.Core.Contracts.Files;
//using System.Security.Claims;

//namespace Neura.Api.Features.Announcements.UpdatePostImage;

//public sealed class UpdatePostImageEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPut("api/announcements/posts/{postId:int}/image", async (
//            int postId,
//            [FromForm] UploadImageRequest request,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new UpdatePostImageCommand(postId, request, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.NoContent() 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .DisableAntiforgery()
//        .WithTags("Announcements")
//        .WithName("UpdatePostImage");
//    }
//}

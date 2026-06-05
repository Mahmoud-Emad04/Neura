// ═══════════════════════════════════════════════════════════════════════════
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by LessonsController (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ═══════════════════════════════════════════════════════════════════════════

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;
//using Neura.Core.Abstractions;
//using System.Security.Claims;

//namespace Neura.Api.Features.Lessons.Video.GetVideoLink;

//public sealed class GetVideoLinkEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/lessons/{id:int}/video/link", async (
//            int id,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var query = new GetVideoLinkQuery(id, userId);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("Lessons")
//        .WithName("GetVideoLink");
//    }
//}

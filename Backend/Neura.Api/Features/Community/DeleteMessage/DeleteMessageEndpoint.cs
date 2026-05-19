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

//namespace Neura.Api.Features.Community.DeleteMessage;

//public sealed class DeleteMessageEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapDelete("api/community/messages/{messageId:long}", async (
//            long messageId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            try
//            {
//                var command = new DeleteMessageCommand(messageId, userId);
//                var result = await sender.Send(command, ct);

//                return Results.Ok(result);
//            }
//            catch (UnauthorizedAccessException ex)
//            {
//                return Results.Forbid();
//            }
//            catch (KeyNotFoundException ex)
//            {
//                return Results.NotFound(ex.Message);
//            }
//        })
//        .RequireAuthorization()
//        .WithTags("Community")
//        .WithName("DeleteMessage");
//    }
//}

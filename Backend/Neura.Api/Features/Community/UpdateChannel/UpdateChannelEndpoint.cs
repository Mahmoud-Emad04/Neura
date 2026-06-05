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
//using Neura.Core.Contracts.Community;
//using System.Security.Claims;

//namespace Neura.Api.Features.Community.UpdateChannel;

//public sealed class UpdateChannelEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPut("api/community/channels/{channelId:int}", async (
//            int channelId,
//            [FromBody] UpdateChannelRequest request,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            try
//            {
//                var command = new UpdateChannelCommand(channelId, request, userId);
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
//        .WithName("UpdateChannel");
//    }
//}

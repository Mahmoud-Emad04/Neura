using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Community;
using System.Security.Claims;

namespace Neura.Api.Features.Community.ReorderChannels;

public sealed class ReorderChannelsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/community/courses/{courseId:int}/channels/reorder", async (
            int courseId,
            [FromBody] ReorderChannelsRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var command = new ReorderChannelsCommand(courseId, request, userId);
                var result = await sender.Send(command, ct);
                
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .RequireAuthorization()
        .WithTags("Community")
        .WithName("ReorderChannels");
    }
}

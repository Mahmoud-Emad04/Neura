using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Community;
using System.Security.Claims;

namespace Neura.Api.Features.Community.CreateChannel;

public sealed class CreateChannelEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/community/courses/{courseId:int}/channels", async (
            int courseId,
            [FromBody] CreateChannelRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var command = new CreateChannelCommand(courseId, request, userId);
                var result = await sender.Send(command, ct);
                
                return Results.CreatedAtRoute("GetChannels", new { courseId }, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        })
        .RequireAuthorization()
        .WithTags("Community")
        .WithName("CreateChannel");
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using System.Security.Claims;

namespace Neura.Api.Features.Community.EditMessage;

public sealed class EditMessageEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("community/messages/{messageId:long}", async (
            long messageId,
            [FromBody] EditMessageRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var command = new EditMessageCommand(messageId, request, userId);
                var result = await sender.Send(command, ct);
                
                return Results.Ok(result);
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
        .WithName("EditMessage");
    }
}

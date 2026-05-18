using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Authorization.Attributes;
using System.Security.Claims;

namespace Neura.Api.Features.Tags.DeleteTag;

public sealed class DeleteTagEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("tags/{id:int}", async (
            int id,
            [FromQuery] bool force,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new DeleteTagCommand(id, force, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok() 
                : result.ToProblemMinimal();
        })
        .WithMetadata(new AdminOnlyAttribute())
        .WithTags("Tags")
        .WithName("DeleteTag");
    }
}

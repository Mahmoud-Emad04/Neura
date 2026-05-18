using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.Tags;
using System.Security.Claims;

namespace Neura.Api.Features.Tags.BulkUpdateTagsOrder;

public sealed class BulkUpdateTagsOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("tags/bulk/order", async (
            [FromBody] BulkUpdateTagsOrderRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new BulkUpdateTagsOrderCommand(request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok() 
                : result.ToProblemMinimal();
        })
        .WithMetadata(new AdminOnlyAttribute())
        .WithTags("Tags")
        .WithName("BulkUpdateTagsOrder");
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.Lessons.GetArticleContent;

public sealed class GetArticleContentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("lessons/{id:int}/article", async (
            int id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var query = new GetArticleContentQuery(id, userId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Lessons")
        .WithName("GetArticleContent");
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Sections.GetSectionById;

public sealed class GetSectionByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("sections/{sectionId:int}", async (
            int sectionId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetSectionByIdQuery(sectionId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization("SectionPermission_ViewContent")
        .WithTags("Sections")
        .WithName("GetSectionById");
    }
}

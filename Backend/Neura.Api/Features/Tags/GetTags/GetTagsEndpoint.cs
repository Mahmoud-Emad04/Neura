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
//using Neura.Core.Authorization.Attributes;
//using Neura.Core.Contracts.Tags;

//namespace Neura.Api.Features.Tags.GetTags;

//public sealed class GetTagsEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/tags", async (
//            [AsParameters] TagFilters filters,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var query = new GetTagsQuery(filters);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .WithMetadata(new AdminOnlyAttribute())
//        .WithTags("Tags")
//        .WithName("GetAllTags");
//    }
//}

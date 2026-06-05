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

//namespace Neura.Api.Features.Tags.GetPopularTags;

//public sealed class GetPopularTagsEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/tags/popular", async (
//            [FromQuery] int count,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            count = count <= 0 ? 10 : count;

//            var query = new GetPopularTagsQuery(count);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .AllowAnonymous()
//        .WithTags("Tags")
//        .WithName("GetPopularTags");
//    }
//}

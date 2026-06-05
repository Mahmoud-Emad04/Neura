// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;

//namespace Neura.Api.Features.Tags.GetActiveTags;

//public sealed class GetActiveTagsEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/tags/active", async (
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var query = new GetActiveTagsQuery();
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .AllowAnonymous()
//        .WithTags("Tags")
//        .WithName("GetActiveTags");
//    }
//}

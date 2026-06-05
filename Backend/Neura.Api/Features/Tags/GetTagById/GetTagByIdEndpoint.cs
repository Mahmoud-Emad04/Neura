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
//using Neura.Core.Authorization.Attributes;

//namespace Neura.Api.Features.Tags.GetTagById;

//public sealed class GetTagByIdEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/tags/{id:int}", async (
//            int id,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var query = new GetTagByIdQuery(id);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .WithMetadata(new AdminOnlyAttribute())
//        .WithTags("Tags")
//        .WithName("GetTagById");
//    }
//}

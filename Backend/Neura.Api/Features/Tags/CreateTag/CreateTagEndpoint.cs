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
//using Neura.Core.Abstractions;
//using Neura.Core.Authorization.Attributes;
//using Neura.Core.Contracts.Tags;
//using System.Security.Claims;

//namespace Neura.Api.Features.Tags.CreateTag;

//public sealed class CreateTagEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPost("api/tags", async (
//            [FromBody] CreateTagRequest request,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new CreateTagCommand(request, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.CreatedAtRoute("GetTagById", new { id = result.Value.Id }, result.Value)
//                : result.ToProblemMinimal();
//        })
//        .WithMetadata(new AdminOnlyAttribute())
//        .WithTags("Tags")
//        .WithName("CreateTag");
//    }
//}

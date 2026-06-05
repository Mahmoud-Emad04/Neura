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

//namespace Neura.Api.Features.Tags.BulkToggleTagsActive;

//public sealed class BulkToggleTagsActiveEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPatch("api/tags/bulk/toggle-active", async (
//            [FromBody] BulkToggleTagsActiveRequest request,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new BulkToggleTagsActiveCommand(request, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.Ok() 
//                : result.ToProblemMinimal();
//        })
//        .WithMetadata(new AdminOnlyAttribute())
//        .WithTags("Tags")
//        .WithName("BulkToggleTagsActive");
//    }
//}

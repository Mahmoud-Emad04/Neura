// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Neura.Api.Extensions;
//using Neura.Api.Infrastructure;
//using Neura.Core.Contracts.Files;
//using System.Security.Claims;

//namespace Neura.Api.Features.Auth.UpdateImage;

//public sealed class UpdateImageEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPut("auth/image", async (
//            [FromForm] UploadImageRequest request,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.GetUserId()!;

//            var command = new UpdateImageCommand(request, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess
//                ? Results.Ok(result.Value)
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .DisableAntiforgery()
//        .WithTags("Auth")
//        .WithName("UpdateAuthImage");
//    }
//}

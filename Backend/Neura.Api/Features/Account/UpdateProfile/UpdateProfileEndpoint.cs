// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Neura.Api.Extensions;
//using Neura.Api.Infrastructure;
//using Neura.Core.Contracts.Users;

//namespace Neura.Api.Features.Account.UpdateProfile;

//public sealed class UpdateProfileEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPut("me", async (
//            [FromBody] UpdateProfileRequest request,
//            HttpContext context,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = context.User.GetUserId()!;

//            var command = new UpdateProfileCommand(userId, request);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess
//                ? Results.NoContent()
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("Account")
//        .WithName("UpdateProfile");
//    }
//}

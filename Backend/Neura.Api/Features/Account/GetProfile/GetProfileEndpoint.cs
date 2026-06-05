// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Neura.Api.Extensions;
//using Neura.Api.Infrastructure;

//namespace Neura.Api.Features.Account.GetProfile;

//public sealed class GetProfileEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("me", async (
//            HttpContext context,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = context.User.GetUserId()!;

//            var query = new GetProfileQuery(userId);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess
//                ? Results.Ok(result.Value)
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("Account")
//        .WithName("GetProfile");
//    }
//}

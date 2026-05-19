// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Neura.Api.Infrastructure;
//using Neura.Core.Contracts.Authentication;

//namespace Neura.Api.Features.Auth.Login;

//public sealed class LoginEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPost("auth/login", async (
//            LoginRequest request,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var command = new LoginCommand(request);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess
//                ? Results.Ok(result.Value)
//                : result.ToProblemMinimal();
//        })
//        .AllowAnonymous()
//        .WithTags("Auth")
//        .WithName("Login");
//    }
//}

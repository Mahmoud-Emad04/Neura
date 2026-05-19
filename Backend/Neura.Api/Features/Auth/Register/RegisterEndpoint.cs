// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Neura.Api.Infrastructure;
//using Neura.Core.Contracts.Authentication;

//namespace Neura.Api.Features.Auth.Register;

//public sealed class RegisterEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPost("auth/register", async (
//            RegisterRequest request,
//            HttpRequest httpRequest,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var origin = httpRequest.Headers.Origin.ToString();
//            if (string.IsNullOrEmpty(origin))
//                origin = $"{httpRequest.Scheme}://{httpRequest.Host}{httpRequest.PathBase}";

//            var command = new RegisterCommand(request, origin);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess
//                ? Results.Ok()
//                : result.ToProblemMinimal();
//        })
//        .AllowAnonymous()
//        .WithTags("Auth")
//        .WithName("Register");
//    }
//}

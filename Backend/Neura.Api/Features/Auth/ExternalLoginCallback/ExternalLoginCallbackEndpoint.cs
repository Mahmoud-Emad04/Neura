// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Neura.Api.Infrastructure;

//namespace Neura.Api.Features.Auth.ExternalLoginCallback;

//public sealed class ExternalLoginCallbackEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("auth/external-callback", async (
//            IConfiguration configuration,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var command = new ExternalLoginCallbackCommand();
//            var result = await sender.Send(command, ct);
//            var frontendUrl = configuration["FrontendUrl"];

//            if (!result.IsSuccess)
//            {
//                var safeError = Uri.EscapeDataString(result.Error.Message ?? "unknown");
//                return Results.Redirect($"{frontendUrl}/login?error={safeError}");
//            }

//            return Results.Redirect(
//                $"{frontendUrl}/callback#token={result.Value.Token}&refreshToken={result.Value.RefreshToken}");
//        })
//        .AllowAnonymous()
//        .WithTags("Auth")
//        .WithName("ExternalLoginCallback");
//    }
//}

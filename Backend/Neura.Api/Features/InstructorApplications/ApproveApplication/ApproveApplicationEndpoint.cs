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
//using Neura.Core.Abstractions;
//using Neura.Core.Authorization.Attributes;
//using System.Security.Claims;

//namespace Neura.Api.Features.InstructorApplications.ApproveApplication;

//public sealed class ApproveApplicationEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPost("api/instructor/applications/{id:int}/approve", async (
//            int id,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var reviewerId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new ApproveApplicationCommand(id, reviewerId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .WithMetadata(new AdminOnlyAttribute())
//        .WithTags("InstructorApplication")
//        .WithName("ApproveApplication");
//    }
//}

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
//using Neura.Core.InstructorApplication;
//using System.Security.Claims;

//namespace Neura.Api.Features.InstructorApplications.SubmitApplication;

//public sealed class SubmitApplicationEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPost("api/instructor/apply", async (
//            [FromBody] SubmitApplicationRequest request,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new SubmitApplicationCommand(request, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.CreatedAtRoute("GetMyApplicationStatus", null, result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("InstructorApplication")
//        .WithName("SubmitApplication");
//    }
//}

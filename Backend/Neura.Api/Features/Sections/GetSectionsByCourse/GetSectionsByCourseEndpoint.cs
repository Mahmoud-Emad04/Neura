// ═══════════════════════════════════════════════════════════════════════════
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by SectionsController (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ═══════════════════════════════════════════════════════════════════════════

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;
//using Neura.Core.Abstractions;

//namespace Neura.Api.Features.Sections.GetSectionsByCourse;

//public sealed class GetSectionsByCourseEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/courses/{courseId}/sections", async (
//            string courseId,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var query = new GetSectionsByCourseQuery(courseId);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .AllowAnonymous()
//        .WithTags("Sections")
//        .WithName("GetSectionsByCourse");
//    }
//}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Section;
using System.Security.Claims;

namespace Neura.Api.Features.Sections.CreateSection;

public sealed class CreateSectionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("courses/{courseId}/sections", async (
            string courseId,
            [FromBody] SectionRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new CreateSectionCommand(courseId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.CreatedAtRoute("GetSectionById", new { sectionId = result.Value.Id }, result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization("CoursePermission_EditContent")
        .WithTags("Sections")
        .WithName("CreateSection");
    }
}

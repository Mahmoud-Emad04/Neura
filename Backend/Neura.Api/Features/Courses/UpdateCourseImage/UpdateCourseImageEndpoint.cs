using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Files;
using System.Security.Claims;

namespace Neura.Api.Features.Courses.UpdateCourseImage;

public sealed class UpdateCourseImageEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("courses/{courseId}/cover-image", async (
            string courseId,
            [FromForm] UploadImageRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new UpdateCourseImageCommand(courseId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.NoContent() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization("CoursePermission_EditContent")
        .DisableAntiforgery()
        .WithTags("Courses")
        .WithName("UpdateCourseImage");
    }
}

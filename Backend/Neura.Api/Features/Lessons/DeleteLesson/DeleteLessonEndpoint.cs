using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.Lessons.DeleteLesson;

public sealed class DeleteLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("lessons/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new DeleteLessonCommand(id, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.NoContent() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization("LessonPermission_EditContent")
        .WithTags("Lessons")
        .WithName("DeleteLesson");
    }
}

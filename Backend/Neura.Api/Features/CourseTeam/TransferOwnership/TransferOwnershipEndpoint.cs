using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;
using System.Security.Claims;

namespace Neura.Api.Features.CourseTeam.TransferOwnership;

public sealed class TransferOwnershipEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/courses/{courseId:int}/team/transfer", async (
            int courseId,
            [FromBody] TransferOwnershipRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var requesterId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new TransferOwnershipCommand(courseId, request, requesterId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(new { message = "Ownership transferred successfully" }) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.TransferOwnership))
        .WithTags("CourseTeam")
        .WithName("TransferOwnership");
    }
}

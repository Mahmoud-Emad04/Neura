using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Authorization.Attributes;
using Neura.Core.InstructorApplication;
using System.Security.Claims;

namespace Neura.Api.Features.InstructorApplications.RejectApplication;

public sealed class RejectApplicationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("instructor/applications/{id:int}/reject", async (
            int id,
            [FromBody] ReviewApplicationRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var reviewerId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new RejectApplicationCommand(id, request, reviewerId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .WithMetadata(new AdminOnlyAttribute())
        .WithTags("InstructorApplication")
        .WithName("RejectApplication");
    }
}

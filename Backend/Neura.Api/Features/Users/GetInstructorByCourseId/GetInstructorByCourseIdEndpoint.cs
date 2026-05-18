using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;

namespace Neura.Api.Features.Users.GetInstructorByCourseId;

public sealed class GetInstructorByCourseIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/course/{courseId}", async (
            string courseId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetInstructorByCourseIdQuery(courseId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .WithTags("Users")
        .WithName("GetInstructorByCourseId");
    }
}

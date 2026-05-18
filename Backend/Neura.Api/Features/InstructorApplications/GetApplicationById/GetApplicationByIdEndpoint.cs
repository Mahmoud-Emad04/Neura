using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Authorization.Attributes;

namespace Neura.Api.Features.InstructorApplications.GetApplicationById;

public sealed class GetApplicationByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/instructor/applications/{id:int}", async (
            int id,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetApplicationByIdQuery(id);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .WithMetadata(new AdminOnlyAttribute())
        .WithTags("InstructorApplication")
        .WithName("GetApplicationById");
    }
}

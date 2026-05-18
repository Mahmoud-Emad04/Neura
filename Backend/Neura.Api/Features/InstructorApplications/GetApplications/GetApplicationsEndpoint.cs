using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Enums;

namespace Neura.Api.Features.InstructorApplications.GetApplications;

public sealed class GetApplicationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/instructor/applications", async (
            [FromQuery] ApplicationStatus? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = new GetApplicationsQuery(status, page, pageSize);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .WithMetadata(new AdminOnlyAttribute())
        .WithTags("InstructorApplication")
        .WithName("GetApplications");
    }
}

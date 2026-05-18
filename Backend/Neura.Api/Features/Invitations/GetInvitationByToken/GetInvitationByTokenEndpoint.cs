using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;

namespace Neura.Api.Features.Invitations.GetInvitationByToken;

public sealed class GetInvitationByTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/invitations/{token}", async (
            string token,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetInvitationByTokenQuery(token);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .WithTags("Invitations")
        .WithName("GetInvitationByToken");
    }
}

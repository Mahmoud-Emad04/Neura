using Microsoft.AspNetCore.Routing;

namespace Neura.Api.Infrastructure;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

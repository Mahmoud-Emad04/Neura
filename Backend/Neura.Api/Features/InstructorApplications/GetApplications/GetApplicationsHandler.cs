using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.InstructorApplication;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.InstructorApplications.GetApplications;

internal sealed class GetApplicationsHandler(ApplicationDbContext context) 
    : IRequestHandler<GetApplicationsQuery, Result<PaginatedList<ApplicationListResponse>>>
{
    public async Task<Result<PaginatedList<ApplicationListResponse>>> Handle(
        GetApplicationsQuery query, CancellationToken ct)
    {
        var status = query.Status;
        var pageNumber = query.PageNumber;
        var pageSize = query.PageSize;

        var queryable = context.InstructorApplications
            .AsNoTracking()
            .Include(a => a.User)
            .AsQueryable();

        if (status.HasValue) 
            queryable = queryable.Where(a => a.Status == status.Value);

        queryable = queryable.OrderByDescending(a => a.CreatedOn);

        var projectedQuery = queryable.Select(a => new ApplicationListResponse
        {
            Id = a.Id,
            UserId = a.UserId,
            UserName = $"{a.User.FirstName} {a.User.LastName}",
            UserEmail = a.User.Email ?? string.Empty,
            Status = a.Status,
            CreatedOn = a.CreatedOn,
            ReviewedOn = a.ReviewedOn
        });

        var paginatedList = await PaginatedList<ApplicationListResponse>.CreateAsync(
            projectedQuery, pageNumber, pageSize, null, ct);

        return Result.Success(paginatedList);
    }
}

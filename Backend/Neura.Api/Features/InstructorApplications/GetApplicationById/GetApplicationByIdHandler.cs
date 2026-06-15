using MediatR;
using Neura.Core.Errors;
using Neura.Core.InstructorApplication;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.InstructorApplications.GetApplicationById;

internal sealed class GetApplicationByIdHandler(ApplicationDbContext context)
    : IRequestHandler<GetApplicationByIdQuery, Result<ApplicationResponse>>
{
    public async Task<Result<ApplicationResponse>> Handle(
        GetApplicationByIdQuery query, CancellationToken ct)
    {
        var application = await context.InstructorApplications
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.ReviewedBy)
            .FirstOrDefaultAsync(a => a.Id == query.Id, ct);

        if (application is null)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationNotFound);

        return Result.Success(InstructorApplicationHelpers.MapToResponse(application, application.User, application.ReviewedBy));
    }
}

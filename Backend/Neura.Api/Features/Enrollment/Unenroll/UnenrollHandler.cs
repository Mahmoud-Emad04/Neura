using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Errors;
using Neura.Core.Enums;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Enrollment.Unenroll;

internal sealed class UnenrollHandler(
    ApplicationDbContext context,
    ILogger<UnenrollHandler> logger) 
    : IRequestHandler<UnenrollCommand, Result>
{
    public async Task<Result> Handle(
        UnenrollCommand request, CancellationToken ct)
    {
        var courseUser = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == request.CourseId && cu.UserId == request.UserId && !cu.IsDeleted, ct);

        if (courseUser is null) return Result.Failure(EnrollmentErrors.NotEnrolled);

        if (courseUser.CourseRole.Level == (int)CourseRoleType.CourseOwner)
            return Result.Failure(EnrollmentErrors.CannotUnenrollOwner);

        if (courseUser.CourseRole.Level >= (int)CourseRoleType.Assistant)
            return Result.Failure(EnrollmentErrors.CannotUnenrollTeamMember);

        courseUser.IsDeleted = true;
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} unenrolled from course {CourseId}", request.UserId, request.CourseId);
        return Result.Success();
    }
}

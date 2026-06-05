using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Instructor;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Users.GetInstructorByCourseId;

internal sealed class GetInstructorByCourseIdHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers) 
    : IRequestHandler<GetInstructorByCourseIdQuery, Result<InstructorSummaryResponse>>
{
    public async Task<Result<InstructorSummaryResponse>> Handle(
        GetInstructorByCourseIdQuery query, CancellationToken ct)
    {
        if (!TryDecodeCourseId(query.CourseId, out var courseId))
            return Result.Failure<InstructorSummaryResponse>(CourseErrors.CourseNotFound);

        var course = await context.Courses.FindAsync(new object[] { courseId }, ct);
        if (course is null)
            return Result.Failure<InstructorSummaryResponse>(CourseErrors.CourseNotFound);

        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == course.CreatedById, ct);

        if (user is null)
            return Result.Failure<InstructorSummaryResponse>(UserErrors.UserNotFound);

        var instructorCourseIds = await context.Courses
            .Where(c => c.CreatedById == course.CreatedById)
            .Select(c => c.Id)
            .ToListAsync(ct);

        instructorCourseIds ??= [];

        var (globalStudentCount, globalRating, globalRatingDataCount) =
            await GetStudentsAndRating(context, instructorCourseIds, ct);
            
        string baseUrl = helpers.GetBaseUrl();
        
        return Result.Success(user.Adapt<InstructorSummaryResponse>() with
        {
            Name = $"{user.FirstName} {user.LastName}",
            TotalStudents = globalStudentCount,
            TotalReviews = globalRatingDataCount,
            ImageUrl = string.IsNullOrEmpty(user.ImageUrl) ? null : $"{baseUrl}/{user.ImageUrl}",
            Rating = globalRating,
            TotalCourses = instructorCourseIds.Count
        });
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }

        courseId = numbers[0];
        return true;
    }

    private static async Task<(int globalStudentCount, double globalRating, int globalRatingDataCount)> GetStudentsAndRating(
        ApplicationDbContext context, List<int> instructorCourseIds, CancellationToken cancellationToken)
    {
        var studentRoleMask = CoursePermissionMasks.Student;

        var globalStudentCount = await context.CourseUsers
            .Where(cu => instructorCourseIds.Contains(cu.CourseId) && cu.PermissionMask == studentRoleMask)
            .Select(cu => cu.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var globalRatingData = await context.Reviews
            .Where(r => instructorCourseIds.Contains(r.CourseId))
            .Select(r => (double?)r.Rating)
            .ToListAsync(cancellationToken);

        var globalRating = globalRatingData.Count > 0 ? globalRatingData.Average() ?? 0 : 0;

        return (globalStudentCount, globalRating, globalRatingData.Count);
    }
}

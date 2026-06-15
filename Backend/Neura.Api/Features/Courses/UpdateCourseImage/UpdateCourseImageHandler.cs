using MediatR;
using Neura.Core.Errors;
using Neura.Core.FilesConsts;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.UpdateCourseImage;

internal sealed class UpdateCourseImageHandler(
    ApplicationDbContext context,
    IFileService fileService,
    IServiceHelpers helpers)
    : IRequestHandler<UpdateCourseImageCommand, Result>
{
    public async Task<Result> Handle(
        UpdateCourseImageCommand command, CancellationToken ct)
    {
        if (!TryDecodeCourseId(command.CourseIdKey, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        var course = await context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, ct);

        if (course is null)
            return Result.Failure(CourseErrors.CourseNotFound);

        var defaultPath = Path.Combine("Images", ImageConsts.Course, ImageConsts.DefaultCourseImage);

        if (!string.IsNullOrEmpty(course.ImageUrl) && course.ImageUrl != defaultPath)
            fileService.Delete(course.ImageUrl);

        course.ImageUrl = await fileService.UploadImageAsync(
            command.Request.Image,
            ImageConsts.Course,
            ct);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);
        return Result.Success();
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
}

using MediatR;
using Neura.Core.Contracts.Section;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Sections.CreateSection;

internal sealed class CreateSectionHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<CreateSectionCommand, Result<SectionResponse>>
{
    public async Task<Result<SectionResponse>> Handle(
        CreateSectionCommand command, CancellationToken ct)
    {
        if (!TryDecodeCourseId(command.CourseIdKey, out var courseId))
            return Result.Failure<SectionResponse>(CourseErrors.CourseNotFound);

        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Title) || request.Position < 0)
            return Result.Failure<SectionResponse>(SectionErrors.SectionInvalidData);

        var exists = await context.Sections.AnyAsync(
            s => s.CourseId == courseId && s.Position == request.Position && !s.IsDeleted, ct);

        if (exists)
            return Result.Failure<SectionResponse>(SectionErrors.SectionPositionConflict);

        var section = request.Adapt<Section>();
        section.CourseId = courseId;
        section.CreatedById = command.UserId;
        section.CreatedOn = DateTime.UtcNow;

        context.Sections.Add(section);
        await context.SaveChangesAsync(ct);

        return Result.Success(section.Adapt<SectionResponse>());
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

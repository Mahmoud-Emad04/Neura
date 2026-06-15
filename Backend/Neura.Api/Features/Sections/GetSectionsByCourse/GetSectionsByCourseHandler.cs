using MediatR;
using Neura.Core.Contracts.Section;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Sections.GetSectionsByCourse;

internal sealed class GetSectionsByCourseHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetSectionsByCourseQuery, Result<IEnumerable<SectionResponse>>>
{
    public async Task<Result<IEnumerable<SectionResponse>>> Handle(
        GetSectionsByCourseQuery request, CancellationToken ct)
    {
        if (!TryDecodeCourseId(request.CourseIdKey, out var courseId))
            return Result.Failure<IEnumerable<SectionResponse>>(CourseErrors.CourseNotFound);

        var sections = await context.Sections
            .Where(s => s.CourseId == courseId)
            .AsNoTracking()
            .ToListAsync(ct);

        var response = sections.Adapt<IEnumerable<SectionResponse>>();

        return Result.Success(response);
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

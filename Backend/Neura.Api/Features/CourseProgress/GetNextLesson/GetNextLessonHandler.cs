using MediatR;
using Neura.Api.Features.CourseProgress.GetCourseProgress;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.CourseProgress.GetNextLesson;

internal sealed class GetNextLessonHandler(ISender sender)
    : IRequestHandler<GetNextLessonQuery, Result<NextLessonResponse?>>
{
    public async Task<Result<NextLessonResponse?>> Handle(
        GetNextLessonQuery query, CancellationToken ct)
    {
        var progressResult = await sender.Send(
            new GetCourseProgressQuery(query.CourseKeyId, query.UserId), ct);

        if (progressResult.IsFailure)
            return Result.Failure<NextLessonResponse?>(progressResult.Error);

        return Result.Success(progressResult.Value.NextLesson);
    }
}

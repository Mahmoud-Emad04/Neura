using MediatR;
using Neura.Core.Contracts.Exam;
using Neura.Core.Contracts.Question;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Exams.GetExamByLessonId;

internal sealed class GetExamByLessonIdHandler(ApplicationDbContext context)
    : IRequestHandler<GetExamByLessonIdQuery, Result<ExamDetailResponse>>
{
    public async Task<Result<ExamDetailResponse>> Handle(
        GetExamByLessonIdQuery query, CancellationToken ct)
    {
        var exam = await context.Exams
            .AsNoTracking()
            .Include(e => e.Questions.OrderBy(q => q.Order))
                .ThenInclude(q => q.AnswerOptions.OrderBy(a => a.Order))
            .Include(e => e.Attempts)
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == query.LessonId, ct);

        if (exam is null)
            return Result.Failure<ExamDetailResponse>(ExamErrors.NoExamForLesson);

        var questionIds = exam.Questions.Select(q => q.Id).ToList();

        var questionsWithAttempts = await context.AttemptAnswers
            .AsNoTracking()
            .Where(aa => questionIds.Contains(aa.QuestionId))
            .Select(aa => aa.QuestionId)
            .Distinct()
            .ToHashSetAsync(ct);

        var response = exam.Adapt<ExamDetailResponse>();

        response.Questions = exam.Questions.Select(q =>
        {
            var questionResponse = q.Adapt<QuestionResponse>();
            questionResponse.HasAttempts = questionsWithAttempts.Contains(q.Id);
            return questionResponse;
        }).ToList();

        return Result.Success(response);
    }
}

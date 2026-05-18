using Ganss.Xss;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Exam;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Exams.UpdateExamSettings;

internal sealed class UpdateExamSettingsHandler(
    ApplicationDbContext context,
    HtmlSanitizer sanitizer) 
    : IRequestHandler<UpdateExamSettingsCommand, Result<ExamResponse>>
{
    public async Task<Result<ExamResponse>> Handle(
        UpdateExamSettingsCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var request = command.Request;
        var userId = command.UserId;

        var exam = await context.Exams
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId, ct);

        if (exam is null)
            return Result.Failure<ExamResponse>(ExamErrors.ExamNotFound);

        exam.Title = sanitizer.Sanitize(request.Title);
        exam.Description = request.Description is not null
            ? sanitizer.Sanitize(request.Description)
            : null;
        exam.DurationInMinutes = request.DurationInMinutes;
        exam.PassingScorePercentage = request.PassingScorePercentage;
        exam.MaxAttempts = request.MaxAttempts;
        exam.ShuffleQuestions = request.ShuffleQuestions;
        exam.ShuffleAnswers = request.ShuffleAnswers;
        exam.NumberOfQuestionsToServe = request.NumberOfQuestionsToServe;
        exam.EnableTabSwitchDetection = request.EnableTabSwitchDetection;
        exam.MaxViolationsBeforeAutoSubmit = request.MaxViolationsBeforeAutoSubmit;
        exam.UpdatedById = userId;
        exam.UpdatedOn = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        var response = exam.Adapt<ExamResponse>();

        response.TotalQuestions = await context.Questions
            .AsNoTracking()
            .CountAsync(q => q.ExamId == exam.Id, ct);

        response.TotalPoints = await context.Questions
            .AsNoTracking()
            .Where(q => q.ExamId == exam.Id)
            .SumAsync(q => q.Points, ct);

        response.TotalAttempts = await context.ExamAttempts
            .AsNoTracking()
            .CountAsync(a => a.ExamId == exam.Id, ct);

        return Result.Success(response);
    }
}

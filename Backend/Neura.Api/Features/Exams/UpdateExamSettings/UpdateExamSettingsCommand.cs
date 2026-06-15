using MediatR;
using Neura.Core.Contracts.Exam;

namespace Neura.Api.Features.Exams.UpdateExamSettings;

public sealed record UpdateExamSettingsCommand(int LessonId, UpdateExamSettingsRequest Request, string UserId)
    : IRequest<Result<ExamResponse>>;

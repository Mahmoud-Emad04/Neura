using MediatR;

namespace Neura.Api.Features.Exams.PublishExamGrades;

public sealed record PublishExamGradesCommand(int ExamId, string UserId)
    : IRequest<Result>;

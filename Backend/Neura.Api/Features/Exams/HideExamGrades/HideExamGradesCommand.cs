using MediatR;

namespace Neura.Api.Features.Exams.HideExamGrades;

public sealed record HideExamGradesCommand(int ExamId, string UserId)
    : IRequest<Result>;

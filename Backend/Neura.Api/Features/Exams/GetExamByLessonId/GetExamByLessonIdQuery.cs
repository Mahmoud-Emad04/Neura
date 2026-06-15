using MediatR;
using Neura.Core.Contracts.Exam;

namespace Neura.Api.Features.Exams.GetExamByLessonId;

public sealed record GetExamByLessonIdQuery(int LessonId, string UserId)
    : IRequest<Result<ExamDetailResponse>>;

using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Exam;

namespace Neura.Api.Features.Exams.GetExamById;

public sealed record GetExamByIdQuery(int LessonId, string UserId) 
    : IRequest<Result<ExamDetailResponse>>;

using MediatR;
using Neura.Core.Contracts.Exam;

namespace Neura.Api.Features.Exams.CreateExam;

public sealed record CreateExamCommand(CreateExamRequest Request, string UserId)
    : IRequest<Result<ExamResponse>>;

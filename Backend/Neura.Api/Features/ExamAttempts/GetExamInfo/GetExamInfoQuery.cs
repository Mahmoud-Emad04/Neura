using MediatR;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAttempts.GetExamInfo;

public sealed record GetExamInfoQuery(int LessonId, string UserId)
    : IRequest<Result<ExamInfoResponse>>;

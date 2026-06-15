using MediatR;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAnalytics.GetStudentAttemptDetail;

public sealed record GetStudentAttemptDetailQuery(int ExamId, int AttemptId, string UserId)
    : IRequest<Result<AttemptResultResponse>>;

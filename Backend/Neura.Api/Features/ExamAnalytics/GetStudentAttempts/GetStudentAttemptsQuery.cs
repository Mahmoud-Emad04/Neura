using MediatR;
using Neura.Core.Contracts.Analytics;

namespace Neura.Api.Features.ExamAnalytics.GetStudentAttempts;

public sealed record GetStudentAttemptsQuery(
    int ExamId, string UserId,
    int Page = 1, int PageSize = 20,
    string? SortBy = null, bool Descending = true)
    : IRequest<Result<ExamStudentAttemptsResponse>>;

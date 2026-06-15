using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Analytics;

namespace Neura.Api.Features.ExamAnalytics.GetStudentOwnAttempts;

public sealed record GetStudentOwnAttemptsQuery(
    int ExamId, string UserId,
    int Page = 1, int PageSize = 20,
    string? SortBy = null, bool Descending = true)
    : IRequest<Result<ExamStudentAttemptsResponse>>;

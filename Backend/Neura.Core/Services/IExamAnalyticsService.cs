using Neura.Core.Abstractions;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Core.Services;

public interface IExamAnalyticsService
{
    Task<Result<ExamAnalyticsResponse>> GetExamAnalyticsAsync(int examId, string userId);
    Task<Result<ExamStudentAttemptsResponse>> GetStudentAttemptsAsync(
        int examId, string userId, int page = 1, int pageSize = 20, string? sortBy = null, bool descending = true);
    Task<Result<AttemptResultResponse>> GetStudentAttemptDetailAsync(
        int attemptId, string userId);
    Task<Result<ScoreDistributionResponse>> GetScoreDistributionAsync(int examId, string userId);
}
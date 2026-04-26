using Neura.Core.Abstractions;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Core.Services;

public interface IExamAttemptService
{
    Task<Result<ExamInfoResponse>> GetExamInfoAsync(int examId, string userId);
    Task<Result<StartAttemptResponse>> StartAttemptAsync(int examId, string userId);
    Task<Result<StartAttemptResponse>> ResumeAttemptAsync(int attemptId, string userId);
    Task<Result> SaveAnswerAsync(int attemptId, int questionId, SaveAnswerRequest request, string userId);
    Task<Result<SubmitAttemptResponse>> SubmitAsync(int attemptId, string userId);
    Task<Result<AttemptResultResponse>> GetResultsAsync(int attemptId, string userId);
    Task<Result<ViolationResponse>> RecordViolationAsync(int attemptId, ViolationRequest request, string userId);
}
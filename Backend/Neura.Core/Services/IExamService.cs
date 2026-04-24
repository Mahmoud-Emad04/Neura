using Neura.Core.Abstractions;
using Neura.Core.Contracts.Exam;

namespace Neura.Core.Services;

public interface IExamService
{
    Task<Result<ExamResponse>> CreateAsync(CreateExamRequest request, string userId);
    Task<Result<ExamDetailResponse>> GetByIdAsync(int examId, string userId);
    Task<Result<ExamDetailResponse>> GetByLessonIdAsync(int lessonId, string userId);
    Task<Result<ExamResponse>> UpdateSettingsAsync(int examId, UpdateExamSettingsRequest request, string userId);
    Task<Result> PublishAsync(int examId, string userId);
    Task<Result> UnpublishAsync(int examId, string userId);
    Task<Result> DeleteAsync(int examId, string userId);
}
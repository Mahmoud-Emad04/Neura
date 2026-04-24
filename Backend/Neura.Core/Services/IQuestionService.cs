using Neura.Core.Abstractions;
using Neura.Core.Contracts.Question;

namespace Neura.Core.Services;

public interface IQuestionService
{
    Task<Result<QuestionResponse>> AddAsync(int examId, CreateQuestionRequest request, string userId);
    Task<Result<QuestionResponse>> UpdateAsync(int questionId, UpdateQuestionRequest request, string userId);
    Task<Result> DeleteAsync(int questionId, string userId);
    Task<Result> ReorderAsync(int examId, ReorderQuestionsRequest request, string userId);
}
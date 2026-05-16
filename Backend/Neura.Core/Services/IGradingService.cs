using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Core.Services;

public interface IGradingService
{
    Task GradeAttemptAsync(ExamAttempt attempt, AttemptStatus status);
}

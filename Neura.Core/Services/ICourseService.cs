using Neura.Core.Abstractions;
using Neura.Core.Contracts.Course;

namespace Neura.Core.Services;

public interface ICourseService
{
    Task<Result<IEnumerable<CourseResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<CourseResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default);
    Task<Result> CreateAsync(CourseRequest request,string userId, CancellationToken cancellationToken = default);
}
using GraduationProject.Core.Abstractions;
using GraduationProject.Core.Contracts.Course;

namespace GraduationProject.Core.Service;

public interface ICourseService
{
    Task<Result<IEnumerable<CourseResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<CourseResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default);
    Task<Result> CreateAsync(CourseRequest request,string userId, CancellationToken cancellationToken = default);
}
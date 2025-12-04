using Neura.Core.Abstractions;
using Neura.Core.Contracts.Course;
using Neura.Core.Contracts.Files;

namespace Neura.Core.Services;

public interface ICourseService
{
    Task<Result<IEnumerable<CourseResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<CourseResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default);
    Task<Result<CourseResponse>> CreateAsync(CourseRequest request, UploadImageRequest uploadImage, string userId, CancellationToken cancellationToken = default);
    Task<Result<CourseResponse>> UpdateAsync(string keyId, CourseUpdateRequest request,
        UploadImageRequest? uploadImage, string userId, CancellationToken cancellationToken = default);
}
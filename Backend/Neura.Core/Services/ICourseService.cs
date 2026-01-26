using Neura.Core.Abstractions;
using Neura.Core.Contracts.Course;
using Neura.Core.Contracts.Files;
using Neura.Core.Contracts;

namespace Neura.Core.Services;

public interface ICourseService
{
    Task<Result<IEnumerable<CourseResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<CourseResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default);
    Task<Result<CourseResponse>> CreateAsync(CourseRequest request, string userId, CancellationToken cancellationToken = default);
    Task<Result> UpdateImageAsync(string keyId, UploadImageRequest uploadImage,
            string userId, CancellationToken cancellationToken = default);
    Task<Result<CourseResponse>> UpdateAsync(string keyId, CourseUpdateRequest request,
         string userId, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(string keyId, string userId, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<CourseResponse>>> GetPagedAsync(int page = 1, int pageSize = 10, int? tagId = null, CancellationToken cancellationToken = default);

    // Admin operations
    Task<Result<PagedResult<CourseResponse>>> GetDeletedAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result> PurgeAsync(string keyId, CancellationToken cancellationToken = default);
    Task<Result> RestoreAsync(string keyId, CancellationToken cancellationToken = default);
}
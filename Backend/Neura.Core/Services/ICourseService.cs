using Neura.Core.Abstractions;
using Neura.Core.Contracts;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Course;
using Neura.Core.Contracts.Files;

namespace Neura.Core.Services;

public interface ICourseService
{
    Task<Result<PaginatedList<CourseResponse>>> GetAllAsync(RequestFilters filters,
        CancellationToken cancellationToken = default);

    Task<Result<CourseResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default);

    Task<Result<CourseResponse>> CreateAsync(CourseRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateImageAsync(string keyId, UploadImageRequest uploadImage,
        string userId, CancellationToken cancellationToken = default);

    Task<Result<CourseResponse>> UpdateAsync(string keyId, CourseUpdateRequest request,
        string userId, CancellationToken cancellationToken = default);

    Task<Result> EnrollAsync(string keyId, string userId, CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CourseResponse>>> GetEnrolledCoursesAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<Result> UnenrollAsync(string keyId, string userId, CancellationToken cancellationToken = default);
    Task<Result> AddReviewAsync(string keyId, string userId, ReviewRequest request, CancellationToken cancellationToken = default);

    //Task<Result> DeleteAsync(string keyId, string userId, CancellationToken cancellationToken = default);

    //Task<Result<PaginatedList<CourseResponse>>> GetPagedAsync(int page = 1, int pageSize = 10, int? tagId = null, CancellationToken cancellationToken = default);

    //// Admin operations
    //Task<Result<PaginatedList<CourseResponse>>> GetDeletedAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    //Task<Result> PurgeAsync(string keyId, CancellationToken cancellationToken = default);
    //Task<Result> RestoreAsync(string keyId, CancellationToken cancellationToken = default);
}
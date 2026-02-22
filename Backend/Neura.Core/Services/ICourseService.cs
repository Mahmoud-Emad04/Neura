using Neura.Core.Abstractions;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Course;
using Neura.Core.Contracts.Files;

namespace Neura.Core.Services;

public interface ICourseService
{
    Task<Result<PaginatedList<CourseSummaryResponse>>> GetAllAsync(RequestFilters filters, string? userId,
        CancellationToken cancellationToken = default);

    Task<Result<CourseResponse>> GetContentByIdAsync(string keyId, string? userId,
        CancellationToken cancellationToken = default);

    Task<Result<CourseMetadataResponse>> GetCourseMetadataAsync(string keyId, string? userId,
        CancellationToken cancellationToken = default);

    Task<Result<CourseMetadataResponse>> CreateAsync(CourseRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateImageAsync(string keyId, UploadImageRequest uploadImage,
        string userId, CancellationToken cancellationToken = default);

    Task<Result<CourseMetadataResponse>> UpdateAsync(string keyId, CourseUpdateRequest request,
        string userId, CancellationToken cancellationToken = default);

    Task<Result> EnrollAsync(string keyId, string userId, CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CourseMetadataResponse>>> GetEnrolledCoursesAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<Result> UnenrollAsync(string keyId, string userId, CancellationToken cancellationToken = default);

    Task<Result> ToggleBookmarkAsync(string keyId, string userId, CancellationToken cancellationToken = default);

    Task<Result<PaginatedList<CourseSummaryResponse>>> GetBookmarkedAsync(string userId, RequestFilters filters,
        CancellationToken cancellationToken = default);
}
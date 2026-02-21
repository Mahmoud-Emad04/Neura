using Neura.Core.Abstractions;
using Neura.Core.Contracts.Instructor;
using Neura.Core.Contracts.Users;

namespace Neura.Core.Services;

public interface IUserService
{
    Task<Result<UserProfileResponse>> GetProfileAsync(string userId);
    Task<Result> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<Result<InstructorSummaryResponse>> GetInstructorByCourseId(string keyId, CancellationToken cancellationToken = default);
    Task SendMail();
}
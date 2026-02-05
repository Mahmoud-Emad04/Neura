using Neura.Core.Abstractions;
using Neura.Core.Contracts.Section;

namespace Neura.Core.Services;

public interface ISectionService
{
    Task<Result<IEnumerable<SectionResponse>>> GetAllByCourseAsync(string courseKeyId,
        CancellationToken cancellationToken = default);

    Task<Result<SectionResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default);

    Task<Result<SectionResponse>> CreateAsync(string courseKeyId, SectionRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result<SectionResponse>> UpdateAsync(string keyId, SectionUpdateRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result> ToggleStatusAsync(string keyId, CancellationToken cancellationToken = default);
}
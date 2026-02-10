using Neura.Core.Abstractions;
using Neura.Core.Contracts.Section;

namespace Neura.Core.Services;

public interface ISectionService
{
    Task<Result<IEnumerable<SectionResponse>>> GetAllByCourseAsync(string courseKeyId,
        CancellationToken cancellationToken = default);

    Task<Result<SectionResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<SectionResponse>> CreateAsync(string courseKeyId, SectionRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result<SectionResponse>> UpdateAsync(int id, SectionUpdateRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result> ToggleStatusAsync(int id, CancellationToken cancellationToken = default);
}
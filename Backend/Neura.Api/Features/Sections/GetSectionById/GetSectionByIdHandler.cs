using MediatR;
using Neura.Core.Contracts.Section;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Sections.GetSectionById;

internal sealed class GetSectionByIdHandler(
    ApplicationDbContext context)
    : IRequestHandler<GetSectionByIdQuery, Result<SectionResponse>>
{
    public async Task<Result<SectionResponse>> Handle(
        GetSectionByIdQuery request, CancellationToken ct)
    {
        var section = await context.Sections
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == request.SectionId, ct);

        if (section is null)
            return Result.Failure<SectionResponse>(SectionErrors.SectionNotFound);

        return Result.Success(section.Adapt<SectionResponse>());
    }
}

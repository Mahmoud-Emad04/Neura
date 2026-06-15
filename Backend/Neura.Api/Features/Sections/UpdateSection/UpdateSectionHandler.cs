using MediatR;
using Neura.Core.Contracts.Section;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Sections.UpdateSection;

internal sealed class UpdateSectionHandler(
    ApplicationDbContext context)
    : IRequestHandler<UpdateSectionCommand, Result<SectionResponse>>
{
    public async Task<Result<SectionResponse>> Handle(
        UpdateSectionCommand command, CancellationToken ct)
    {
        var sectionId = command.SectionId;
        var request = command.Request;

        var section = await context.Sections
            .SingleOrDefaultAsync(s => s.Id == sectionId, ct);

        if (section is null)
            return Result.Failure<SectionResponse>(SectionErrors.SectionNotFound);

        if (string.IsNullOrWhiteSpace(request.Title) || request.Position < 0)
            return Result.Failure<SectionResponse>(SectionErrors.SectionInvalidData);

        var conflict = await context.Sections.AnyAsync(
            s => s.CourseId == section.CourseId &&
                 s.Id != section.Id &&
                 s.Position == request.Position &&
                 !s.IsDeleted,
            ct);

        if (conflict)
            return Result.Failure<SectionResponse>(SectionErrors.SectionPositionConflict);

        request.Adapt(section);
        section.UpdatedOn = DateTime.UtcNow;
        section.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        return Result.Success(section.Adapt<SectionResponse>());
    }
}

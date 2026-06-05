using MediatR;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Sections.ToggleSectionStatus;

internal sealed class ToggleSectionStatusHandler(
    ApplicationDbContext context)
    : IRequestHandler<ToggleSectionStatusCommand, Result>
{
    public async Task<Result> Handle(
        ToggleSectionStatusCommand command, CancellationToken ct)
    {
        var section = await context.Sections
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(s => s.Id == command.SectionId, ct);

        if (section is null)
            return Result.Failure(SectionErrors.SectionNotFound);

        if (section.IsDeleted)
        {
            section.IsDeleted = false;
            section.DeletedOn = null;
            section.DeletedById = null;
        }
        else
        {
            section.IsDeleted = true;
            section.DeletedOn = DateTime.UtcNow;
            section.DeletedById = command.UserId;
        }

        section.UpdatedOn = DateTime.UtcNow;
        section.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

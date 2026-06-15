using MediatR;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Sections.DeleteSection;

internal sealed class DeleteSectionHandler(
    ApplicationDbContext context)
    : IRequestHandler<DeleteSectionCommand, Result>
{
    public async Task<Result> Handle(
        DeleteSectionCommand command, CancellationToken ct)
    {
        var section = await context.Sections
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(s => s.Id == command.SectionId, ct);

        if (section is null)
            return Result.Failure(SectionErrors.SectionNotFound);

        if (section.IsDeleted)
            return Result.Failure(SectionErrors.SectionAlreadyDeleted);

        section.IsDeleted = true;
        section.DeletedOn = DateTime.UtcNow;
        section.DeletedById = command.UserId;
        section.UpdatedOn = DateTime.UtcNow;
        section.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.DeleteTag;

internal sealed class DeleteTagHandler(ApplicationDbContext context) 
    : IRequestHandler<DeleteTagCommand, Result>
{
    public async Task<Result> Handle(
        DeleteTagCommand command, CancellationToken ct)
    {
        var tag = await context.Tags
            .Include(t => t.Courses)
            .SingleOrDefaultAsync(t => t.Id == command.Id, ct);

        if (tag is null)
            return Result.Failure(TagErrors.TagNotFound);

        var courseCount = tag.Courses.Count(c => !c.IsDeleted);

        if (courseCount > 0 && !command.Force)
            return Result.Failure(TagErrors.CannotDeleteTagWithCourses);

        if (command.Force && courseCount > 0) 
            tag.Courses.Clear();

        tag.IsDeleted = true;
        tag.IsActive = false;
        tag.UpdatedOn = DateTime.UtcNow;
        tag.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

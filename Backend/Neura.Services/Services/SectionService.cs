using Neura.Core.Contracts.Section;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class SectionService(
    ApplicationDbContext context,
    IServiceHelpers helpers,
    ILogger<SectionService> logger) : ISectionService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IServiceHelpers _helpers = helpers;
    private readonly ILogger<SectionService> _logger = logger;

    public async Task<Result<IEnumerable<SectionResponse>>> GetAllByCourseAsync(string courseKeyId,
        CancellationToken cancellationToken = default)
    {
        //var courseIds = Decode(courseKeyId);
        //if (courseIds.Length == 0) return Result.Failure<IEnumerable<SectionResponse>>(CourseErrors.CourseNotFound);
        //int courseId = courseIds[0];
        var courseIds = _helpers.DecodeHash(courseKeyId);
        if (courseIds.Length == 0)
            return Result.Failure<IEnumerable<SectionResponse>>(CourseErrors.CourseNotFound);
        var courseId = courseIds[0];

        var sections = await _context.Sections
            .Where(s => s.CourseId == courseId)
            //.Include(s => s.Lessons)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var response = sections.Adapt<IEnumerable<SectionResponse>>();

        return Result.Success(response);
    }

	public async Task<Result<SectionResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
	{
		var section = await _context.Sections
			//.Include(s => s.Lessons)
			.AsNoTracking()
			.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (section is null) return Result.Failure<SectionResponse>(SectionErrors.SectionNotFound);

        return Result.Success(section.Adapt<SectionResponse>());
    }

    public async Task<Result<SectionResponse>> CreateAsync(string courseKeyId, SectionRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        //var courseIds = Decode(courseKeyId);
        //if (courseIds.Length == 0) return Result.Failure<SectionResponse>(CourseErrors.CourseNotFound);
        //int courseId = courseIds[0];
        var courseIds = _helpers.DecodeHash(courseKeyId);
        if (courseIds.Length == 0)
            return Result.Failure<SectionResponse>(CourseErrors.CourseNotFound);
        var courseId = courseIds[0];

        // validate basic fields
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<SectionResponse>(SectionErrors.SectionInvalidData);

        if (request.Position < 0)
            return Result.Failure<SectionResponse>(SectionErrors.SectionInvalidData);

        // ensure no duplicate position inside the same course
        var exists = await _context.Sections.AnyAsync(
            s => s.CourseId == courseId && s.Position == request.Position && !s.IsDeleted, cancellationToken);
        if (exists)
            return Result.Failure<SectionResponse>(SectionErrors.SectionPositionConflict);

        var section = request.Adapt<Section>();
        section.CourseId = courseId;
        section.CreatedById = userId;
        section.CreatedOn = DateTime.UtcNow;

        _context.Sections.Add(section);
        await _context.SaveChangesAsync(cancellationToken);

        // create public key id for the section
        //var hashids = new Hashids("Section", 8);
        //var keyId = hashids.Encode(section.Id);

        var response = section.Adapt<SectionResponse>();

        return Result.Success(response);
    }

	public async Task<Result<SectionResponse>> UpdateAsync(int id, SectionUpdateRequest request, string userId, CancellationToken cancellationToken = default)
	{
		var section = await _context.Sections
			.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (section is null) return Result.Failure<SectionResponse>(SectionErrors.SectionNotFound);

        // validate
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<SectionResponse>(SectionErrors.SectionInvalidData);

        if (request.Position < 0)
            return Result.Failure<SectionResponse>(SectionErrors.SectionInvalidData);

        // check duplicate position within same course (exclude current section)
        var conflict = await _context.Sections
            .AnyAsync(
                s => s.CourseId == section.CourseId && s.Id != section.Id && s.Position == request.Position &&
                     !s.IsDeleted, cancellationToken);
        if (conflict)
            return Result.Failure<SectionResponse>(SectionErrors.SectionPositionConflict);

        request.Adapt(section);
        section.UpdatedOn = DateTime.UtcNow;
        section.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(section.Adapt<SectionResponse>());
    }

	public async Task<Result> ToggleStatusAsync(int id, CancellationToken cancellationToken = default)
	{
		var section = await _context.Sections
			.IgnoreQueryFilters()
			.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (section is null) return Result.Failure(SectionErrors.SectionNotFound);

        var userId = CurrentUserId();

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
            section.DeletedById = userId;
        }

        section.UpdatedOn = DateTime.UtcNow;
        section.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private string? CurrentUserId()
    {
        return _helpers.GetCurrentUserId();
    }
}
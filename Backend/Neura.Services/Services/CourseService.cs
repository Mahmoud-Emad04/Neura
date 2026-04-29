using Neura.Core.Abstractions.Specification;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Courses;
using Neura.Core.Contracts.Files;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Contracts.Section;
using Neura.Core.Enums;
using Neura.Core.FilesConsts;
using Neura.Core.Specifications.Courses;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class CourseService(
    ApplicationDbContext context,
    IFileService fileService,
    IServiceHelpers helpers,
    UserManager<ApplicationUser> userManager,
    ILogger<CourseService> logger) : ICourseService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IFileService _fileService = fileService;
    private readonly IServiceHelpers _helpers = helpers;
    private readonly ILogger<CourseService> _logger = logger;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<PaginatedList<CourseSummaryResponse>>> GetAllAsync(
        RequestFilters filters,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var spec = new CourseFilterSpecification(filters);

        var query = SpecificationEvaluator.GetQuery(_context.Courses.AsNoTracking(), spec);

        var projectedQuery = query.ProjectToType<CourseSummaryResponse>();

        var baseUrl = BaseUrl();

        var paginatedCourses = await PaginatedList<CourseSummaryResponse>.CreateAsync(
            projectedQuery,
            filters.PageNumber,
            filters.PageSize,
            c => c.ImageUrl = $"{baseUrl}/{c.ImageUrl}",
            cancellationToken
        );


        foreach (var course in paginatedCourses.Items)
        {
            TryDecodeCourseId(course.KeyId, out var courseId);

            var lessons = await _context.Lessons.Where(l => l.Section.CourseId == courseId && !l.IsDeleted).Select(l => new { l.Duration }).ToListAsync(cancellationToken);

            course.NumberOfStudents =
                await _context.CourseUsers.CountAsync(c => c.CourseId == courseId && !c.IsDeleted, cancellationToken);

            course.NumberOfLessons = lessons.Count();
            course.Hours = lessons.Sum(l => (int)l.Duration.TotalMinutes);
        }

        if (userId is not null)
        {
            var bookmarkedCourseIds = await _context.CourseBookmarks
                .Where(b => b.UserId == userId && !b.IsDeleted)
                .Select(b => b.CourseId)
                .ToListAsync(cancellationToken);

            var courseIds = paginatedCourses.Items.Select(c => _helpers.DecodeHash(c.KeyId)[0]).ToList();

            var enrolledCourseIds = await _context.CourseUsers
                .Where(cu => courseIds.Contains(cu.CourseId) && cu.UserId == userId)
                .Select(cu => cu.CourseId)
                .ToHashSetAsync(cancellationToken);

            foreach (var course in paginatedCourses.Items)
                if (TryDecodeCourseId(course.KeyId, out var id))
                {
                    course.IsBookmarked = bookmarkedCourseIds.Contains(id);
                    course.IsEnrolled = enrolledCourseIds.Contains(id);
                }
        }

        return Result.Success(paginatedCourses);
    }

    public async Task<Result<EditableCoursesListSummaryResponse>> GetEditableCoursesAsync(
        EditableCourseFilters filters,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var ownerLevel = (int)CourseRoleType.CourseOwner;
        var coInstructorLevel = (int)CourseRoleType.CoInstructor;
        var studentLevel = (int)CourseRoleType.Student;

        var query = _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .Include(cu => cu.Course)
            .Where(cu =>
                cu.UserId == userId &&
                !cu.IsDeleted &&
                cu.CourseRole.Level >= coInstructorLevel)
            .Select(cu => new
            {
                CourseUser = cu,
                cu.Course,
                RoleLevel = cu.CourseRole.Level,
                IsOwner = cu.CourseRole.Level == ownerLevel,
                IsCoInstructor = cu.CourseRole.Level == coInstructorLevel
            })
            .Where(x => !x.Course.IsDeleted);

        query = filters.RoleFilter switch
        {
            EditableRoleFilter.OwnedOnly => query.Where(x => x.IsOwner),
            EditableRoleFilter.CoInstructorOnly => query.Where(x => x.IsCoInstructor),
            _ => query
        };

        if (filters.Status.HasValue) query = query.Where(x => x.Course.Status == filters.Status.Value);

        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var searchTerm = filters.SearchTerm.ToLower().Trim();
            query = query.Where(x =>
                x.Course.Title.ToLower().Contains(searchTerm) ||
                (x.Course.Description != null && x.Course.Description.ToLower().Contains(searchTerm)));
        }

        var totalOwnedCourses = await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .CountAsync(cu =>
                    cu.UserId == userId &&
                    !cu.IsDeleted &&
                    !cu.Course.IsDeleted &&
                    cu.CourseRole.Level == ownerLevel,
                cancellationToken);

        var totalCoInstructorCourses = await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .CountAsync(cu =>
                    cu.UserId == userId &&
                    !cu.IsDeleted &&
                    !cu.Course.IsDeleted &&
                    cu.CourseRole.Level == coInstructorLevel,
                cancellationToken);

        var totalCount = await query.CountAsync(cancellationToken);
        //totalCount = (int)Math.Ceiling((decimal)totalCount / filters.PageSize);

        var courseIds = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(x => x.Course.Id)
            .ToListAsync(cancellationToken);

        // Get courses with related data
        var coursesData = await query
            .Where(x => courseIds.Contains(x.Course.Id))
            .Select(x => new
            {
                x.Course,
                x.RoleLevel,
                x.IsOwner,
                x.IsCoInstructor
            })
            .ToListAsync(cancellationToken);

        var studentCounts = await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .Where(cu =>
                courseIds.Contains(cu.CourseId) &&
                !cu.IsDeleted &&
                cu.CourseRole.Level == studentLevel)
            .GroupBy(cu => cu.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);

        var baseUrl = BaseUrl();

        var items = coursesData.Select(x =>
        {
            var course = x.Course;
            var isOwner = x.IsOwner;
            var isCoInstructor = x.IsCoInstructor;

            studentCounts.TryGetValue(course.Id, out var studentCount);

            return new EditableCourseSummaryResponse
            {
                KeyId = _helpers.Encode(course.Id),
                Title = course.Title,
                ImageUrl = Path.Combine(baseUrl, course.ImageUrl),
                Status = course.Status,
                StatusName = course.Status.ToString(),
                IsEnrollmentOpen = course.IsEnrollmentOpen,
                IsPubliclyVisible = course.IsPubliclyVisible,
                RoleName = isOwner ? "Owner" : "Co-Instructor",
                IsOwner = isOwner,
                IsCoInstructor = isCoInstructor,
                NumberOfStudents = studentCount,
                CreatedOn = course.CreatedOn,
                UpdatedOn = course.UpdatedOn,
                AvailableActions = BuildAvailableActions(course.Status, isOwner, isCoInstructor)
            };
        }).ToList();

        var orderedItems = courseIds
            .Select(id => items.First(i => _helpers.DecodeHash(i.KeyId)[0] == id))
            .ToList();

        var paginatedList = new PaginatedList<EditableCourseSummaryResponse>(
            orderedItems,
            filters.PageNumber,
            totalCount,
            filters.PageSize);

        return Result.Success(new EditableCoursesListSummaryResponse
        {
            TotalOwnedCourses = totalOwnedCourses,
            TotalCoInstructorCourses = totalCoInstructorCourses,
            Courses = paginatedList
        });
    }


    public async Task<Result<CourseResponse>> GetContentByIdAsync(
        string keyId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var courseExists = await _context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId, cancellationToken);

        if (!courseExists)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var isEnrolled = !string.IsNullOrEmpty(userId) && await _context.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu => cu.CourseId == courseId &&
                           cu.UserId == userId &&
                           !cu.IsDeleted,
                     cancellationToken);

        var sections = await _context.Sections
            .AsNoTracking()
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.Position)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.Description,
                s.Position,
                Lessons = s.Lessons
                    .OrderBy(l => l.OrderIndex)
                    .Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.Description,
                        l.Type,
                        l.Duration,
                        l.OrderIndex,
                        l.IsPreview,
                        Exam = l.Exam == null ? null : new
                        {
                            l.Exam.Id,
                            l.Exam.Title,
                            TotalQuestions = l.Exam.Questions.Count(),
                            l.Exam.DurationInMinutes,
                            l.Exam.PassingScorePercentage,
                            l.Exam.MaxAttempts
                        }
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var sectionResponses = sections.Select(s => new SectionResponse(
            Id: s.Id,
            Title: s.Title,
            Description: s.Description,
            Position: s.Position,
            TotalMinutes: (int)s.Lessons.Sum(l => l.Duration.TotalMinutes),
            LessonsCount: s.Lessons.Count,
            Lessons: s.Lessons.Select(l => new LessonResponse(
                // If Quiz → use Exam.Id & Exam.Title, else use Lesson.Id & Lesson.Title
                Id: l.Type == LessonType.Quiz && l.Exam != null ? l.Exam.Id : l.Id,
                Title: l.Type == LessonType.Quiz && l.Exam != null ? l.Exam.Title : l.Title,
                Description: l.Description,
                Type: l.Type.ToString(),
                Duration: l.Duration,
                OrderIndex: l.OrderIndex,
                IsPreview: l.IsPreview,
                IsLocked: !l.IsPreview && !isEnrolled,
                Exam: l.Exam == null ? null : new ExamPreviewInfo(
                    TotalQuestions: l.Exam.TotalQuestions,
                    DurationInMinutes: l.Exam.DurationInMinutes,
                    PassingScorePercentage: l.Exam.PassingScorePercentage,
                    MaxAttempts: l.Exam.MaxAttempts
                )
            )).ToList()
        )).ToList();

        var totalMinutes = sectionResponses.Sum(s => s.TotalMinutes);
        var totalLessons = sectionResponses.Sum(s => s.LessonsCount);

        var response = new CourseResponse(
            KeyId: keyId,
            TotalHours: (int)Math.Round(totalMinutes / 60.0),
            TotalLessons: totalLessons,
            Sections: sectionResponses
        );

        return Result.Success(response);
    }


    public async Task<Result<CourseMetadataResponse>> GetCourseMetadataAsync(string keyId, string? userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Tags)
            .Include(c => c.Prerequisites)
            .Include(c => c.LearningOutcomes)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        return await BuildCourseMetadataResponse(course, userId, courseId, cancellationToken);
    }

    public async Task<Result<CourseMetadataResponse>> CreateAsync(CourseRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var tags = await _context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count != request.Tags.Count)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseTagNotFound);

        var ownerUser = await _userManager.FindByIdAsync(userId);

        if (ownerUser is null)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.UserNotFound);

        var ownerRole = await _context.CourseRoles
            .FirstOrDefaultAsync(r => r.Level == (int)CourseRoleType.CourseOwner, cancellationToken);

        if (ownerRole is null)
        {
            _logger.LogError("CourseOwner role not found in database. Please run database seeding.");

            return Result.Failure<CourseMetadataResponse>(
                new Error("Course.RoleNotFound", "Course role configuration is missing",
                    StatusCodes.Status500InternalServerError));
        }

        var course = request.Adapt<Course>();

        course.DisplayInstructorName = request.InstructorName;
        course.CreatedById = userId;
        course.CreatedOn = DateTime.UtcNow;
        course.Tags = tags;

        if (request.Image is not null)
            course.ImageUrl = await _fileService.UploadImageAsync(request.Image, ImageConsts.Course, cancellationToken);
        else
            course.ImageUrl = DefaultCourseImagePath();

        _logger.LogInformation("Image with Title {title} & Url {imageurl} Created", course.Title, course.ImageUrl);

        _context.Courses.Add(course);

        // Create CourseUser entry with Owner role
        var courseUser = new CourseUser
        {
            Course = course,
            UserId = userId,
            CourseRoleId = ownerRole.Id,
            PermissionMask = ownerRole.PermissionMask,
            EnrolledOn = DateTime.UtcNow,
            EnrolledById = null
        };

        await _context.CourseUsers.AddAsync(courseUser, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Course {CourseId} '{Title}' created by user {UserId}. Status: {Status}",
            course.Id,
            course.Title,
            userId,
            course.Status);

        return Result.Success(course.Adapt<CourseMetadataResponse>());
    }

    public async Task<Result> UpdateImageAsync(string keyId, UploadImageRequest uploadImage, string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure(CourseErrors.CourseNotFound);

        if (course.ImageUrl != DefaultCourseImagePath())
            _fileService.Delete(course.ImageUrl);

        course.ImageUrl = await _fileService.UploadImageAsync(uploadImage.Image, ImageConsts.Course, cancellationToken);
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CourseMetadataResponse>> UpdateAsync(string keyId, CourseUpdateRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        //throw new NotImplementedException();
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .Include(c => c.Tags)
            .Include(c => c.LearningOutcomes)
            .Include(c => c.Prerequisites)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        var tags = await _context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count != request.Tags.Count)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.TagNotFound);

        //// ══════════════════════════════════════════════════════════════
        //// 4. Update Basic Properties
        //// ══════════════════════════════════════════════════════════════

        course.Title = request.Title.Trim();
        course.Description = request.Description?.Trim() ?? string.Empty;
        course.Price = request.Price;
        course.DisplayInstructorName = request.InstructorName?.Trim();
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;
        course.IsPubliclyVisible = request.IsPubliclyVisible;

        course.Tags.Clear();
        foreach (var tag in tags) course.Tags.Add(tag);

        _context.CourseLearningOutcomes.RemoveRange(course.LearningOutcomes);

        course.LearningOutcomes = request.LearningOutcomes
            .Where(lo => !string.IsNullOrWhiteSpace(lo))
            .Select((outcome, index) => new CourseLearningOutcome
            {
                CourseId = courseId,
                Outcome = outcome.Trim()
            })
            .ToList();

        _context.CoursePrerequisites.RemoveRange(course.Prerequisites);

        course.Prerequisites = request.Prerequisites
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select((prereq, index) => new CoursePrerequisite
            {
                CourseId = courseId,
                Requirement = prereq.Trim()
            })
            .ToList();

        if (request.Image is not null)
        {
            if (!string.IsNullOrEmpty(course.ImageUrl) && course.ImageUrl != DefaultCourseImagePath())
                _fileService.Delete(course.ImageUrl);

            course.ImageUrl = await _fileService.UploadImageAsync(
                request.Image,
                ImageConsts.Course,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await BuildCourseMetadataResponse(course, userId, courseId, cancellationToken);
    }
    public async Task<Result> ToggleBookmarkAsync(string keyId, string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        if (await _context.CourseBookmarks.FirstOrDefaultAsync(cb => cb.CourseId == courseId && cb.UserId == userId,
                cancellationToken) is not { } bookmark)
            await _context.CourseBookmarks.AddAsync(
                new CourseBookmark
                { CourseId = courseId, UserId = userId, IsDeleted = false, CreatedOn = DateTime.UtcNow },
                cancellationToken);
        else
            bookmark.IsDeleted = !bookmark.IsDeleted;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PaginatedList<CourseSummaryResponse>>> GetBookmarkedAsync
        (string userId, RequestFilters filters, CancellationToken cancellationToken = default)
    {
        var spec = new BookmarkedCoursesFilterSpecification(userId, filters);

        var query = SpecificationEvaluator.GetQuery(_context.CourseBookmarks.AsNoTracking(), spec);

        var projectedQuery = query.ProjectToType<CourseSummaryResponse>();

        var baseUrl = BaseUrl();

        var paginatedCourses = await PaginatedList<CourseSummaryResponse>.CreateAsync(
            projectedQuery,
            filters.PageNumber,
            filters.PageSize,
            c => c.ImageUrl = $"{baseUrl}/{c.ImageUrl}",
            cancellationToken
        );

        return Result.Success(paginatedCourses);
    }

    public async Task<Result<CourseStatusResponse>> GetCourseStatusAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusResponse>(CourseErrors.CourseNotFound);

        ActivationRequirements? requirements = null;

        if (course.Status == CourseStatus.Pending)
            requirements = await GetActivationRequirementsAsync(courseId, cancellationToken);

        var response = new CourseStatusResponse
        {
            KeyId = keyId,
            Status = course.Status,
            StatusName = course.Status.ToString(),
            IsEnrollmentOpen = course.IsEnrollmentOpen,
            IsAccessibleToStudents = course.IsAccessibleToStudents,
            IsPubliclyVisible = course.IsPubliclyVisible,
            CanActivate = course.Status == CourseStatus.Pending,
            CanComplete = course.Status == CourseStatus.Active,
            CanReactivate = course.Status == CourseStatus.Completed,
            CanUnpublish = course.Status == CourseStatus.Active,
            Requirements = requirements
        };

        return Result.Success(response);
    }

    public async Task<Result<CourseStatusUpdateResponse>> ActivateCourseAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var hasPublishedContent = await _context.Lessons
            .AsNoTracking()
            .AnyAsync(l =>
                    l.Section.CourseId == courseId &&
                    l.IsPublished &&
                    !l.IsDeleted &&
                    !l.Section.IsDeleted,
                cancellationToken);

        if (!hasPublishedContent)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseHasNoPublishedContent);

        var previousStatus = course.Status;

        var result = course.Activate();
        if (result.IsFailure)
            return Result.Failure<CourseStatusUpdateResponse>(result.Error);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Course {CourseId} activated by user {UserId}. Status changed from {PreviousStatus} to {CurrentStatus}",
            courseId,
            userId,
            previousStatus,
            course.Status);

        return Result.Success(new CourseStatusUpdateResponse
        {
            KeyId = keyId,
            PreviousStatus = previousStatus,
            CurrentStatus = course.Status,
            Message = "Course activated successfully. Students can now enroll.",
            UpdatedAt = course.UpdatedOn.Value
        });
    }

    public async Task<Result<CourseStatusUpdateResponse>> CompleteCourseAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var previousStatus = course.Status;

        var result = course.Complete();
        if (result.IsFailure)
            return Result.Failure<CourseStatusUpdateResponse>(result.Error);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Course {CourseId} completed by user {UserId}. Status changed from {PreviousStatus} to {CurrentStatus}",
            courseId,
            userId,
            previousStatus,
            course.Status);

        return Result.Success(new CourseStatusUpdateResponse
        {
            KeyId = keyId,
            PreviousStatus = previousStatus,
            CurrentStatus = course.Status,
            Message =
                "Course marked as completed. Enrolled students can still access content, but no new enrollments allowed.",
            UpdatedAt = course.UpdatedOn.Value
        });
    }

    public async Task<Result<CourseStatusUpdateResponse>> ReactivateCourseAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var previousStatus = course.Status;

        var result = course.Reactivate();
        if (result.IsFailure)
            return Result.Failure<CourseStatusUpdateResponse>(result.Error);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Course {CourseId} reactivated by user {UserId}. Status changed from {PreviousStatus} to {CurrentStatus}",
            courseId,
            userId,
            previousStatus,
            course.Status);

        return Result.Success(new CourseStatusUpdateResponse
        {
            KeyId = keyId,
            PreviousStatus = previousStatus,
            CurrentStatus = course.Status,
            Message = "Course reactivated successfully. New students can now enroll.",
            UpdatedAt = course.UpdatedOn.Value
        });
    }

    public async Task<Result<CourseStatusUpdateResponse>> UnpublishCourseAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var previousStatus = course.Status;

        var result = course.Unpublish();
        if (result.IsFailure)
            return Result.Failure<CourseStatusUpdateResponse>(result.Error);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Course {CourseId} unpublished by user {UserId}. Status changed from {PreviousStatus} to {CurrentStatus}",
            courseId,
            userId,
            previousStatus,
            course.Status);

        return Result.Success(new CourseStatusUpdateResponse
        {
            KeyId = keyId,
            PreviousStatus = previousStatus,
            CurrentStatus = course.Status,
            Message = "Course unpublished. It is now hidden from public listings.",
            UpdatedAt = course.UpdatedOn.Value
        });
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = _helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }

        courseId = numbers[0];
        return true;
    }

    private string BaseUrl()
    {
        return _helpers.GetBaseUrl();
    }

    private static string DefaultCourseImagePath()
    {
        return Path.Combine("Images", ImageConsts.Course, ImageConsts.DefaultCourseImage);
    }

    private async Task<ActivationRequirements> GetActivationRequirementsAsync(
        int courseId,
        CancellationToken cancellationToken)
    {
        var stats = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new
            {
                TotalSections = c.Sections.Count(s => !s.IsDeleted),
                TotalLessons = c.Sections
                    .Where(s => !s.IsDeleted)
                    .SelectMany(s => s.Lessons)
                    .Count(l => !l.IsDeleted),
                PublishedLessons = c.Sections
                    .Where(s => !s.IsDeleted)
                    .SelectMany(s => s.Lessons)
                    .Count(l => !l.IsDeleted && l.IsPublished)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var missingRequirements = new List<string>();

        if (stats?.TotalSections == 0)
            missingRequirements.Add("Course must have at least one section.");

        if (stats?.TotalLessons == 0)
            missingRequirements.Add("Course must have at least one lesson.");

        if (stats?.PublishedLessons == 0)
            missingRequirements.Add("Course must have at least one published lesson.");

        return new ActivationRequirements
        {
            HasSections = stats?.TotalSections > 0,
            HasLessons = stats?.TotalLessons > 0,
            HasPublishedLessons = stats?.PublishedLessons > 0,
            TotalSections = stats?.TotalSections ?? 0,
            TotalLessons = stats?.TotalLessons ?? 0,
            PublishedLessons = stats?.PublishedLessons ?? 0,
            MissingRequirements = missingRequirements
        };
    }

    private static CourseAvailableActions BuildAvailableActions(
        CourseStatus status,
        bool isOwner,
        bool isCoInstructor)
    {
        return new CourseAvailableActions
        {
            // Both Owner and CoInstructor can edit
            CanEdit = true,
            CanAddSections = true,
            CanAddLessons = true,

            // Only Owner can do these
            CanDelete = isOwner,
            CanManageStudents = isOwner,
            CanManageInstructors = isOwner,

            // Status transitions (Owner only)
            CanActivate = isOwner && status == CourseStatus.Pending,
            CanComplete = isOwner && status == CourseStatus.Active,
            CanReactivate = isOwner && status == CourseStatus.Completed,
            CanUnpublish = isOwner && status == CourseStatus.Active
        };
    }

    private async Task<Result<CourseMetadataResponse>> BuildCourseMetadataResponse(Course course, string? userId,
        int courseId, CancellationToken cancellationToken)
    {
        var ownerUser = await _userManager.FindByIdAsync(course.CreatedById);

        if (ownerUser is null)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        CourseUser? userCourseRole = null;
        if (userId is not null)
            userCourseRole = await _context.CourseUsers
                .AsNoTracking()
                .Include(cu => cu.CourseRole)
                .FirstOrDefaultAsync(cu =>
                        cu.CourseId == courseId &&
                        cu.UserId == userId &&
                        !cu.IsDeleted,
                    cancellationToken);

        var studentLevel = (int)CourseRoleType.Student;

        var numberOfStudents = await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .CountAsync(cu =>
                    cu.CourseId == courseId &&
                    cu.CourseRole.Level == studentLevel &&
                    !cu.IsDeleted,
                cancellationToken);

        var isBookmarked = userId is not null &&
                           await _context.CourseBookmarks.AnyAsync(
                               cb => cb.CourseId == courseId && cb.UserId == userId && !cb.IsDeleted,
                               cancellationToken);

        var response = course.Adapt<CourseMetadataResponse>() with
        {
            InstructorName = course.DisplayInstructorName ?? $"{ownerUser.FirstName} {ownerUser.LastName}",

            ImageUrl = Path.Combine(BaseUrl(), course.ImageUrl),

            Tags = course.Tags.Select(c => c.Name).ToList(),

            NumberOfStudents = numberOfStudents,

            IsEnrolled = userCourseRole is not null,

            IsBookmarked = isBookmarked,

            IsOwner = userCourseRole?.CourseRole.Level == (int)CourseRoleType.CourseOwner
        };

        return Result.Success(response);
    }
}
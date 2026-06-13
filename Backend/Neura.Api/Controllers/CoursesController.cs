using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.Courses.ActivateCourse;
using Neura.Api.Features.Courses.CompleteCourse;
using Neura.Api.Features.Courses.CreateCourse;
using Neura.Api.Features.Courses.DeleteCourse;
using Neura.Api.Features.Courses.GetAllCourses;
using Neura.Api.Features.Courses.GetBookmarkedCourses;
using Neura.Api.Features.Courses.GetCourseContent;
using Neura.Api.Features.Courses.GetCourseFullContent;
using Neura.Api.Features.Courses.GetCourseMetadata;
using Neura.Api.Features.Courses.GetCourseStatus;
using Neura.Api.Features.Courses.GetEditableCourses;
using Neura.Api.Features.Courses.ReactivateCourse;
using Neura.Api.Features.Courses.ToggleCourseBookmark;
using Neura.Api.Features.Courses.UnpublishCourse;
using Neura.Api.Features.Courses.UpdateCourseDetails;
using Neura.Api.Features.Courses.UpdateCourseImage;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Course;
using Neura.Core.Contracts.Courses;
using Neura.Core.Contracts.Files;
using Neura.Core.Enums;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CoursesController(ISender sender) : ControllerBase
{
    // ====================
    // READ OPERATIONS
    // ====================

    /// <summary>
    ///     Retrieves a paginated list of courses based on dynamic filters.
    /// </summary>
    /// <remarks>
    ///     Anonymous access is allowed. If the user is authenticated, bookmark and enrollment
    ///     status will be populated for each course.
    /// </remarks>
    /// <param name="filters">Pagination, search, and sorting parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated list of course summaries.</returns>
    /// <response code="200">Returns the paginated course list.</response>
    [ProducesResponseType(typeof(PaginatedList<CourseSummaryResponse>), StatusCodes.Status200OK)]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] RequestFilters filters,
        CancellationToken ct)
    {
        var query = new GetAllCoursesQuery(filters, User.GetUserId());
        var result = await sender.Send(query, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Retrieves the content (sections and lessons) for a specific course.
    /// </summary>
    /// <remarks>
    ///     Returns the course's sections and lessons hierarchy.
    ///     Provide the public HashId (e.g., "Xy7zK"), not the integer database ID.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The course content including sections and lessons.</returns>
    /// <response code="200">Returns the course content.</response>
    /// <response code="404">Course not found or HashId is invalid.</response>
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpGet("{courseId}/content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(
        [FromRoute] string courseId,
        CancellationToken ct)
    {
        var query = new GetCourseContentQuery(courseId, User.GetUserId());
        var result = await sender.Send(query, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Retrieves the full content of all courses including learning outcomes, prerequisites,
    ///     sections with their lessons, and article text for article-type lessons.
    /// </summary>
    /// <remarks>
    ///     Returns the complete course hierarchy with lesson text only for article-type lessons.
    ///     Non-article lessons will have null for LessonText.
    ///     CourseId is the raw integer database ID.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of all courses with their full content.</returns>
    /// <response code="200">Returns the full content of all courses.</response>
    [ProducesResponseType(typeof(List<CourseFullContentResponse>), StatusCodes.Status200OK)]
    [HttpGet("full-content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFullContent(
        CancellationToken ct)
    {
        var query = new GetCourseFullContentQuery();
        var result = await sender.Send(query, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Retrieves metadata (details, tags, stats) for a specific course.
    /// </summary>
    /// <remarks>
    ///     Returns course info, tags, learning outcomes, prerequisites, enrollment count,
    ///     and the caller's enrollment/bookmark/owner status.
    ///     Provide the public HashId (e.g., "Xy7zK"), not the integer database ID.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The course metadata.</returns>
    /// <response code="200">Returns the course metadata.</response>
    /// <response code="404">Course not found or HashId is invalid.</response>
    [ProducesResponseType(typeof(CourseMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpGet("{courseId}/metadata")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMetadataById(
        [FromRoute] string courseId,
        CancellationToken ct)
    {
        var query = new GetCourseMetadataQuery(courseId, User.GetUserId());
        var result = await sender.Send(query, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpGet("{courseId}/status")]
    [HasCoursePermission(CoursePermission.ViewAnalytics)]
    public async Task<IActionResult> GetStatus(
        [FromRoute] string courseId,
        CancellationToken ct)
    {
        var query = new GetCourseStatusQuery(courseId, User.GetUserId()!);
        var result = await sender.Send(query, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ====================================
    // WRITE OPERATIONS (Create / Update)
    // ====================================

    /// <summary>
    ///     Creates a new course and assigns the creator as the owner.
    /// </summary>
    /// <remarks>
    ///     The authenticated user is automatically added as the course owner.
    ///     A <c>Location</c> header pointing to the content endpoint is returned on success.
    /// </remarks>
    /// <param name="request">The course creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the Location header of the new course.</returns>
    /// <response code="201">Course created successfully.</response>
    /// <response code="400">Validation failed (e.g., invalid or duplicate tags).</response>
    [ProducesResponseType(typeof(CourseMetadataResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    //[InstructorOnly]
    [HttpPost("")]
    public async Task<IActionResult> Create(
        [FromForm] CourseRequest request,
        CancellationToken ct)
    {
        var command = new CreateCourseCommand(request, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMetadataById), new { courseId = result.Value.KeyId }, result.Value.KeyId)
            : result.ToProblem();
    }

    /// <summary>
    ///     Updates the details of a course (title, description, tags, dates, outcomes, prerequisites).
    /// </summary>
    /// <remarks>
    ///     Requires <see cref="CoursePermission.EditContent" /> permission on the course.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Course updated successfully.</response>
    /// <response code="404">Course or one or more tags not found.</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpPut("{courseId}")]
    [HasCoursePermission(CoursePermission.EditContent)]
    public async Task<IActionResult> UpdateDetails(
        [FromRoute] string courseId,
        [FromForm] CourseUpdateRequest request,
        CancellationToken ct)
    {
        var command = new UpdateCourseDetailsCommand(courseId, request, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    /// <summary>
    ///     Uploads or replaces the cover image for a course.
    /// </summary>
    /// <remarks>
    ///     Accepts <c>multipart/form-data</c>. The previous image is deleted automatically
    ///     unless it is the default placeholder.
    ///     Requires <see cref="CoursePermission.EditContent" /> permission on the course.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="uploadImage">The form file containing the new image.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Image updated successfully.</response>
    /// <response code="404">Course not found.</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpPut("{courseId}/cover-image")]
    [HasCoursePermission(CoursePermission.EditContent)]
    public async Task<IActionResult> UpdateImage(
        [FromRoute] string courseId,
        [FromForm] UploadImageRequest uploadImage,
        CancellationToken ct)
    {
        var command = new UpdateCourseImageCommand(courseId, uploadImage, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    /// <summary>
    ///     Gets all courses the current user can edit (as Owner or Co-Instructor).
    /// </summary>
    /// <remarks>
    ///     Returns courses where the user has CourseOwner or CoInstructor permissions.
    ///     Includes stats like student count, lesson count, and available actions.
    /// </remarks>
    [HttpGet("my/editable")]
    [ProducesResponseType(typeof(EditableCoursesListSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEditableCourses(
        [FromQuery] EditableCourseFilters filters,
        CancellationToken ct)
    {
        var query = new GetEditableCoursesQuery(filters, User.GetUserId()!);
        var result = await sender.Send(query, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ============================
    // ACTIONS (Enroll / Bookmark)
    // ============================

    /// <summary>
    ///     Toggles the bookmark status for a specific course.
    /// </summary>
    /// <remarks>
    ///     Adds the bookmark if it does not exist; removes it if it does.
    ///     Soft-deleted bookmarks are restored automatically.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Bookmark status toggled successfully.</response>
    /// <response code="404">Course not found.</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpPost("{courseId}/bookmark")]
    public async Task<IActionResult> ToggleBookmark(
        [FromRoute] string courseId,
        CancellationToken ct)
    {
        var command = new ToggleCourseBookmarkCommand(courseId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    /// <summary>
    ///     Retrieves a paginated list of courses bookmarked by the current user.
    /// </summary>
    /// <param name="filters">Pagination, search, and sorting parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated list of bookmarked course summaries.</returns>
    /// <response code="200">Returns the bookmarked courses.</response>
    [ProducesResponseType(typeof(PaginatedList<CourseSummaryResponse>), StatusCodes.Status200OK)]
    [HttpGet("bookmarked")]
    public async Task<IActionResult> GetBookmarked(
        [FromQuery] RequestFilters filters,
        CancellationToken ct)
    {
        var query = new GetBookmarkedCoursesQuery(filters, User.GetUserId()!);
        var result = await sender.Send(query, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════════════════════════
    // Commands — Status Transitions
    // ══════════════════════════════════════════════════════════════

    [HasCoursePermission(CoursePermission.ManageSettings)]
    [HttpPost("{courseId}/activate")]
    public async Task<IActionResult> Activate(
        [FromRoute] string courseId,
        CancellationToken ct)
    {
        var command = new ActivateCourseCommand(courseId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HasCoursePermission(CoursePermission.ManageSettings)]
    [HttpPost("{courseId}/complete")]
    public async Task<IActionResult> Complete(
        [FromRoute] string courseId,
        CancellationToken ct)
    {
        var command = new CompleteCourseCommand(courseId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HasCoursePermission(CoursePermission.ManageSettings)]
    [HttpPost("{courseId}/reactivate")]
    public async Task<IActionResult> Reactivate(
        [FromRoute] string courseId,
        CancellationToken ct)
    {
        var command = new ReactivateCourseCommand(courseId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HasCoursePermission(CoursePermission.ManageSettings)]
    [HttpPost("{courseId}/unpublish")]
    public async Task<IActionResult> Unpublish(
        [FromRoute] string courseId,
        CancellationToken ct)
    {
        var command = new UnpublishCourseCommand(courseId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Soft deletes a course.
    /// </summary>
    [HasCoursePermission(CoursePermission.DeleteCourse)]
    [HttpDelete("{courseId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(
        [FromRoute] string courseId,
        CancellationToken ct)
    {
        var command = new DeleteCourseCommand(courseId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
}
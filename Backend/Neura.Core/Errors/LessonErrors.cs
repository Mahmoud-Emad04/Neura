using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class LessonErrors
{
	// 1. EXISTENCE
	public static readonly Error NotFound = new(
		"Lesson.NotFound",
		"The requested lesson was not found.",
		StatusCodes.Status404NotFound);

	public static readonly Error InvalidId = new(
		"Lesson.InvalidId",
		"The specified lesson ID is invalid.",
		StatusCodes.Status400BadRequest);
	public static readonly Error LessonPositionConflict =
		new("Lesson.PositionConflict", "Another lesson in this section already uses the same position.",
			StatusCodes.Status409Conflict);

	// 2. VIDEO & FILE HANDLING
	public static readonly Error VideoNotFound = new(
		"Lesson.VideoNotFound",
		"This lesson is marked as a video, but no video file is associated with it.",
		StatusCodes.Status404NotFound);

	public static readonly Error VideoTooLarge = new("Lesson.VideoTooLarge",
		"Video file size exceeds maximum allowed limit of 500MB.", StatusCodes.Status400BadRequest);

	public static readonly Error VideoTooLong = new("Lesson.VideoTooLong",
		"Video duration exceeds maximum allowed limit of 20 minutes.",
		StatusCodes.Status400BadRequest);

	public static readonly Error InvalidVideo = new("Lesson.InvalidVideo",
		"Unable to validate video duration or file is invalid.", StatusCodes.Status400BadRequest);

	public static readonly Error FileNotFound = new(
		"Lesson.FileNotFound",
		"The physical video file could not be found on the server.",
		StatusCodes.Status500InternalServerError);

	public static readonly Error InvalidVideoFormat = new(
		"Lesson.InvalidVideoFormat",
		"Only .mp4 and .webm formats are supported.",
		StatusCodes.Status400BadRequest);

	public static readonly Error VideoRequired = new(
		"Lesson.VideoRequired",
		"A video file is required for this lesson type.",
		StatusCodes.Status400BadRequest);

	// 3. ACCESS CONTROL & SCHEDULING
	public static readonly Error NotEnrolled = new(
		"Lesson.NotEnrolled",
		"You must be enrolled in the course to view this lesson.",
		StatusCodes.Status403Forbidden);

	public static readonly Error NotAvailableYet = new(
		"Lesson.NotAvailableYet",
		"This lesson is scheduled for a future date and is not yet available.",
		StatusCodes.Status403Forbidden);

	public static readonly Error PreviewNotAllowed = new(
		"Lesson.PreviewNotAllowed",
		"This lesson is not available for free preview.",
		StatusCodes.Status403Forbidden);

	// 4. POSITION & MANAGEMENT
	public static readonly Error InvalidPosition = new(
		"Lesson.InvalidPosition",
		"The specified position is invalid for this lesson's section.",
		StatusCodes.Status400BadRequest);

	public static readonly Error PositionOutOfRange = new(
		"Lesson.PositionOutOfRange",
		"Position must be between 1 and the total number of lessons in the section.",
		StatusCodes.Status400BadRequest);

	public static readonly Error CannotDeleteLesson = new(
		"Lesson.CannotDeleteLesson",
		"This lesson cannot be deleted.",
		StatusCodes.Status400BadRequest);

	public static readonly Error UnauthorizedModification = new(
		"Lesson.UnauthorizedModification",
		"You do not have permission to modify this lesson.",
		StatusCodes.Status403Forbidden);

	public static readonly Error SectionNotFound = new(
		"Lesson.SectionNotFound",
		"The lesson's section could not be found.",
		StatusCodes.Status404NotFound);
}
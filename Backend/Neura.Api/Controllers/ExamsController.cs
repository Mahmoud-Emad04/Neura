using Neura.Api.Extensions;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.Exam;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;

    public ExamsController(IExamService examService)
    {
        _examService = examService;
    }

    // ══════════════════════════════════════════
    //  POST /api/exams
    // ══════════════════════════════════════════
    [HttpPost]
    [ProducesResponseType(typeof(ExamResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateExamRequest request)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.CreateAsync(request, userId);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { lessonId = result.Value.LessonId }, result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  GET /api/exams/{examId}
    // ══════════════════════════════════════════
    [HttpGet("{lessonId:int}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(ExamDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int lessonId)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.GetByIdAsync(lessonId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  GET /api/exams/by-lesson/{lessonId}
    // ══════════════════════════════════════════
    [HttpGet("by-lesson/{lessonId:int}")]
    [ProducesResponseType(typeof(ExamDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLessonId([FromRoute] int lessonId)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.GetByLessonIdAsync(lessonId, userId);

        return result.IsSuccess
             ? Ok(result.Value)
             : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exams/{examId}/settings
    // ══════════════════════════════════════════
    [HttpPut("{lessonId:int}/settings")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(typeof(ExamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings(
        [FromRoute] int lessonId,
        [FromBody] UpdateExamSettingsRequest request)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.UpdateSettingsAsync(lessonId, request, userId);

        return result.IsSuccess
             ? Ok(result.Value)
             : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exams/{examId}/publish
    // ══════════════════════════════════════════
    [HttpPut("{lessonId:int}/publish")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish([FromRoute] int lessonId)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.PublishAsync(lessonId, userId);

        return result.IsSuccess
             ? Ok()
             : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exams/{lessonId}/unpublish
    // ══════════════════════════════════════════
    [HttpPut("{lessonId:int}/unpublish")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unpublish([FromRoute] int lessonId)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.UnpublishAsync(lessonId, userId);

        return result.IsSuccess
             ? Ok()
             : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  DELETE /api/exams/{lessonId}
    // ══════════════════════════════════════════
    [HttpDelete("{lessonId:int}")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int lessonId)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.DeleteAsync(lessonId, userId);

        return result.IsSuccess
             ? Ok()
             : result.ToProblem();
    }
}

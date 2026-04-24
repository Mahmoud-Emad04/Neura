using Neura.Api.Extensions;
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
            ? CreatedAtAction(nameof(GetById), new { examId = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  GET /api/exams/{examId}
    // ══════════════════════════════════════════
    [HttpGet("{examId:int}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(ExamDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int examId)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.GetByIdAsync(examId, userId);

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
    [HttpPut("{examId:int}/settings")]
    [ProducesResponseType(typeof(ExamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings(
        [FromRoute] int examId,
        [FromBody] UpdateExamSettingsRequest request)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.UpdateSettingsAsync(examId, request, userId);

        return result.IsSuccess
             ? Ok(result.Value)
             : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exams/{examId}/publish
    // ══════════════════════════════════════════
    [HttpPut("{examId:int}/publish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish([FromRoute] int examId)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.PublishAsync(examId, userId);

        return result.IsSuccess
             ? Ok()
             : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exams/{examId}/unpublish
    // ══════════════════════════════════════════
    [HttpPut("{examId:int}/unpublish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unpublish([FromRoute] int examId)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.UnpublishAsync(examId, userId);

        return result.IsSuccess
             ? Ok()
             : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  DELETE /api/exams/{examId}
    // ══════════════════════════════════════════
    [HttpDelete("{examId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int examId)
    {
        var userId = User.GetUserId()!;
        var result = await _examService.DeleteAsync(examId, userId);

        return result.IsSuccess
             ? Ok()
             : result.ToProblem();
    }
}

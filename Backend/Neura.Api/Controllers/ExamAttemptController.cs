using Neura.Api.Extensions;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExamAttemptsController : ControllerBase
{
    private readonly IExamAttemptService _attemptService;

    public ExamAttemptsController(IExamAttemptService attemptService)
    {
        _attemptService = attemptService;
    }

    // ══════════════════════════════════════════
    //  GET /api/exam-attempts/exam/{examId}/info
    //  Student landing page — exam info + attempt status
    // ══════════════════════════════════════════
    [HttpGet("exam/{lessonId:int}/info")]
    [ProducesResponseType(typeof(ExamInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamInfo([FromRoute] int lessonId)
    {
        var userId = User.GetUserId()!;
        var result = await _attemptService.GetExamInfoAsync(lessonId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  POST /api/exam-attempts/exam/{examId}/start
    //  Start a new attempt
    // ══════════════════════════════════════════
    [HttpPost("exam/{lessonId:int}/start")]
    [ProducesResponseType(typeof(StartAttemptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartAttempt([FromRoute] int lessonId)
    {
        var userId = User.GetUserId()!;
        var result = await _attemptService.StartAttemptAsync(lessonId, userId);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, result.Value);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  GET /api/exam-attempts/{attemptId}/resume
    //  Resume an in-progress attempt (page refresh)
    // ══════════════════════════════════════════
    [HttpGet("{attemptId:int}/resume")]
    [ProducesResponseType(typeof(StartAttemptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeAttempt([FromRoute] int attemptId)
    {
        var userId = User.GetUserId()!;
        var result = await _attemptService.ResumeAttemptAsync(attemptId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exam-attempts/{attemptId}/answers/{questionId}
    //  Auto-save a single answer
    // ══════════════════════════════════════════
    [HttpPut("{attemptId:int}/answers/{questionId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveAnswer(
        [FromRoute] int attemptId,
        [FromRoute] int questionId,
        [FromBody] SaveAnswerRequest request)
    {
        var userId = User.GetUserId()!;
        var result = await _attemptService.SaveAnswerAsync(attemptId, questionId, request, userId);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  POST /api/exam-attempts/{attemptId}/submit
    //  Final submit + grading
    // ══════════════════════════════════════════
    [HttpPost("{attemptId:int}/submit")]
    [ProducesResponseType(typeof(SubmitAttemptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit([FromRoute] int attemptId)
    {
        var userId = User.GetUserId()!;
        var result = await _attemptService.SubmitAsync(attemptId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  GET /api/exam-attempts/{attemptId}/results
    //  View results after submission
    // ══════════════════════════════════════════
    [HttpGet("{attemptId:int}/results")]
    [ProducesResponseType(typeof(AttemptResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResults([FromRoute] int attemptId)
    {
        var userId = User.GetUserId()!;
        var result = await _attemptService.GetResultsAsync(attemptId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  POST /api/exam-attempts/{attemptId}/violations
    //  Record tab-switch violation
    // ══════════════════════════════════════════
    [HttpPost("{attemptId:int}/violations")]
    [ProducesResponseType(typeof(ViolationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordViolation(
        [FromRoute] int attemptId,
        [FromBody] ViolationRequest request)
    {
        var userId = User.GetUserId()!;
        var result = await _attemptService.RecordViolationAsync(attemptId, request, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}

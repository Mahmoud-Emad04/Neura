using Neura.Api.Extensions;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Controllers;

[Route("api/exams/{examId:int}/analytics")]
[ApiController]
[HasExamPermission(Core.Enums.CoursePermission.ViewAnalytics)]
public class ExamAnalyticsController : ControllerBase
{
    private readonly IExamAnalyticsService _analyticsService;

    public ExamAnalyticsController(IExamAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    // ══════════════════════════════════════════
    //  GET /api/exams/{examId}/analytics
    //  Full dashboard — overview + per-question breakdown
    // ══════════════════════════════════════════
    [HttpGet]
    [ProducesResponseType(typeof(ExamAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalytics([FromRoute] int examId)
    {
        var userId = User.GetUserId()!;
        var result = await _analyticsService.GetExamAnalyticsAsync(examId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  GET /api/exams/{examId}/analytics/attempts
    //  Paginated student attempts list
    // ══════════════════════════════════════════
    [HttpGet("attempts")]
    [ProducesResponseType(typeof(ExamStudentAttemptsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentAttempts(
        [FromRoute] int examId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = true)
    {
        var userId = User.GetUserId()!;
        var result = await _analyticsService.GetStudentAttemptsAsync(examId, userId, page, pageSize, sortBy, descending);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  GET /api/exams/{examId}/analytics/attempts/{attemptId}
    //  Instructor views a specific student's attempt detail
    // ══════════════════════════════════════════
    [HttpGet("attempts/{attemptId:int}")]
    [ProducesResponseType(typeof(AttemptResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentAttemptDetail(
        [FromRoute] int examId,
        [FromRoute] int attemptId)
    {
        var userId = User.GetUserId()!;
        var result = await _analyticsService.GetStudentAttemptDetailAsync(attemptId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  GET /api/exams/{examId}/analytics/score-distribution
    //  Score histogram data
    // ══════════════════════════════════════════
    [HttpGet("score-distribution")]
    [ProducesResponseType(typeof(ScoreDistributionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScoreDistribution([FromRoute] int examId)
    {
        var userId = User.GetUserId()!;
        var result = await _analyticsService.GetScoreDistributionAsync(examId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}

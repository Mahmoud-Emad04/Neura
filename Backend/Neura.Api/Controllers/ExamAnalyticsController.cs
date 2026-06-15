using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.ExamAnalytics.GetExamAnalytics;
using Neura.Api.Features.ExamAnalytics.GetExamViolationById;
using Neura.Api.Features.ExamAnalytics.GetExamViolations;
using Neura.Api.Features.ExamAnalytics.GetScoreDistribution;
using Neura.Api.Features.ExamAnalytics.GetStudentAttemptDetail;
using Neura.Api.Features.ExamAnalytics.GetStudentAttempts;
using Neura.Api.Features.ExamAnalytics.GetStudentExamAnalytics;
using Neura.Api.Features.ExamAnalytics.GetStudentOwnAttempts;
using Neura.Api.Features.ExamAnalytics.GetStudentScoreDistribution;
using Neura.Api.Features.ExamAttempts.FlagAttemptViolation;
using Neura.Api.Features.ExamAttempts.ResolveViolation;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Controllers;

[Route("api/exams/{examId:int}/analytics")]
[ApiController]
[Authorize]
public class ExamAnalyticsController(ISender sender) : ControllerBase
{
    // ==========================================
    //  GET /api/exams/{examId}/analytics
    //  Full dashboard - overview + per-question breakdown
    // ==========================================
    [HttpGet]
    [HasExamPermission(Core.Enums.CoursePermission.ViewAnalytics)]
    [ProducesResponseType(typeof(ExamAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalytics([FromRoute] int examId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetExamAnalyticsQuery(examId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/{examId}/analytics/attempts
    //  Paginated student attempts list
    // ==========================================
    [HttpGet("attempts")]
    [HasExamPermission(Core.Enums.CoursePermission.ViewAnalytics)]
    [ProducesResponseType(typeof(ExamStudentAttemptsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentAttempts(
        [FromRoute] int examId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = true,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId()!;
        var query = new GetStudentAttemptsQuery(examId, userId, page, pageSize, sortBy, descending);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/{examId}/analytics/attempts/{attemptId}
    //  Instructor views a specific student's attempt detail
    // ==========================================
    [HttpGet("attempts/{attemptId:int}")]
    [HasExamPermission(Core.Enums.CoursePermission.ViewAnalytics)]
    [ProducesResponseType(typeof(AttemptResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentAttemptDetail(
        [FromRoute] int examId,
        [FromRoute] int attemptId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetStudentAttemptDetailQuery(examId, attemptId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/{examId}/analytics/score-distribution
    //  Score histogram data
    // ==========================================
    [HttpGet("score-distribution")]
    [HasExamPermission(Core.Enums.CoursePermission.ViewAnalytics)]
    [ProducesResponseType(typeof(ScoreDistributionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScoreDistribution([FromRoute] int examId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetScoreDistributionQuery(examId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/{examId}/analytics/student
    //  Student views their own analytics and class comparison
    // ==========================================
    [HttpGet("student")]
    [ProducesResponseType(typeof(StudentExamAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentAnalytics([FromRoute] int examId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetStudentExamAnalyticsQuery(examId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/{examId}/analytics/student/score-distribution
    //  Student views score histogram data for the exam
    // ==========================================
    [HttpGet("student/score-distribution")]
    [ProducesResponseType(typeof(ScoreDistributionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentScoreDistribution([FromRoute] int examId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetStudentScoreDistributionQuery(examId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/{examId}/analytics/student/attempts
    //  Student views their own paginated attempts list
    // ==========================================
    [HttpGet("student/attempts")]
    [ProducesResponseType(typeof(ExamStudentAttemptsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentOwnAttempts(
        [FromRoute] int examId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = true,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId()!;
        var query = new GetStudentOwnAttemptsQuery(examId, userId, page, pageSize, sortBy, descending);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  PUT /api/exams/{examId}/analytics/attempts/{attemptId}/flag-violation
    //  Instructor flags an attempt for cheating/violation
    // ==========================================
    [HttpPut("attempts/{attemptId:int}/flag-violation")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FlagViolation(
        [FromRoute] int examId,
        [FromRoute] int attemptId,
        [FromBody] FlagAttemptViolationRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new FlagAttemptViolationCommand(attemptId, request.Reason, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    // ==========================================
    //  PUT /api/exams/{examId}/analytics/attempts/{attemptId}/resolve-violation
    //  Instructor resolves a violation and overrides the grade
    // ==========================================
    [HttpPut("attempts/{attemptId:int}/resolve-violation")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveViolation(
        [FromRoute] int examId,
        [FromRoute] int attemptId,
        [FromBody] ResolveViolationRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new ResolveViolationCommand(attemptId, request.NewScore, request.Notes, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/{examId}/analytics/violations
    //  Course owner retrieves all violations for an exam
    // ==========================================
    [HttpGet("violations")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(typeof(ExamViolationsListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetViolations([FromRoute] int examId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetExamViolationsQuery(examId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/{examId}/analytics/violations/{violationId}
    //  Course owner retrieves a specific violation by ID
    // ==========================================
    [HttpGet("violations/{violationId:int}")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(typeof(ExamViolationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetViolationById(
        [FromRoute] int examId,
        [FromRoute] int violationId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetExamViolationByIdQuery(examId, violationId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}

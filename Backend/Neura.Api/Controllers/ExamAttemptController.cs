using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.ExamAttempts.GetExamInfo;
using Neura.Api.Features.ExamAttempts.GetResults;
using Neura.Api.Features.ExamAttempts.RecordViolation;
using Neura.Api.Features.ExamAttempts.ResumeAttempt;
using Neura.Api.Features.ExamAttempts.SaveAnswer;
using Neura.Api.Features.ExamAttempts.StartAttempt;
using Neura.Api.Features.ExamAttempts.SubmitAttempt;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Controllers;

[Route("api/exam-attempts")]
[ApiController]
[Authorize]
public class ExamAttemptsController(ISender sender) : ControllerBase
{
    // ══════════════════════════════════════════
    //  GET /api/exam-attempts/exam/{lessonId}/info
    //  Student landing page — exam info + attempt status
    // ══════════════════════════════════════════
    [HttpGet("exam/{lessonId:int}/info")]
    [ProducesResponseType(typeof(ExamInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamInfo(
        [FromRoute] int lessonId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetExamInfoQuery(lessonId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  POST /api/exam-attempts/exam/{lessonId}/start
    //  Start a new attempt
    // ══════════════════════════════════════════
    [HttpPost("exam/{lessonId:int}/start")]
    [ProducesResponseType(typeof(StartAttemptResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartAttempt(
        [FromRoute] int lessonId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new StartAttemptCommand(lessonId, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
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
    public async Task<IActionResult> ResumeAttempt(
        [FromRoute] int attemptId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new ResumeAttemptCommand(attemptId, userId);
        var result = await sender.Send(command, ct);

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
        [FromBody] SaveAnswerRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new SaveAnswerCommand(attemptId, questionId, request, userId);
        var result = await sender.Send(command, ct);

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
    public async Task<IActionResult> Submit(
        [FromRoute] int attemptId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new SubmitAttemptCommand(attemptId, userId);
        var result = await sender.Send(command, ct);

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
    public async Task<IActionResult> GetResults(
        [FromRoute] int attemptId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetResultsQuery(attemptId, userId);
        var result = await sender.Send(query, ct);

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
        [FromBody] ViolationRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new RecordViolationCommand(attemptId, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}

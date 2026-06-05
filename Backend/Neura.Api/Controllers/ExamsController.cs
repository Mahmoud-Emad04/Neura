using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.Exam;
using Neura.Api.Extensions;
using Neura.Api.Features.Exams.CreateExam;
using Neura.Api.Features.Exams.DeleteExam;
using Neura.Api.Features.Exams.GetExamById;
using Neura.Api.Features.Exams.GetExamByLessonId;
using Neura.Api.Features.Exams.PublishExam;
using Neura.Api.Features.Exams.UnpublishExam;
using Neura.Api.Features.Exams.UpdateExamSettings;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExamsController(ISender sender) : ControllerBase
{
    // ==========================================
    //  POST /api/exams
    // ==========================================
    [HttpPost]
    [ProducesResponseType(typeof(ExamResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateExamRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new CreateExamCommand(request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { lessonId = result.Value.LessonId }, result.Value)
            : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/{examId}
    // ==========================================
    [HttpGet("{lessonId:int}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(ExamDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int lessonId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetExamByIdQuery(lessonId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  GET /api/exams/by-lesson/{lessonId}
    // ==========================================
    [HttpGet("by-lesson/{lessonId:int}")]
    [ProducesResponseType(typeof(ExamDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLessonId([FromRoute] int lessonId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetExamByLessonIdQuery(lessonId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  PUT /api/exams/{examId}/settings
    // ==========================================
    [HttpPut("{lessonId:int}/settings")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(typeof(ExamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings(
        [FromRoute] int lessonId,
        [FromBody] UpdateExamSettingsRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new UpdateExamSettingsCommand(lessonId, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    //  PUT /api/exams/{examId}/publish
    // ==========================================
    [HttpPut("{lessonId:int}/publish")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish([FromRoute] int lessonId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new PublishExamCommand(lessonId, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    // ==========================================
    //  PUT /api/exams/{lessonId}/unpublish
    // ==========================================
    [HttpPut("{lessonId:int}/unpublish")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unpublish([FromRoute] int lessonId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new UnpublishExamCommand(lessonId, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    // ==========================================
    //  DELETE /api/exams/{lessonId}
    // ==========================================
    [HttpDelete("{lessonId:int}")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int lessonId, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new DeleteExamCommand(lessonId, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }
}

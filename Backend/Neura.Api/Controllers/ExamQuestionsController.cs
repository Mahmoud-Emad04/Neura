using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.ExamQuestions.AddQuestion;
using Neura.Api.Features.ExamQuestions.DeleteQuestion;
using Neura.Api.Features.ExamQuestions.ReorderQuestions;
using Neura.Api.Features.ExamQuestions.UpdateQuestion;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.Question;

namespace Neura.Api.Controllers;

[Route("api/exams/{lessonId:int}/questions")]
[ApiController]
[Authorize]
public class ExamQuestionsController(ISender sender) : ControllerBase
{
    // ══════════════════════════════════════════
    //  POST /api/exams/{lessonId}/questions
    // ══════════════════════════════════════════
    [HttpPost]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(typeof(QuestionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(
        [FromRoute] int lessonId,
        [FromBody] CreateQuestionRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new AddQuestionCommand(lessonId, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Add), new { lessonId = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exams/{lessonId}/questions/{questionId}
    // ══════════════════════════════════════════
    [HttpPut("{questionId:int}")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(typeof(QuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int lessonId,
        [FromRoute] int questionId,
        [FromBody] UpdateQuestionRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new UpdateQuestionCommand(questionId, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  DELETE /api/exams/{lessonId}/questions/{questionId}
    // ══════════════════════════════════════════
    [HttpDelete("{questionId:int}")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] int lessonId,
        [FromRoute] int questionId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new DeleteQuestionCommand(questionId, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exams/{lessonId}/questions/reorder
    // ══════════════════════════════════════════
    [HttpPut("reorder")]
    [HasExamPermission(Core.Enums.CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(
        [FromRoute] int lessonId,
        [FromBody] ReorderQuestionsRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new ReorderQuestionsCommand(lessonId, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }
}

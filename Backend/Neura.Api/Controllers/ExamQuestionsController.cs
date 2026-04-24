using Neura.Api.Extensions;
using Neura.Core.Contracts.Question;

namespace Neura.Api.Controllers;

[Route("api/exams/{examId:int}/questions")]
[ApiController]
[Authorize]
public class ExamQuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;

    public ExamQuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    // ══════════════════════════════════════════
    //  POST /api/exams/{examId}/questions
    // ══════════════════════════════════════════
    [HttpPost]
    [ProducesResponseType(typeof(QuestionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(
        [FromRoute] int examId,
        [FromBody] CreateQuestionRequest request)
    {
        var userId = User.GetUserId()!;
        var result = await _questionService.AddAsync(examId, request, userId);

        return result.IsSuccess
        ? CreatedAtAction(nameof(Add), new { examId = result.Value.Id }, result.Value)
        : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exams/{examId}/questions/{questionId}
    // ══════════════════════════════════════════
    [HttpPut("{questionId:int}")]
    [ProducesResponseType(typeof(QuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int examId,
        [FromRoute] int questionId,
        [FromBody] UpdateQuestionRequest request)
    {
        var userId = User.GetUserId()!;
        var result = await _questionService.UpdateAsync(questionId, request, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  DELETE /api/exams/{examId}/questions/{questionId}
    // ══════════════════════════════════════════
    [HttpDelete("{questionId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] int examId,
        [FromRoute] int questionId)
    {
        var userId = User.GetUserId()!;
        var result = await _questionService.DeleteAsync(questionId, userId);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }

    // ══════════════════════════════════════════
    //  PUT /api/exams/{examId}/questions/reorder
    // ══════════════════════════════════════════
    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(
        [FromRoute] int examId,
        [FromBody] ReorderQuestionsRequest request)
    {
        var userId = User.GetUserId()!;
        var result = await _questionService.ReorderAsync(examId, request, userId);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }
}

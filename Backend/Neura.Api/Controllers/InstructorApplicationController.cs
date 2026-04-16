using System.Security.Claims;
using Neura.Core.InstructorApplication;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/instructor")]
[Authorize]
public class InstructorApplicationController : ControllerBase
{
    private readonly IInstructorApplicationService _applicationService;

    public InstructorApplicationController(IInstructorApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    /// <summary>
    ///     Submit a new instructor application
    /// </summary>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitApplication([FromBody] SubmitApplicationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _applicationService.SubmitApplicationAsync(userId, request);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMyApplicationStatus), result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Get current user's application status
    /// </summary>
    [HttpGet("application")]
    [ProducesResponseType(typeof(MyApplicationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyApplicationStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _applicationService.GetMyApplicationStatusAsync(userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Update pending application
    /// </summary>
    [HttpPut("application")]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApplication([FromBody] UpdateApplicationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _applicationService.UpdateApplicationAsync(userId, request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}
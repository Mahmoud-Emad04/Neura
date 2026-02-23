using Neura.Core.Contracts.Instructor;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Get Instructor By CourseId
    /// </summary>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(InstructorSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpGet("course/{courseId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInstructorByCourseId(string courseId, CancellationToken cancellationToken)
    {
        var response = await _userService.GetInstructorByCourseId(courseId, cancellationToken);
        return response.IsSuccess ?
            Ok(response.Value) :
            response.ToProblem();
    }
}

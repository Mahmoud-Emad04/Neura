using Neura.Api.Extensions;
using Neura.Core.Contracts.Users;

namespace Neura.Api.Controllers;

[Route("me")]
[ApiController]
[Authorize]
public class AccountController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    ///     Gets the current user's profile.
    ///     Route: GET /me
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Info()
    {
        var result = await _userService.GetProfileAsync(User.GetUserId()!);

        return Ok(result.Value);
    }

    /// <summary>
    ///     Updates the current user's profile details.
    ///     Route: PUT /me
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
    {
        await _userService.UpdateProfileAsync(User.GetUserId()!, request);

        return NoContent();
    }

    /// <summary>
    ///     Changes the current user's password.
    ///     Route: PUT /me/password
    /// </summary>
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _userService.ChangePasswordAsync(User.GetUserId()!, request);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
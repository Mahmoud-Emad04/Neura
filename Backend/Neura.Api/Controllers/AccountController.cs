using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.Account.ChangePassword;
using Neura.Api.Features.Account.GetProfile;
using Neura.Api.Features.Account.UpdateProfile;
using Neura.Core.Contracts.Users;

namespace Neura.Api.Controllers;

[Route("me")]
[ApiController]
[Authorize]
public class AccountController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Gets the current user's profile.
    ///     Route: GET /me
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Info(CancellationToken ct)
    {
        var query = new GetProfileQuery(User.GetUserId()!);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Updates the current user's profile details.
    ///     Route: PUT /me
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateProfileCommand(User.GetUserId()!, request);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Changes the current user's password.
    ///     Route: PUT /me/password
    /// </summary>
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var command = new ChangePasswordCommand(User.GetUserId()!, request);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
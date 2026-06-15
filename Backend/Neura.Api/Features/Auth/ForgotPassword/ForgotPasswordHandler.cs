using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Neura.Core.Errors;
using System.Text;

namespace Neura.Api.Features.Auth.ForgotPassword;

internal sealed class ForgotPasswordHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<ForgotPasswordHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(
        ForgotPasswordCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (await userManager.FindByEmailAsync(request.Email) is not { } user)
            return Result.Success();

        if (!user.EmailConfirmed)
            return Result.Failure(UserErrors.EmailNotConfirmed);

        var origin = command.Origin ?? "https://neuralearning.netlify.app";

        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        logger.LogInformation("Reset code: {code}", code);

        BackgroundJob.Enqueue<AuthJobs>(jobs => jobs.SendResetPasswordEmail(user.Email!, user.FirstName, code));

        return Result.Success();
    }
}

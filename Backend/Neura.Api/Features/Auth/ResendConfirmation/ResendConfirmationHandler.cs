using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Neura.Core.Errors;
using System.Text;

namespace Neura.Api.Features.Auth.ResendConfirmation;

internal sealed class ResendConfirmationHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<ResendConfirmationHandler> logger)
    : IRequestHandler<ResendConfirmationCommand, Result>
{
    public async Task<Result> Handle(
        ResendConfirmationCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (await userManager.FindByEmailAsync(request.Email) is not { } user)
            return Result.Success();

        if (user.EmailConfirmed)
            return Result.Failure(UserErrors.DuplicatedConfirmation);

        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        logger.LogInformation("Confirmation code: {code}", code);

        var origin = command.Origin ?? "https://neuralearning.netlify.app";

        BackgroundJob.Enqueue<AuthJobs>(jobs => jobs.SendConfirmationEmail(user.Email!, user.FirstName, user.Id, code));

        return Result.Success();
    }
}

using Hangfire;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Authentication;
using Neura.Core.Entities;
using Neura.Core.Errors;
using System.Text;

namespace Neura.Api.Features.Auth.Register;

internal sealed class RegisterHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<RegisterHandler> logger) 
    : IRequestHandler<RegisterCommand, Result>
{
    public async Task<Result> Handle(
        RegisterCommand command, CancellationToken ct)
    {
        var registerRequest = command.Request;

        var isEmailExist = await userManager.Users.AnyAsync(u => u.Email == registerRequest.Email, ct);

        if (isEmailExist)
            return Result.Failure(UserErrors.DuplicatedEmail);

        var username = registerRequest.UserName.Trim().ToUpperInvariant();

        var isUserNameExist =
            await userManager.Users.AnyAsync(u => u.NormalizedUserName == username, ct);

        if (isUserNameExist)
            return Result.Failure(UserErrors.DuplicatedUserName);

        var user = registerRequest.Adapt<ApplicationUser>();
        user.ImageUrl = AuthHelpers.DefaultUserImagePath();
        var result = await userManager.CreateAsync(user, registerRequest.Password);
        
        if (result.Succeeded)
        {
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            logger.LogInformation("User with {UserName} & code {Code} Has been created.", user.UserName, code);

            var origin = command.Origin ?? "https://neuralearning.netlify.app";

            BackgroundJob.Enqueue<AuthJobs>(jobs => jobs.SendConfirmationEmail(user.Email!, user.FirstName, user.Id, code));

            return Result.Success();
        }

        foreach (var er in result.Errors)
            logger.LogWarning("Error {er}", er);

        var error = result.Errors.FirstOrDefault();

        return Result.Failure(new Error(error!.Code, error.Description, StatusCodes.Status400BadRequest));
    }
}

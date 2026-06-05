using MediatR;
using Microsoft.AspNetCore.Identity;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Files;
using Neura.Core.Entities;
using Neura.Core.Errors;
using Neura.Core.FilesConsts;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Auth.UpdateImage;

internal sealed class UpdateImageHandler(
    UserManager<ApplicationUser> userManager,
    IFileService fileService,
    IServiceHelpers helpers) 
    : IRequestHandler<UpdateImageCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        UpdateImageCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return Result.Failure<string>(UserErrors.UserNotFound);

        if (!string.IsNullOrEmpty(user.ImageUrl) && user.ImageUrl != AuthHelpers.DefaultUserImagePath())
            fileService.Delete(user.ImageUrl);

        user.ImageUrl = await fileService.UploadImageAsync(
            command.Request.Image,
            ImageConsts.User,
            ct);

        await userManager.UpdateAsync(user);
        string url = helpers.GetBaseUrl();
        return Result.Success($"{url}/{user.ImageUrl}");
    }
}

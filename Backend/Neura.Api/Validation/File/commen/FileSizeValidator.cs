using Neura.Core.Settings;

namespace Neura.Api.Validation.File.commen;

public class FileSizeValidator : AbstractValidator<IFormFile>
{
    public FileSizeValidator()
    {
        RuleFor(r => r)
            .Must((request, context) => request.Length <= FileSettings.MaxFileSizeInBytes)
            .WithMessage($"Size of File must be less than or equal to {FileSettings.MaxFileSizeInMB} MB")
            .When(r => r is not null);
    }
}
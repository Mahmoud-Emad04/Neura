using System.Text.RegularExpressions;

namespace Neura.Api.Validation.File.commen;

public class FileNameValidator : AbstractValidator<IFormFile>
{
    public FileNameValidator()
    {
        RuleFor(r => r.FileName)
            .Matches(@"^[\w\-. ]+\.(jpg|jpeg|png|gif|webp)$", RegexOptions.IgnoreCase)
            .WithMessage("Invalid file name")
            .When(r => r is not null);
    }
}
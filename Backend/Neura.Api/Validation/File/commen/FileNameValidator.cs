namespace Neura.Api.Validation.File.commen;

public class FileNameValidator : AbstractValidator<IFormFile>
{
    public FileNameValidator()
    {
        RuleFor(r => r.FileName)
            .Matches("^[A-Za-z0-9_\\-.]*$")
            .WithMessage("Invalid file name")
            .When(r => r is not null);
    }
}

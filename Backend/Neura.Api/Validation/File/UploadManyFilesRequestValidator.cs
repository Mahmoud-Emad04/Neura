using Neura.Api.Validation.File.commen;
using Neura.Core.Contracts.Files;

namespace Neura.Api.Validation.File;

public class UploadManyFilesRequestValidator : AbstractValidator<UploadManyFilesRequest>
{
    public UploadManyFilesRequestValidator()
    {
        RuleFor(r => r.Files)
            .NotNull()
            .Must(f => f.Count > 0)
            .WithMessage("At least one file is required.");

        RuleForEach(r => r.Files)
            .SetValidator(new FileSizeValidator())
            .SetValidator(new BlockedSignaturesValidator())
            .SetValidator(new FileNameValidator())
            .When(r => r.Files is not null);
    }
}

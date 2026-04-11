using Neura.Api.Validation.File.commen;
using Neura.Core.Contracts.Files;

namespace Neura.Api.Validation.File;

public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    public UploadFileRequestValidator()
    {
        RuleFor(r => r.File)
            .NotNull()
            .SetValidator(new FileSizeValidator())
            .SetValidator(new BlockedSignaturesValidator())
            .SetValidator(new FileNameValidator())
            .When(r => r.File is not null);
    }
}
using Neura.Core.Contracts.Webhook;

namespace Neura.Api.Validation.Webhook;

public class CheatingAlertRequestValidator : AbstractValidator<CheatingAlertRequest>
{
    private static readonly string[] AllowedSeverities = ["Low", "Medium", "High", "Critical"];
    public CheatingAlertRequestValidator()
    {
        RuleFor(x => x.ExamId)
            .NotEmpty();

        RuleFor(x => x.StudentId)
            .NotEmpty();

        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(s => AllowedSeverities.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Severity must be one of: {string.Join(", ", AllowedSeverities)}");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Timestamp)
            .GreaterThan(0);
    }
}

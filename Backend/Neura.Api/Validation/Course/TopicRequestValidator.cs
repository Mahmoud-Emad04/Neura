namespace Neura.Api.Validation.Course;

public class TopicRequestValidator : AbstractValidator<TopicRequest>
{
    public TopicRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(3, 100);
    }
}
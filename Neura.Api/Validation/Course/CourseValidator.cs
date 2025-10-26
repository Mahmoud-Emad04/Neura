using Neura.Core.Contracts.Course;

namespace Neura.Api.Validation.Course;

public class CourseValidator : AbstractValidator<CourseRequest>
{
    public CourseValidator()
    {
        RuleFor(c => c.Title).NotEmpty().Length(3, 100);

        RuleFor(c => c.Description).NotEmpty().Length(3, 1000);

        RuleFor(c => c.Topics)
            .Must((course, context) => IsValidSequence(course))
            .WithMessage((course) => $"Topic positions must be unique and sequential starting from 1 and ending at {course.Topics!.Count}.")
            .When(c => c.Topics is not null);
        
        RuleFor(c => c.Topics)
            .Must((course, context) => IsValidNameSequence(course))
            .WithMessage("Topic names must be unique")
            .When(c => c.Topics is not null);

        // RuleForEach(c => c.Topics)
        //     .SetValidator((course, context) => new TopicRequestValidator(course.Topics!.Count))
        //     .WithMessage((course) => $"Topic positions must be unique and sequential starting from 1 and ending at {course.Topics!.Count}.")
        //     .When(c => c.Topics is not null);
    }

    public bool IsValidSequence(CourseRequest course)
    {
        if (course.Topics is not null && course.Topics.Any())
        {
            var positions = course.Topics
                .Select(t => t.Position)
                .OrderBy(p => p)
                .ToList();

            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i] != i + 1)
                    return false;
            }
        }

        return true;
    }

    public bool IsValidNameSequence(CourseRequest course)
    {
        if (course.Topics is not null && course.Topics.Any())
        {
            var positions = course.Topics
                .Select(t => t.Name)
                .OrderBy(p => p)
                .ToList();

            for (int i = 1; i < positions.Count; i++)
            {
                if (positions[i] == positions[i-1])
                    return false;
            }
        }

        return true;
    }
}
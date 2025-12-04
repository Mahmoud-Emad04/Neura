using Neura.Api.Validation.File.commen;
using Neura.Core.Contracts.Course;
using Neura.Core.Settings;

namespace Neura.Api.Validation.Course;

public class CourseValidator : AbstractValidator<CourseRequest>
{
    public CourseValidator()
    {
        RuleFor(c => c.Title).NotEmpty().Length(3, 100);

        RuleFor(c => c.Description).NotEmpty().Length(3, 1000);

        RuleFor(c => c.Startin)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));

        RuleFor(c => c.Endin)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .GreaterThan(c => c.Startin);

        RuleFor(c => c.Tags).NotEmpty();

        RuleFor(c => c.Tags)
            .Must((request, context) => request.Tags.Distinct().Count() == request.Tags.Count())
            .WithMessage("Tags must be unique")
            .When(c => c.Tags is not null);
    }

    //public bool IsValidSequence(CourseRequest course)
    //{
    //    if (course.Topics is not null && course.Topics.Any())
    //    {
    //        var positions = course.Topics
    //            .Select(t => t.Position)
    //            .OrderBy(p => p)
    //            .ToList();

    //        for (int i = 0; i < positions.Count; i++)
    //        {
    //            if (positions[i] != i + 1)
    //                return false;
    //        }
    //    }

    //    return true;
    //}

    //public bool IsValidNameSequence(CourseRequest course)
    //{
    //    if (course.Topics is not null && course.Topics.Any())
    //    {
    //        var positions = course.Topics
    //            .Select(t => t.Name)
    //            .OrderBy(p => p)
    //            .ToList();

    //        for (int i = 1; i < positions.Count; i++)
    //        {
    //            if (positions[i] == positions[i-1])
    //                return false;
    //        }
    //    }

    //    return true;
    //}
}
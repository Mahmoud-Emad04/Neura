using Neura.Api.Validation.File.commen;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Settings;

namespace Neura.Api.Validation.Announcement
{
	public class PostRequestValidator : AbstractValidator<PostRequest>
	{
		public PostRequestValidator()
		{
			RuleFor(p => p.Title)
				.NotEmpty()
				.WithMessage("Title is required")
				.MaximumLength(200)
				.WithMessage("Title cannot exceed 200 characters");

			RuleFor(p => p.Content)
				.NotEmpty()
				.WithMessage("Content is required");

			RuleFor(p => p.IsPublic)
				.NotNull()
				.WithMessage("IsPublic is required");

			RuleFor(p => p.CourseId)
				.GreaterThan(0)
				.WithMessage("CourseId must be greater than 0")
				.When(p => p.CourseId.HasValue);

			RuleFor(p => p.SectionId)
				.GreaterThan(0)
				.WithMessage("SectionId must be greater than 0")
				.When(p => p.SectionId.HasValue);

			RuleFor(p => p.Image)
				.SetValidator(new FileSizeValidator())
				.SetValidator(new BlockedSignaturesValidator())
				.SetValidator(new FileNameValidator())
				.Must(request =>
				{
					if (request is null)
						return true;

					var extension = Path.GetExtension(request.FileName.ToLower());
					return FileSettings.AllowedImagesExtensions.Contains(extension);
				})
				.WithMessage("Invalid image extension")
				.When(p => p.Image is not null);
		}
	}
}


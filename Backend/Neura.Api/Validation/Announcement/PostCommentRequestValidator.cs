
using Neura.Api.Validation.File.commen;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Settings;

namespace Neura.Api.Validation.Announcement
{
	public class PostCommentRequestValidator : AbstractValidator<PostCommentRequest>
	{
		public PostCommentRequestValidator()
		{
			RuleFor(c => c.Content)
				.NotEmpty()
				.WithMessage("Comment content is required")
				.MinimumLength(1)
				.WithMessage("Comment must contain at least one character")
				.MaximumLength(5000)
				.WithMessage("Comment cannot exceed 5000 characters");

			RuleFor(c => c.ParentCommentId)
				.GreaterThan(0)
				.WithMessage("ParentCommentId must be greater than 0")
				.When(c => c.ParentCommentId.HasValue);

			RuleFor(c => c.Image)
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
				.When(c => c.Image is not null);
		}
	}
}


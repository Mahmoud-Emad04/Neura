using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class AnnouncementErrors
{
    public static readonly Error PostNotFound =
        new("Post.NotFound", "The specified post was not found.", StatusCodes.Status404NotFound);

    public static readonly Error PostInvalidData =
        new("Post.InvalidData", "One or more post fields are invalid.", StatusCodes.Status400BadRequest);

    public static readonly Error PostAccessDenied =
        new("Post.AccessDenied", "You do not have permission to perform this action on this post.",
            StatusCodes.Status403Forbidden);

    public static readonly Error CommentNotFound =
        new("Comment.NotFound", "The specified comment was not found.", StatusCodes.Status404NotFound);

    public static readonly Error CommentInvalidData =
        new("Comment.InvalidData", "One or more comment fields are invalid.", StatusCodes.Status400BadRequest);

    public static readonly Error CommentAccessDenied =
        new("Comment.AccessDenied", "You do not have permission to perform this action on this comment.",
            StatusCodes.Status403Forbidden);

    public static readonly Error ParentCommentNotFound =
        new("ParentComment.NotFound", "The specified parent comment was not found.", StatusCodes.Status404NotFound);

    public static readonly Error CourseNotFound =
        new("Course.NotFound", "The specified course was not found.", StatusCodes.Status404NotFound);

    public static readonly Error SectionNotFound =
        new("Section.NotFound", "The specified section was not found.", StatusCodes.Status404NotFound);
}
using Neura.Core.Contracts.Announcement;
using Neura.Core.Entities;

namespace Neura.Api.Features.Announcements;

public static class AnnouncementHelpers
{
    public static readonly IReadOnlyDictionary<int, List<PostCommentProjection>> EmptyRepliesLookup
        = new Dictionary<int, List<PostCommentProjection>>();

    public sealed class PostProjection
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public int? CourseId { get; set; }
        public int? SectionId { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string? UpdatedById { get; set; }
        public string? CreatorFirstName { get; set; }
        public string? CreatorLastName { get; set; }
        public string? CreatorImageUrl { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<PostCommentProjection> Comments { get; set; } = new();
    }

    public sealed class PostCommentProjection
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string? UpdatedById { get; set; }
        public string? CreatorFirstName { get; set; }
        public string? CreatorLastName { get; set; }
        public string? CreatorImageUrl { get; set; }
    }

    public static IQueryable<PostProjection> ProjectPosts(IQueryable<Post> posts, string? currentUserId)
    {
        return posts.Select(p => new PostProjection
        {
            Id = p.Id,
            Title = p.Title,
            Content = p.Content,
            IsPublic = p.IsPublic,
            CourseId = p.CourseId,
            SectionId = p.SectionId,
            ImageUrl = p.ImageUrl,
            CreatedOn = p.CreatedOn,
            UpdatedOn = p.UpdatedOn,
            CreatedById = p.CreatedById,
            UpdatedById = p.UpdatedById,
            CreatorFirstName = p.CreatedBy != null ? p.CreatedBy.FirstName : null,
            CreatorLastName = p.CreatedBy != null ? p.CreatedBy.LastName : null,
            CreatorImageUrl = p.CreatedBy != null ? p.CreatedBy.ImageUrl : null,
            LikesCount = p.Likes.Count,
            CommentsCount = p.Comments.Count(c => !c.IsDeleted),
            IsLikedByCurrentUser = currentUserId != null
                                   && p.Likes.Any(l => l.UserId == currentUserId),
            Comments = p.Comments
                .Where(c => !c.IsDeleted)
                .Select(c => new PostCommentProjection
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    ParentCommentId = c.ParentCommentId,
                    Content = c.Content,
                    ImageUrl = c.ImageUrl,
                    CreatedOn = c.CreatedOn,
                    UpdatedOn = c.UpdatedOn,
                    CreatedById = c.CreatedById,
                    UpdatedById = c.UpdatedById,
                    CreatorFirstName = c.CreatedBy != null ? c.CreatedBy.FirstName : null,
                    CreatorLastName = c.CreatedBy != null ? c.CreatedBy.LastName : null,
                    CreatorImageUrl = c.CreatedBy != null ? c.CreatedBy.ImageUrl : null
                })
                .ToList()
        });
    }

    public static IQueryable<PostCommentProjection> ProjectComments(IQueryable<PostComment> comments)
    {
        return comments.Select(c => new PostCommentProjection
        {
            Id = c.Id,
            PostId = c.PostId,
            ParentCommentId = c.ParentCommentId,
            Content = c.Content,
            ImageUrl = c.ImageUrl,
            CreatedOn = c.CreatedOn,
            UpdatedOn = c.UpdatedOn,
            CreatedById = c.CreatedById,
            UpdatedById = c.UpdatedById,
            CreatorFirstName = c.CreatedBy != null ? c.CreatedBy.FirstName : null,
            CreatorLastName = c.CreatedBy != null ? c.CreatedBy.LastName : null,
            CreatorImageUrl = c.CreatedBy != null ? c.CreatedBy.ImageUrl : null
        });
    }

    public static PostResponse MapProjectionToResponse(PostProjection p, string baseUrl, string? currentUserId)
    {
        var repliesLookup = BuildRepliesLookup(p.Comments);
        var isCreatedByCurrentUser = !string.IsNullOrEmpty(currentUserId) && p.CreatedById == currentUserId;

        var rootComments = p.Comments
            .Where(c => c.ParentCommentId == null)
            .OrderBy(c => c.CreatedOn)
            .Select(c => MapCommentProjectionToResponse(c, repliesLookup, baseUrl, currentUserId))
            .ToList();

        return new PostResponse(
            p.Id,
            p.Title,
            p.Content,
            p.IsPublic,
            p.CourseId,
            p.SectionId,
            BuildImageUrl(p.ImageUrl, baseUrl),
            p.LikesCount,
            p.CommentsCount,
            p.CreatedOn,
            p.UpdatedOn,
            p.CreatedById,
            BuildFullName(p.CreatorFirstName, p.CreatorLastName),
            BuildImageUrl(p.CreatorImageUrl, baseUrl),
            isCreatedByCurrentUser,
            p.UpdatedById,
            p.IsLikedByCurrentUser,
            rootComments);
    }

    public static PostCommentResponse MapCommentProjectionToResponse(
        PostCommentProjection root,
        IReadOnlyDictionary<int, List<PostCommentProjection>> repliesLookup,
        string baseUrl,
        string? currentUserId)
    {
        var mapped = new Dictionary<int, PostCommentResponse>();
        var stack = new Stack<(PostCommentProjection node, bool processed)>();
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (node, processed) = stack.Pop();

            if (!processed)
            {
                stack.Push((node, true));
                if (repliesLookup.TryGetValue(node.Id, out var children))
                {
                    for (var i = children.Count - 1; i >= 0; i--)
                        stack.Push((children[i], false));
                }
            }
            else
            {
                List<PostCommentResponse> mappedChildren;
                if (repliesLookup.TryGetValue(node.Id, out var children))
                {
                    mappedChildren = new List<PostCommentResponse>(children.Count);
                    foreach (var c in children)
                        mappedChildren.Add(mapped[c.Id]);
                }
                else
                {
                    mappedChildren = new List<PostCommentResponse>(0);
                }
                var isCreatedByCurrentUser = !string.IsNullOrEmpty(currentUserId) && node.CreatedById == currentUserId;

                mapped[node.Id] = new PostCommentResponse(
                    node.Id,
                    node.PostId,
                    node.ParentCommentId,
                    node.Content,
                    node.ImageUrl,
                    node.CreatedOn,
                    node.UpdatedOn,
                    node.CreatedById,
                    BuildFullName(node.CreatorFirstName, node.CreatorLastName),
                    BuildImageUrl(node.CreatorImageUrl, baseUrl),
                    isCreatedByCurrentUser,
                    node.UpdatedById,
                    mappedChildren);
            }
        }

        return mapped[root.Id];
    }

    public static Dictionary<int, List<PostCommentProjection>> BuildRepliesLookup(
        IEnumerable<PostCommentProjection> comments)
    {
        return comments
            .Where(c => c.ParentCommentId.HasValue)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(c => c.CreatedOn).ToList());
    }

    private static string BuildFullName(string? firstName, string? lastName)
    {
        var hasFirst = !string.IsNullOrWhiteSpace(firstName);
        var hasLast = !string.IsNullOrWhiteSpace(lastName);

        if (hasFirst && hasLast) return $"{firstName} {lastName}";
        if (hasFirst) return firstName!;
        if (hasLast) return lastName!;
        return string.Empty;
    }

    private static string? BuildImageUrl(string? relativePath, string baseUrl) =>
        string.IsNullOrEmpty(relativePath) ? null : $"{baseUrl}/{relativePath}";
}

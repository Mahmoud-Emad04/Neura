namespace Neura.Core.Entities;

public class PostLike
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }

    public Post Post { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
}
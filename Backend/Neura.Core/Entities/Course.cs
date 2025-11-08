namespace Neura.Core.Entities;

public sealed class Course : AuditableEntity
{
    public int Id { get; set; }
   // public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    //public string ImageUrl { get; set; } = string.Empty;
    //TODO Image URL
    //TODO review ICollection or LIST
    public ICollection<Topic> Topics { get; set; } = [];
}
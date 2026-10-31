namespace SpendWise.Data.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    /// <summary>Owner of a personal category; null means the category is global.</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsGlobal => UserId is null;
}

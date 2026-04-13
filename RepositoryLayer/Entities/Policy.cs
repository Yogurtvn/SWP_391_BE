namespace RepositoryLayer.Entities;

public class Policy
{
    public int PolicyId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }
}

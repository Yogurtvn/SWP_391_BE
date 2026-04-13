namespace RepositoryLayer.Entities;

public class ReturnStatus
{
    public int ReturnStatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public ICollection<Return> Returns { get; set; } = [];
}

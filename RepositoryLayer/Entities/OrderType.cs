namespace RepositoryLayer.Entities;

public class OrderType
{
    public int OrderTypeId { get; set; }

    public string OrderTypeName { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = [];
}

namespace RepositoryLayer.Entities;

public class ShippingStatus
{
    public int ShippingStatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = [];
}

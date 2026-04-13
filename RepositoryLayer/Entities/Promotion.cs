namespace RepositoryLayer.Entities;

public class Promotion
{
    public int PromotionId { get; set; }

    public string? PromoCode { get; set; }

    public string? PromotionName { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}

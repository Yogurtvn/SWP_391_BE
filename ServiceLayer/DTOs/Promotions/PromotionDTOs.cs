using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Promotions;

public class PromotionResponse
{
    public int PromotionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal DiscountPercent { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class CreatePromotionRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, 100)]
    public decimal DiscountPercent { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdatePromotionRequest
{
    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, 100)]
    public decimal? DiscountPercent { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public bool? IsActive { get; set; }
}

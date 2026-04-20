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

public class CreatePromotionRequest : IValidatableObject
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "100")]
    public decimal DiscountPercent { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt == default)
        {
            yield return new ValidationResult("StartAt is required.", [nameof(StartAt)]);
        }

        if (EndAt == default)
        {
            yield return new ValidationResult("EndAt is required.", [nameof(EndAt)]);
        }

        if (StartAt != default && EndAt != default && EndAt <= StartAt)
        {
            yield return new ValidationResult("EndAt must be after StartAt.", [nameof(EndAt)]);
        }
    }
}

public class UpdatePromotionRequest : IValidatableObject
{
    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "100")]
    public decimal? DiscountPercent { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public bool? IsActive { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt.HasValue && StartAt.Value == default)
        {
            yield return new ValidationResult("StartAt is required.", [nameof(StartAt)]);
        }

        if (EndAt.HasValue && EndAt.Value == default)
        {
            yield return new ValidationResult("EndAt is required.", [nameof(EndAt)]);
        }

        if (StartAt.HasValue && EndAt.HasValue && EndAt.Value <= StartAt.Value)
        {
            yield return new ValidationResult("EndAt must be after StartAt.", [nameof(EndAt)]);
        }
    }
}

public class UpdatePromotionStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}

public class AssignPromotionVariantsRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    public List<int> VariantIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VariantIds is null)
        {
            yield break;
        }

        if (VariantIds.Count == 0)
        {
            yield break;
        }

        if (VariantIds.Any(variantId => variantId <= 0))
        {
            yield return new ValidationResult("Each variantId must be greater than 0.", [nameof(VariantIds)]);
        }
    }
}

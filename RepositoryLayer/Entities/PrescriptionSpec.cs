using RepositoryLayer.Enums;

namespace RepositoryLayer.Entities;

public class PrescriptionSpec
{
    public int PrescriptionId { get; set; }

    public int UserId { get; set; }

    public decimal? SphLeft { get; set; }

    public decimal? SphRight { get; set; }

    public decimal? CylLeft { get; set; }

    public decimal? CylRight { get; set; }

    public int? AxisLeft { get; set; }

    public int? AxisRight { get; set; }

    public decimal? Pd { get; set; }

    public string? PrescriptionImage { get; set; }

    public int? StaffId { get; set; }

    public PrescriptionStatus PrescriptionStatus { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public User? Staff { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}

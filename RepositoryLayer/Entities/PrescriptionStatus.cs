namespace RepositoryLayer.Entities;

public class PrescriptionStatus
{
    public int PrescriptionStatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public ICollection<PrescriptionSpec> PrescriptionSpecs { get; set; } = [];
}

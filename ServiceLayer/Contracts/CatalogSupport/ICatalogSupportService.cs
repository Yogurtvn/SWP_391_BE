using ServiceLayer.DTOs.CatalogSupport.Request;
using ServiceLayer.DTOs.CatalogSupport.Response;

namespace ServiceLayer.Contracts.CatalogSupport;

public interface ICatalogSupportService
{
    Task<PrescriptionEligibilityResponse?> GetPrescriptionEligibilityAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<VariantAvailabilityResponse?> GetVariantAvailabilityAsync(
        int variantId,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOptionsResponse> GetPrescriptionOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<PrescriptionPricingResponse> CalculatePrescriptionPricingAsync(
        CalculatePrescriptionPricingRequest request,
        CancellationToken cancellationToken = default);
}

using RepositoryLayer.Common;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Prescription.Request;
using ServiceLayer.DTOs.Prescription.Response;

namespace ServiceLayer.Contracts.Prescription;

public interface IPrescriptionService
{
    Task<PagedResult<PrescriptionListItemResponse>> GetPrescriptionsAsync(
        GetPrescriptionsRequest request,
        CancellationToken cancellationToken = default);

    Task<PrescriptionDetailResponse?> GetPrescriptionByIdAsync(
        int prescriptionId,
        CancellationToken cancellationToken = default);

    Task<PrescriptionStatusResponse> ReviewPrescriptionAsync(
        int staffUserId,
        int prescriptionId,
        ReviewPrescriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<PrescriptionStatusResponse> RequestMoreInfoAsync(
        int staffUserId,
        int prescriptionId,
        RequestMorePrescriptionInfoRequest request,
        CancellationToken cancellationToken = default);
}

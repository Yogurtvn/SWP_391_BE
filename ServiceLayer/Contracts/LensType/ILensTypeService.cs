using RepositoryLayer.Common;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.LensType.Request;
using ServiceLayer.DTOs.LensType.Response;

namespace ServiceLayer.Contracts.LensType;

public interface ILensTypeService
{
    Task<PagedResult<LensTypeListItemResponse>> GetLensTypesAsync(
        GetLensTypesRequest request,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<LensTypeDetailResponse?> GetLensTypeByIdAsync(
        int lensTypeId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<LensTypeIdResponse> CreateLensTypeAsync(
        CreateLensTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> UpdateLensTypeAsync(
        int lensTypeId,
        UpdateLensTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> UpdateLensTypeStatusAsync(
        int lensTypeId,
        UpdateLensTypeStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> DeleteLensTypeAsync(int lensTypeId, CancellationToken cancellationToken = default);
}

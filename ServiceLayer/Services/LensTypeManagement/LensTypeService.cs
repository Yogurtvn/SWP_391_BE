using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.LensType;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.LensType.Request;
using ServiceLayer.DTOs.LensType.Response;
using ServiceLayer.Exceptions;
using System.Net;

namespace ServiceLayer.Services.LensTypeManagement;

public class LensTypeService(IUnitOfWork unitOfWork) : ILensTypeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PagedResult<LensTypeListItemResponse>> GetLensTypesAsync(
        GetLensTypesRequest request,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = NormalizeText(request.Search);
        var (sortBy, sortDescending) = NormalizeSort(request.SortBy, request.SortOrder);
        var effectiveIsActive = includeInactive ? request.IsActive : true;
        var repository = _unitOfWork.Repository<LensType>();

        var pagedResult = await repository.GetPagedAsync(
            paginationRequest: new PaginationRequest(request.Page, request.PageSize),
            filter: lensType =>
                (!effectiveIsActive.HasValue || lensType.IsActive == effectiveIsActive.Value) &&
                (normalizedSearch == null
                    || lensType.LensCode.Contains(normalizedSearch)
                    || lensType.LensName.Contains(normalizedSearch)
                    || (lensType.Description != null && lensType.Description.Contains(normalizedSearch))),
            orderBy: query => ApplyOrdering(query, sortBy, sortDescending),
            tracked: false,
            cancellationToken: cancellationToken);

        return PagedResult<LensTypeListItemResponse>.Create(
            pagedResult.Items.Select(MapToListItem).ToList(),
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.TotalItems);
    }

    public async Task<LensTypeDetailResponse?> GetLensTypeByIdAsync(
        int lensTypeId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<LensType>();
        var lensType = await repository.GetFirstOrDefaultAsync(
            entity => entity.LensTypeId == lensTypeId && (includeInactive || entity.IsActive),
            tracked: false);

        return lensType is null ? null : MapToDetail(lensType);
    }

    public async Task<LensTypeIdResponse> CreateLensTypeAsync(
        CreateLensTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureLensCodeUniqueAsync(request.LensCode);

        var repository = _unitOfWork.Repository<LensType>();
        var lensType = new LensType
        {
            LensCode = request.LensCode.Trim(),
            LensName = request.LensName.Trim(),
            Description = NormalizeText(request.Description),
            Price = request.Price,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(lensType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LensTypeIdResponse
        {
            LensTypeId = lensType.LensTypeId
        };
    }

    public async Task<MessageResponse> UpdateLensTypeAsync(
        int lensTypeId,
        UpdateLensTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<LensType>();
        var lensType = await repository.GetByIdAsync(lensTypeId);

        if (lensType is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "LENS_TYPE_NOT_FOUND", "Lens type not found");
        }

        await EnsureLensCodeUniqueAsync(request.LensCode, lensTypeId);

        lensType.LensCode = request.LensCode.Trim();
        lensType.LensName = request.LensName.Trim();
        lensType.Description = NormalizeText(request.Description);
        lensType.Price = request.Price;
        lensType.IsActive = request.IsActive;

        repository.Update(lensType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Lens type updated"
        };
    }

    public async Task<MessageResponse> UpdateLensTypeStatusAsync(
        int lensTypeId,
        UpdateLensTypeStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsActive.HasValue)
        {
            throw CreateValidationException("isActive", "isActive is required");
        }

        var repository = _unitOfWork.Repository<LensType>();
        var lensType = await repository.GetByIdAsync(lensTypeId);

        if (lensType is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "LENS_TYPE_NOT_FOUND", "Lens type not found");
        }

        lensType.IsActive = request.IsActive.Value;
        repository.Update(lensType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Lens type status updated"
        };
    }

    public async Task<MessageResponse> DeleteLensTypeAsync(int lensTypeId, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<LensType>();
        var prescriptionRepository = _unitOfWork.Repository<PrescriptionSpec>();
        var cartPrescriptionRepository = _unitOfWork.Repository<CartPrescriptionDetail>();
        var orderItemRepository = _unitOfWork.Repository<OrderItem>();
        var lensType = await repository.GetByIdAsync(lensTypeId);

        if (lensType is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "LENS_TYPE_NOT_FOUND", "Lens type not found");
        }

        if (await prescriptionRepository.ExistsAsync(item => item.LensTypeId == lensTypeId)
            || await cartPrescriptionRepository.ExistsAsync(item => item.LensTypeId == lensTypeId)
            || await orderItemRepository.ExistsAsync(item => item.LensTypeId == lensTypeId))
        {
            throw new ApiException(
                (int)HttpStatusCode.Conflict,
                "LENS_TYPE_DELETE_NOT_ALLOWED",
                "Lens type cannot be deleted because it is already in use");
        }

        // TODO: revisit lens type delete behavior once API_SPEC.md finalizes whether delete should be hard-delete or soft-delete.
        repository.Remove(lensType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Lens type deleted"
        };
    }

    private async Task EnsureLensCodeUniqueAsync(string lensCode, int? excludedLensTypeId = null)
    {
        var normalizedLensCode = lensCode.Trim();
        var repository = _unitOfWork.Repository<LensType>();
        var exists = await repository.ExistsAsync(
            lensType => lensType.LensCode == normalizedLensCode
                && (!excludedLensTypeId.HasValue || lensType.LensTypeId != excludedLensTypeId.Value));

        if (exists)
        {
            throw new ApiException((int)HttpStatusCode.Conflict, "LENS_CODE_ALREADY_EXISTS", "Lens code already exists");
        }
    }

    private static LensTypeListItemResponse MapToListItem(LensType lensType)
    {
        return new LensTypeListItemResponse
        {
            LensTypeId = lensType.LensTypeId,
            LensCode = lensType.LensCode,
            LensName = lensType.LensName,
            Price = lensType.Price,
            IsActive = lensType.IsActive
        };
    }

    private static LensTypeDetailResponse MapToDetail(LensType lensType)
    {
        return new LensTypeDetailResponse
        {
            LensTypeId = lensType.LensTypeId,
            LensCode = lensType.LensCode,
            LensName = lensType.LensName,
            Description = lensType.Description,
            Price = lensType.Price,
            IsActive = lensType.IsActive
        };
    }

    private static string? NormalizeText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static (string SortBy, bool SortDescending) NormalizeSort(string? sortBy, string? sortOrder)
    {
        var normalizedSortBy = NormalizeText(sortBy)?.ToLowerInvariant() ?? "lenstypeid";
        var normalizedSortOrder = NormalizeText(sortOrder)?.ToLowerInvariant();

        if (normalizedSortBy is not ("lenstypeid" or "lenscode" or "lensname" or "price"))
        {
            throw CreateInvalidQueryException("sortBy", "sortBy is invalid");
        }

        return normalizedSortOrder switch
        {
            null => (normalizedSortBy, false),
            "asc" => (normalizedSortBy, false),
            "desc" => (normalizedSortBy, true),
            _ => throw CreateInvalidQueryException("sortOrder", "sortOrder must be 'asc' or 'desc'")
        };
    }

    private static IOrderedQueryable<LensType> ApplyOrdering(
        IQueryable<LensType> query,
        string sortBy,
        bool sortDescending)
    {
        return sortBy switch
        {
            "lenscode" when sortDescending => query.OrderByDescending(lensType => lensType.LensCode).ThenByDescending(lensType => lensType.LensTypeId),
            "lenscode" => query.OrderBy(lensType => lensType.LensCode).ThenBy(lensType => lensType.LensTypeId),
            "lensname" when sortDescending => query.OrderByDescending(lensType => lensType.LensName).ThenByDescending(lensType => lensType.LensTypeId),
            "lensname" => query.OrderBy(lensType => lensType.LensName).ThenBy(lensType => lensType.LensTypeId),
            "price" when sortDescending => query.OrderByDescending(lensType => lensType.Price).ThenByDescending(lensType => lensType.LensTypeId),
            "price" => query.OrderBy(lensType => lensType.Price).ThenBy(lensType => lensType.LensTypeId),
            _ when sortDescending => query.OrderByDescending(lensType => lensType.LensTypeId),
            _ => query.OrderBy(lensType => lensType.LensTypeId)
        };
    }

    private static ApiException CreateValidationException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "VALIDATION_ERROR",
            "Invalid lens type data",
            new { field, issue });
    }

    private static ApiException CreateInvalidQueryException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "INVALID_QUERY",
            "Invalid query parameters",
            new { field, issue });
    }
}

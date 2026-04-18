using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.CatalogSupport;
using ServiceLayer.DTOs.CatalogSupport.Request;
using ServiceLayer.DTOs.CatalogSupport.Response;
using ServiceLayer.Exceptions;
using System.Net;

namespace ServiceLayer.Services.CatalogSupport;

public class CatalogSupportService(IUnitOfWork unitOfWork) : ICatalogSupportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PrescriptionEligibilityResponse?> GetPrescriptionEligibilityAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Product>();
        var product = await repository.GetByIdAsync(productId);

        if (product is null)
        {
            return null;
        }

        var isEligible = product.IsActive
            && product.ProductType == ProductType.Frame
            && product.PrescriptionCompatible;

        return new PrescriptionEligibilityResponse
        {
            ProductId = product.ProductId,
            IsEligible = isEligible,
            Reason = isEligible
                ? null
                : product.ProductType != ProductType.Frame
                    ? "Only frame products are eligible for prescription flow"
                    : product.PrescriptionCompatible
                        ? "Product is inactive"
                        : "Product is not prescription compatible"
        };
    }

    public async Task<VariantAvailabilityResponse?> GetVariantAvailabilityAsync(
        int variantId,
        CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<ProductVariant>();
        var variant = await repository.GetFirstOrDefaultAsync(
            item => item.VariantId == variantId && item.Product.IsActive && item.IsActive,
            includeProperties: "Inventory,Product",
            tracked: false);

        if (variant is null)
        {
            return null;
        }

        var quantity = variant.Inventory?.Quantity ?? 0;

        return new VariantAvailabilityResponse
        {
            VariantId = variant.VariantId,
            Quantity = quantity,
            IsReadyAvailable = quantity > 0,
            IsPreOrderAllowed = variant.Inventory?.IsPreOrderAllowed ?? false,
            ExpectedRestockDate = variant.Inventory?.ExpectedRestockDate
        };
    }

    public async Task<PrescriptionPricingResponse> CalculatePrescriptionPricingAsync(
        CalculatePrescriptionPricingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var variantId = request.VariantId ?? throw CreatePricingException("variantId", "variantId is required");
        var lensTypeId = request.LensTypeId ?? throw CreatePricingException("lensTypeId", "lensTypeId is required");
        var quantity = request.Quantity ?? throw CreatePricingException("quantity", "quantity is required");

        if (variantId <= 0)
        {
            throw CreatePricingException("variantId", "variantId must be greater than 0");
        }

        if (lensTypeId <= 0)
        {
            throw CreatePricingException("lensTypeId", "lensTypeId must be greater than 0");
        }

        if (quantity <= 0)
        {
            throw CreatePricingException("quantity", "quantity must be greater than 0");
        }

        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var lensTypeRepository = _unitOfWork.Repository<LensType>();

        var variant = await variantRepository.GetFirstOrDefaultAsync(
            item => item.VariantId == variantId
                && item.IsActive
                && item.Product.IsActive
                && item.Product.ProductType == ProductType.Frame
                && item.Product.PrescriptionCompatible,
            includeProperties: "Product",
            tracked: false);

        if (variant is null)
        {
            throw CreatePricingException("variantId", "variantId must reference a prescription-compatible frame variant");
        }

        var lensType = await lensTypeRepository.GetFirstOrDefaultAsync(
            item => item.LensTypeId == lensTypeId && item.IsActive,
            tracked: false);

        if (lensType is null)
        {
            throw CreatePricingException("lensTypeId", "lensTypeId must reference an existing active lens type");
        }

        // TODO: plug in coating pricing source once API_SPEC.md defines it. Coating price is kept at 0 for now.
        var coatingPrice = 0m;
        var framePrice = variant.Price;
        var lensPrice = lensType.Price;

        return new PrescriptionPricingResponse
        {
            FramePrice = framePrice,
            LensPrice = lensPrice,
            CoatingPrice = coatingPrice,
            TotalPrice = (framePrice + lensPrice + coatingPrice) * quantity
        };
    }

    private static ApiException CreatePricingException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "PRICING_CALCULATION_FAILED",
            "Unable to calculate prescription pricing",
            new { field, issue });
    }
}

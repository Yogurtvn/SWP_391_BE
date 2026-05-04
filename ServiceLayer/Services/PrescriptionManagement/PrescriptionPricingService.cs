using ServiceLayer.Contracts.Prescription;
using ServiceLayer.Exceptions;
using System.Net;

namespace ServiceLayer.Services.PrescriptionManagement;

public class PrescriptionPricingService : IPrescriptionPricingService
{
    public PrescriptionPriceCalculation Calculate(
        decimal framePrice,
        decimal lensBasePrice,
        int quantity,
        string errorCode,
        string errorMessage)
    {
        if (quantity <= 0)
        {
            throw CreateApiException(errorCode, errorMessage, "quantity", "quantity must be greater than 0");
        }

        var lensPrice = lensBasePrice;

        return new PrescriptionPriceCalculation
        {
            FramePrice = framePrice,
            LensBasePrice = lensBasePrice,
            LensPrice = lensPrice,
            TotalPrice = (framePrice + lensPrice) * quantity
        };
    }

    private static ApiException CreateApiException(
        string errorCode,
        string errorMessage,
        string field,
        string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            errorCode,
            errorMessage,
            new { field, issue });
    }
}

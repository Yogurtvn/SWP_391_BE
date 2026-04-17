using ServiceLayer.DTOs.Cart.Request;
using ServiceLayer.DTOs.Cart.Response;
using ServiceLayer.DTOs.Common;

namespace ServiceLayer.Contracts.Cart;

public interface ICartService
{
    Task<CartDetailResponse> GetMyCartAsync(int userId, CancellationToken cancellationToken = default);

    Task<MessageResponse> ClearMyCartAsync(int userId, CancellationToken cancellationToken = default);

    Task<StandardCartItemCreatedResponse> AddStandardItemAsync(
        int userId,
        AddStandardCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<StandardCartItemUpdatedResponse> UpdateStandardItemAsync(
        int userId,
        int cartItemId,
        UpdateStandardCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> DeleteStandardItemAsync(
        int userId,
        int cartItemId,
        CancellationToken cancellationToken = default);

    Task<PrescriptionCartItemCreatedResponse> AddPrescriptionItemAsync(
        int userId,
        UpsertPrescriptionCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> UpdatePrescriptionItemAsync(
        int userId,
        int cartItemId,
        UpsertPrescriptionCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> DeletePrescriptionItemAsync(
        int userId,
        int cartItemId,
        CancellationToken cancellationToken = default);
}

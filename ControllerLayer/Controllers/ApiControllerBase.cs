using ControllerLayer.Models;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Exceptions;
using System.Security.Claims;

namespace ControllerLayer.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult ApiError(ApiException exception)
    {
        return StatusCode(
            exception.StatusCode,
            new ApiErrorResponse
            {
                ErrorCode = exception.ErrorCode,
                Message = exception.Message,
                Details = exception.Details
            });
    }

    protected bool TryGetCurrentUserId(out int userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }

    protected bool CanAccessNonPublicCatalogData()
    {
        return User.Identity?.IsAuthenticated == true
            && (User.IsInRole("Admin") || User.IsInRole("Staff"));
    }
}

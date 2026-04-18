using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.Policy;
using ServiceLayer.DTOs.Policy.Request;
using ServiceLayer.DTOs.Policy.Response;

namespace ControllerLayer.Controllers;

[Route("api/policies")]
[ApiController]
public class PoliciesController(IPolicyService policyService) : ControllerBase
{
    private readonly IPolicyService _policyService = policyService;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<PolicyDtoResponse>>> GetPolicies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        var result = await _policyService.GetPoliciesAsync(
            new PaginationRequest(page, pageSize),
            search,
            cancellationToken);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{policyId:int}")]
    public async Task<ActionResult<PolicyDtoResponse>> GetPolicy(int policyId, CancellationToken cancellationToken)
    {
        var result = await _policyService.GetPolicyByIdAsync(policyId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Policy not found." });
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult> CreatePolicy([FromBody] CreatePolicyRequest request, CancellationToken cancellationToken)
    {
        var policyId = await _policyService.CreatePolicyAsync(request, cancellationToken);
        return Ok(new { policyId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{policyId:int}")]
    public async Task<ActionResult> UpdatePolicy(int policyId, [FromBody] UpdatePolicyRequest request, CancellationToken cancellationToken)
    {
        var success = await _policyService.UpdatePolicyAsync(policyId, request, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Policy not found." });
        }

        return Ok(new { message = "Policy updated" });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{policyId:int}")]
    public async Task<ActionResult> DeletePolicy(int policyId, CancellationToken cancellationToken)
    {
        var success = await _policyService.DeletePolicyAsync(policyId, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Policy not found." });
        }

        return Ok(new { message = "Policy deleted" });
    }
}

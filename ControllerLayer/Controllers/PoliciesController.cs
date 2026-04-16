using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Policy;
using ServiceLayer.DTOs.Policy.Request;
using ServiceLayer.DTOs.Policy.Response;

namespace ControllerLayer.Controllers;

[Route("api/policies")]  // Route mặc định: /api/policies
[ApiController]               // Tự động validate model state và trả 400 nếu invalid
public class PoliciesController(IPolicyService policyService) : ControllerBase
{
    private readonly IPolicyService _policyService = policyService; // Inject service qua primary constructor

    // GET /api/Policies?page=1&pageSize=20&search=keyword
    // Lấy danh sách policy có phân trang và tìm kiếm
    [AllowAnonymous]  // Ai cũng có thể xem danh sách policy
    [HttpGet]
    public async Task<ActionResult<PolicyListResponse>> GetPolicies(
        [FromQuery] int page = 1,           // Trang hiện tại, mặc định = 1
        [FromQuery] int pageSize = 20,      // Số item mỗi trang, mặc định = 20
        [FromQuery] string? search = null,  // Từ khóa tìm kiếm theo title (optional)
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;             // Đảm bảo page không âm
        if (pageSize < 1) pageSize = 20;    // Đảm bảo pageSize hợp lệ

        var result = await _policyService.GetPoliciesAsync(page, pageSize, search, cancellationToken);
        return Ok(result);                  // Trả về 200 OK với danh sách policy
    }

    // GET /api/Policies/{policyId}
    // Lấy chi tiết 1 policy theo ID
    [AllowAnonymous]  // Ai cũng có thể xem chi tiết policy
    [HttpGet("{policyId:int}")]  // Route constraint: policyId phải là số nguyên
    public async Task<ActionResult<PolicyDtoResponse>> GetPolicy(int policyId, CancellationToken cancellationToken)
    {
        var result = await _policyService.GetPolicyByIdAsync(policyId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Policy not found." });  // 404 nếu không tìm thấy
        }

        return Ok(result);  // 200 OK với chi tiết policy
    }

    // POST /api/Policies
    // Tạo policy mới
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin")])
    [HttpPost]
    public async Task<ActionResult> CreatePolicy([FromBody] CreatePolicyRequest request, CancellationToken cancellationToken)
    {
        var policyId = await _policyService.CreatePolicyAsync(request, cancellationToken);
        return Ok(new { policyId });  // 200 OK trả về ID của policy vừa tạo
    }

    // PUT /api/Policies/{policyId}
    // Cập nhật policy theo ID
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin")])
    [HttpPut("{policyId:int}")]
    public async Task<ActionResult> UpdatePolicy(int policyId, [FromBody] UpdatePolicyRequest request, CancellationToken cancellationToken)
    {
        var success = await _policyService.UpdatePolicyAsync(policyId, request, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Policy not found." });  // 404 nếu không tìm thấy
        }

        return Ok(new { message = "Policy updated" });  // 200 OK khi cập nhật thành công
    }

    // DELETE /api/Policies/{policyId}
    // Xóa policy theo ID
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin")])
    [HttpDelete("{policyId:int}")]
    public async Task<ActionResult> DeletePolicy(int policyId, CancellationToken cancellationToken)
    {
        var success = await _policyService.DeletePolicyAsync(policyId, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Policy not found." });  // 404 nếu không tìm thấy
        }

        return Ok(new { message = "Policy deleted" });  // 200 OK khi xóa thành công
    }
}

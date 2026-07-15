using Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Identity;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/accounts")]
[Authorize(Roles = "Admin")]
public sealed class AccountsAdminController(IIdentityService identity) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) => ApiResponseMapper.ToActionResult(this, await identity.ListUsersAsync(page, pageSize, ct), "Accounts loaded.");
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, AdminUserRequest request, CancellationToken ct) => Map(await identity.AdminUpdateUserAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, request, ct), "Account updated.");
    private IActionResult Map(Core.Common.Result result, string message) => result.IsSuccess ? Ok(new Shared.ApiResponse<object?>(200, message, null)) : StatusCode(ApiResponseMapper.StatusCode(result.Error!), new Shared.ApiResponse<Shared.ApiErrorData>(ApiResponseMapper.StatusCode(result.Error!), result.Error!.Message, new Shared.ApiErrorData(result.Error!.Fields)));
}

[ApiController]
[Route("api/operations/accounts")]
[Authorize(Policy = "Operations")]
public sealed class OperationsAccountsController(IIdentityService identity) : ControllerBase
{
    [HttpPost("{id:guid}/confirm-phone")] public async Task<IActionResult> ConfirmPhone(Guid id, CancellationToken ct) { var result = await identity.ConfirmPhoneAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, ct); return result.IsSuccess ? Ok(new Shared.ApiResponse<object?>(200, "Phone confirmed.", null)) : StatusCode(ApiResponseMapper.StatusCode(result.Error!), new Shared.ApiResponse<Shared.ApiErrorData>(ApiResponseMapper.StatusCode(result.Error!), result.Error!.Message, new Shared.ApiErrorData(result.Error!.Fields))); }
}

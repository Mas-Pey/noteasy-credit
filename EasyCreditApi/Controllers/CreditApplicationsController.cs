using EasyCreditApi.Application.DTO;
using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/credit-applications")]
[Authorize]
public class CreditApplicationsController : ControllerBase
{
    private readonly ICreditApplicationSpService _service;

    public CreditApplicationsController(ICreditApplicationSpService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(ApiResponse<object>.Success(new { total = items.Count, items }));
    }

    [HttpPost("preview-installment")]
    public async Task<IActionResult> PreviewInstallment([FromBody] ReqPreviewInstallmentDto dto)
    {
        var installments = await _service.CalculateInstallmentAsync(dto);
        return Ok(ApiResponse<IReadOnlyList<ResInstallmentDto>>.Success(
            installments,
            "Simulasi cicilan berhasil dihitung."));
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Approve(int id, [FromBody] ReqApproveApplicationDto dto)
    {
        var message = await _service.ApproveAsync(id, dto.Approver);

        return Ok(ApiResponse<object>.Success(
            new { applicationId = id, status = "SUCCESS" },
            message));
    }
}

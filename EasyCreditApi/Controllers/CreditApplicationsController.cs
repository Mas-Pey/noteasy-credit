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
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var (items, total) = await _service.GetPagedApplicationsAsync(status, keyword, page, size);
        var totalPages = size > 0 ? (int)Math.Ceiling((double)total / size) : 1;

        return Ok(ApiResponse<object>.Success(new
        {
            page,
            size,
            total,
            totalPages,
            items
        }, "Daftar pengajuan kredit berhasil diambil."));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var detail = await _service.GetDetailAsync(id);
        return Ok(ApiResponse<ResCreditApplicationDetailDto>.Success(
            detail,
            "Detail pengajuan kredit berhasil diambil."));
    }

    [HttpPost("preview-installment")]
    public async Task<IActionResult> PreviewInstallment([FromBody] ReqPreviewInstallmentDto dto)
    {
        var installments = await _service.CalculateInstallmentAsync(dto);
        return Ok(ApiResponse<IReadOnlyList<ResInstallmentDto>>.Success(
            installments,
            "Simulasi cicilan berhasil dihitung."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReqCreateApplicationDto dto)
    {
        var result = await _service.CreateAsync(dto);

        return StatusCode(StatusCodes.Status201Created, ApiResponse<ResCreateApplicationDto>.Success(
            result,
            "Pengajuan kredit berhasil dibuat dan disimpan."));
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

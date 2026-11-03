using EasyCreditApi.Application.DTO;
using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private const string NotFoundMessage = "Customer tidak ditemukan.";

    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var (items, total) = await _service.GetPagedAsync(keyword, page, size);
        var data = new { page, size, total, items = items.Select(ToDto) };
        return Ok(ApiResponse<object>.Success(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var customer = await _service.GetByIdAsync(id);
        return customer is null
            ? NotFound(ApiResponse<object>.Fail(NotFoundMessage))
            : Ok(ApiResponse<ResCustomerDto>.Success(ToDto(customer)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReqCreateCustomerDto dto)
    {
        var customer = await _service.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = customer.CustomerId },
            ApiResponse<ResCustomerDto>.Success(ToDto(customer), "Customer berhasil dibuat."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ReqUpdateCustomerDto dto)
    {
        var customer = await _service.UpdateAsync(id, dto);
        return customer is null
            ? NotFound(ApiResponse<object>.Fail(NotFoundMessage))
            : Ok(ApiResponse<ResCustomerDto>.Success(ToDto(customer), "Customer berhasil diperbarui."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted
            ? Ok(ApiResponse<object>.Success(null, "Customer berhasil dihapus."))
            : NotFound(ApiResponse<object>.Fail(NotFoundMessage));
    }

    private static ResCustomerDto ToDto(Customer customer) => new()
    {
        CustomerId = customer.CustomerId,
        Name = customer.Name,
        Nik = customer.Nik,
        Phone = customer.Phone,
        Email = customer.Email,
        Address = customer.Address
    };
}

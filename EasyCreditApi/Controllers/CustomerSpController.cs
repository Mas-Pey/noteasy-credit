using EasyCreditApi.Application.DTO;
using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/customers/sp")]
[Authorize]
public class CustomerSpController : ControllerBase
{
    private const string NotFoundMessage = "Customer tidak ditemukan.";

    private readonly ICustomerSpService _service;

    public CustomerSpController(ICustomerSpService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(ApiResponse<object>.Success(new { total = items.Count, items }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _service.GetByIdAsync(id);

        return customer is null
            ? NotFound(ApiResponse<object>.Fail(NotFoundMessage))
            : Ok(ApiResponse<object>.Success(customer));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReqCreateCustomerDto dto)
    {
        var customer = await _service.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = customer.CustomerId },
            ApiResponse<object>.Success(customer, "Customer berhasil dibuat."));
    }
}
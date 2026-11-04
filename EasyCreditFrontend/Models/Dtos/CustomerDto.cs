using System;

namespace EasyCreditFrontend.Models.Dtos;

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Nik { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsDeleted { get; set; } = false;
}

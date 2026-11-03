using System;

namespace EasyCreditFrontend.Models.Dtos;

public class ApplicationListItemDto
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public int TenorMonths { get; set; }
    public string? Status { get; set; }
    public string? AprrovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

using System;
using System.Collections.Generic;

namespace EasyCreditFrontend.Models.Dtos;

public class CreditApplicationDetailDto
{
    public int ApplicationId { get; set; }
    public decimal Amount { get; set; }
    public int TenorMonths { get; set; }
    public decimal InterestRate { get; set; }
    public int CreditScore { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime ApplicationDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public CustomerDto? Customer { get; set; }
    public List<InstallmentDto> Installments { get; set; } = [];
}


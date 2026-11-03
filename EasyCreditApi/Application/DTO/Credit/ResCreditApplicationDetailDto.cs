using EasyCreditApi.Application.DTO.Customer;

namespace EasyCreditApi.Application.DTO.Credit;

public class ResCreditApplicationDetailDto
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

    // Data Customer (mst_customer)
    public ResCustomerDto? Customer { get; set; }

    // Cicilan (trn_credit_installment)
    public List<ResInstallmentDto> Installments { get; set; } = [];
}


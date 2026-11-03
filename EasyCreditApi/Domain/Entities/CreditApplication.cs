namespace EasyCreditApi.Domain.Entities;

public class CreditApplication
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public int TenorMonths { get; set; }
    public string? Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

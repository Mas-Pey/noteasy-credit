namespace EasyCreditApi.Application.DTO.Credit;

public class ResCreditApplicationListItemDto
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerNik { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TenorMonths { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime ApplicationDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}


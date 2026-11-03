using System;

namespace EasyCreditFrontend.Models.ViewModels;

public class ApplicationRowViewModel
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TenorMonths { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool CanApprove =>
        string.Equals(Status, "SUBMITTED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Status, "PENDING", StringComparison.OrdinalIgnoreCase);
}

namespace EasyCreditFrontend.Models.Dtos;

public class CreateApplicationResultDto
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public decimal LoanAmount { get; set; }
    public int TenorMonths { get; set; }
    public decimal InterestRate { get; set; }
    public int CreditScore { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime ApplicationDate { get; set; }
    public int TotalInstallments { get; set; }
}


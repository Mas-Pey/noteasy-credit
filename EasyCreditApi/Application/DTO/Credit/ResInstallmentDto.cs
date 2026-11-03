namespace EasyCreditApi.Application.DTO.Credit;

public class ResInstallmentDto
{
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal InstallmentAmount { get; set; }
    public string Status { get; set; } = "UNPAID";
}


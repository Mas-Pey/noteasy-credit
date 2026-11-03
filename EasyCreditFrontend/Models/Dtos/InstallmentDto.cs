namespace EasyCreditFrontend.Models.Dtos;

public class InstallmentDto
{
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal InstallmentAmount { get; set; }
    public string Status { get; set; } = "UNPAID";
}


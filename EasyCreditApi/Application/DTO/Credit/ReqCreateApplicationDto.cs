using System.ComponentModel.DataAnnotations;

namespace EasyCreditApi.Application.DTO.Credit;

public class ReqCreateApplicationDto
{
    [Required(ErrorMessage = "CustomerId wajib diisi.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Loan amount wajib diisi.")]
    [Range(1000000, 50000000, ErrorMessage = "Nominal pinjaman harus antara Rp 1.000.000 s/d Rp 50.000.000.")]
    public decimal LoanAmount { get; set; }

    [Required(ErrorMessage = "Tenor wajib diisi.")]
    public int TenorMonths { get; set; }
}


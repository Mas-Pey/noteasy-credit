using System.ComponentModel.DataAnnotations;
using EasyCreditFrontend.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyCreditFrontend.Models.ViewModels;

public class CreateApplicationViewModel
{
    [Required(ErrorMessage = "Customer wajib dipilih.")]
    [Display(Name = "Pilih Customer")]
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "Nominal pinjaman wajib diisi.")]
    [Range(1000000, 50000000, ErrorMessage = "Nominal pinjaman harus antara Rp 1.000.000 s/d Rp 50.000.000.")]
    [Display(Name = "Nominal Pinjaman (Rp)")]
    public decimal? LoanAmount { get; set; }

    [Required(ErrorMessage = "Pilihan tenor wajib dipilih.")]
    [Display(Name = "Tenor (Bulan)")]
    public int? TenorMonths { get; set; }

    // Dropdown list nasabah (API PostgreSQL)
    public List<SelectListItem> CustomerOptions { get; set; } = [];

    // Opsi tenor (6, 12, 24, 36 bulan)
    public List<SelectListItem> TenorOptions { get; set; } =
    [
        new() { Value = "6", Text = "6 Bulan" },
        new() { Value = "12", Text = "12 Bulan" },
        new() { Value = "24", Text = "24 Bulan" },
        new() { Value = "36", Text = "36 Bulan" }
    ];
}


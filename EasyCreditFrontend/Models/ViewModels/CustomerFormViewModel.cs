using System;
using System.ComponentModel.DataAnnotations;

namespace EasyCreditFrontend.Models.ViewModels;

public class CustomerFormViewModel
{
    public int CustomerId { get; set; }
    
    [Required(ErrorMessage = "Nama wajib diisi")]
    [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
    [Display(Name = "Nama Lengkap")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nik wajib diisi")]
    [RegularExpression(@"^\d{16,20}$", ErrorMessage = "NIK harus 16-20 digit angka.")]
    [Display(Name = "NIK")]
    public string Nik { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Nomor telepon maks 20 karakter.")]
    [Phone(ErrorMessage = "Format nomor telepon tidak valid.")]
    [Display(Name = "No. Telepon")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    [StringLength(100, ErrorMessage = "Email maks 100 karakter.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "Alamat maks 100 karakter")]
    [Display(Name = "Alamat")]
    public string? Address { get; set; }
}

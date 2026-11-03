using System.ComponentModel.DataAnnotations;

namespace EasyCreditApi.Application.DTO.Customer;

public class ReqUpdateCustomerDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{16,20}$", ErrorMessage = "NIK harus 16-20 digit angka.")]
    public string Nik { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? Address { get; set; }
}
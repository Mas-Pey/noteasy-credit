using System;
using EasyCreditFrontend.Models.Dtos;

namespace EasyCreditFrontend.Models.ViewModels;

// ViewModel khusus hal daftar customer
// Entity/DTO tidak boleh membawa urusan tampilan seperti paging/keyword
public class CustomerIndexViewModel
{
    public List<CustomerDto> Items { get; set; } = new();
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

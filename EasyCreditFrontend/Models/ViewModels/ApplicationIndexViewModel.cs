using System;

namespace EasyCreditFrontend.Models.ViewModels;

public class ApplicationIndexViewModel
{
    public List<ApplicationRowViewModel> Items { get; set; } = new();
    public string? Status { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

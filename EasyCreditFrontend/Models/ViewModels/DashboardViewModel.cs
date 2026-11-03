using System;

namespace EasyCreditFrontend.Models.ViewModels;

public class DashboardViewModel
{
    public bool ApiAvailable { get; set; } = true;
    public int TotalCustomers { get; set; }
    public int TotalApplications { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public List<ApplicationRowViewModel> Recent { get; set; } = new();
}

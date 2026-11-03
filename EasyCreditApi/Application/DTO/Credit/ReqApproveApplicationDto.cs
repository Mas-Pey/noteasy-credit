using System.ComponentModel.DataAnnotations;

namespace EasyCreditApi.Application.DTO.Credit;

public class ReqApproveApplicationDto
{
    [Required, StringLength(100)]
    public string Approver { get; set; } = string.Empty;
}
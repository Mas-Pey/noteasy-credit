using System;

namespace EasyCreditFrontend.Models.Dtos;

public class LoginDataDto
{
    public string AccessToken {get; set;} = string.Empty;
    public string Role {get; set;} = String.Empty;
    public string Name {get; set;} = string.Empty;
    public DateTime ExpiresAt {get; set;}
}
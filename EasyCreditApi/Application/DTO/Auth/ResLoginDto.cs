namespace EasyCreditApi.Application.DTO.Auth;

public record ResLoginDto
(
    string AccessToken,
    string Role,
    string Name,
    DateTime ExpiresAt
);

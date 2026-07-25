namespace backend.Application.DTOs.Auth
{
    public record LoginRequest(string PhoneOrEmail, string Password);

    public record LoginResponse(
        string Token,
        DateTime ExpiresAt,
        Guid UserId,
        string Name,
        string Role);
}
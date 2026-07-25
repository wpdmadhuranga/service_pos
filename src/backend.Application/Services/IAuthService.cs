    using backend.Application.DTOs.Auth;
    namespace backend.Application.Services
{
    public interface IAuthService
    {
        // Returns null on invalid credentials - the caller (controller) turns
        // that into a 401.
        Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    }
}
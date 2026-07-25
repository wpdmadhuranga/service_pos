using Microsoft.EntityFrameworkCore;
using backend.Application.Services;
using backend.Application.Common.Interfaces;
using backend.Application.DTOs.Auth;

using backend.Domain.Entities;

namespace backend.Infrastructure.Auth.Service
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _tokenGenerator;

        public AuthService(
            IApplicationDbContext db,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator tokenGenerator)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<LoginResponse?> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var identifier = request.PhoneOrEmail.Trim().ToLowerInvariant();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.PhoneOrEmail.ToLower() == identifier, cancellationToken);

            // Case 1: no matching account at all.
            if (user is null)
            {
                return null;
            }

            // Case 2: account exists but is deactivated.
            if (!user.IsActive)
            {
                return null;
            }

            // Case 3: wrong password.
            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return null;
            }

            // Success.
            var (token, expiresAt) = _tokenGenerator.GenerateToken(user);

            return new LoginResponse(token, expiresAt, user.Id, user.Name, user.Role.ToString());
        }
    }
}
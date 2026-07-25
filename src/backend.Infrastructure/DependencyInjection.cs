using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using backend.Application.Services;
using backend.Application.Common.Interfaces;
using backend.Infrastructure.Auth.Service;


namespace backend.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<global::backend.Infrastructure.Auth.JwtSettings>(configuration.GetSection("Jwt"));

            services.AddScoped<IPasswordHasher, global::backend.Infrastructure.Auth.PasswordHasher>();
            services.AddScoped<IJwtTokenGenerator>(sp =>
                (IJwtTokenGenerator)ActivatorUtilities.CreateInstance(
                    sp,
                    Type.GetType("backend.Infrastructure.Auth.JwtTokenGenerator, backend.Infrastructure")!));
            services.AddScoped<IAuthService, global::backend.Infrastructure.Auth.Service.AuthService>();

            return services;
        }
    }
}
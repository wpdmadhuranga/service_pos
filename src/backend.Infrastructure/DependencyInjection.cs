using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using backend.Application.Services;
using backend.Application.Pos;
using backend.Application.History;
using backend.Application.DTOs.Services;
using backend.Application.Common.Interfaces;
using backend.Infrastructure.Auth.Service;
using backend.Infrastructure.Pos.Service;
using backend.Infrastructure.History.Service;
using backend.Infrastructure.Services;


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
            services.AddScoped<IPosService, PosService>();
            services.AddScoped<IHistoryService, HistoryService>();
            services.AddScoped<IServiceAdminService, ServiceAdminService>();

            return services;
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using backend.Application.Common.Interfaces;
using backend.Persistence.Context;

namespace backend.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString =
                configuration["DATABASE_URL"]
                ?? configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string is not configured.");
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IApplicationDbContext>(
                sp => sp.GetRequiredService<ApplicationDbContext>());

            return services;
        }
    }
}
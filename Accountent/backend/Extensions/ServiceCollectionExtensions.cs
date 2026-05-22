using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Configuration;
using backend.Data;
using backend.Services.Common;
using backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace backend.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection не задан.");

            services.AddSingleton<IDatabaseConnectionSettings>(
                _ => new DatabaseConnectionSettings(connectionString));

            services.AddDbContextFactory<ApplicationDbContext>((_, options) =>
                options.UseNpgsql(connectionString));

            services.AddSingleton<IDatabaseContextFactory, DatabaseContextFactory>();

            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            services.AddSingleton<IDoubleEntryValidationService, DoubleEntryValidationService>();
            services.AddSingleton<IEntityValidationService, EntityValidationService>();
            services.AddSingleton<IEntityMappingService, EntityMappingService>();
            services.AddSingleton<IDateTimePeriodService, DateTimePeriodService>();
            services.AddSingleton<ISaldoCalculationService, SaldoCalculationService>();

            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IChartOfAccountsService, ChartOfAccountsService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<ICounterpartyService, CounterpartyService>();
            services.AddScoped<IReportService, ReportService>();

            return services;
        }
    }
}

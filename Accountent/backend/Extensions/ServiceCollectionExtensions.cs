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

            var useSqlite = configuration.GetValue<bool>("Testing:UseSqlite");
            services.AddDbContextFactory<ApplicationDbContext>((_, options) =>
            {
                if (useSqlite)
                    options.UseSqlite(connectionString);
                else
                    options.UseNpgsql(connectionString);
            });

            services.AddSingleton<IDatabaseContextFactory, DatabaseContextFactory>();

            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            services.AddSingleton<IDoubleEntryValidationService, DoubleEntryValidationService>();
            services.AddSingleton<IEntityValidationService, EntityValidationService>();
            services.AddSingleton<IEntityMappingService, EntityMappingService>();
            services.AddSingleton<IDateTimePeriodService, DateTimePeriodService>();
            services.AddSingleton<ISaldoCalculationService, SaldoCalculationService>();
            services.AddSingleton<IRefreshTokenService, RefreshTokenService>();

            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminAuditService, AdminAuditService>();
            services.AddScoped<IChartOfAccountsService, ChartOfAccountsService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<ICounterpartyService, CounterpartyService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }
    }
}

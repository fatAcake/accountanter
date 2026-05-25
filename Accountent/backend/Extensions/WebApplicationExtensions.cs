using backend.Abstractions.Data;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Extensions
{
    public static class WebApplicationExtensions
    {
        public static async Task MigrateAndSeedDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDatabaseContextFactory>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogWarning("ConnectionStrings:DefaultConnection не задан — миграции и seed пропущены.");
                return;
            }

            await using var db = factory.CreateDbContext();
            if (app.Environment.IsEnvironment("Testing") ||
                db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            {
                logger.LogInformation("Тестовая/in-memory БД — миграции и демо-seed пропущены.");
                return;
            }

            logger.LogInformation(
                "Проверка PostgreSQL ({Connection})",
                DatabaseBootstrap.DescribeConnection(connectionString));

            try
            {
                await DatabaseBootstrap.EnsureDatabaseExistsAsync(connectionString, logger);

                if (!await db.Database.CanConnectAsync())
                {
                    logger.LogWarning(
                        "PostgreSQL недоступен — миграции и seed пропущены. " +
                        "Запустите сервер БД, создайте базу accountent (scripts/init-postgres.ps1) " +
                        "и проверьте ConnectionStrings__DefaultConnection в .env.");
                    if (app.Environment.IsDevelopment())
                    {
                        logger.LogWarning(
                            "API запущен без БД: /api/auth/register и другие запросы вернут ошибку 503/500.");
                    }
                    return;
                }

                if ((await db.Database.GetPendingMigrationsAsync()).Any())
                {
                    logger.LogInformation("Применение миграций БД...");
                    await db.Database.MigrateAsync();
                }

                await DatabaseSeed.SeedAsync(db, logger);
                logger.LogInformation("Инициализация БД завершена.");
            }
            catch (PostgresException ex)
            {
                logger.LogWarning(
                    ex,
                    "PostgreSQL ({SqlState}): {Message}. Миграции и seed пропущены.",
                    ex.SqlState,
                    ex.MessageText);
            }
            catch (NpgsqlException ex)
            {
                logger.LogWarning(
                    ex,
                    "Не удалось подключиться к PostgreSQL. Убедитесь, что сервер запущен на {Connection}.",
                    DatabaseBootstrap.DescribeConnection(connectionString));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ошибка при миграции/seed — приложение запущено без инициализации БД.");
            }
        }
    }
}

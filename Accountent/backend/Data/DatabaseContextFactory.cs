using backend.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public sealed class DatabaseContextFactory : IDatabaseContextFactory
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;
        private readonly ILogger<DatabaseContextFactory> _logger;

        public DatabaseContextFactory(
            IDbContextFactory<ApplicationDbContext> factory,
            ILogger<DatabaseContextFactory> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public ApplicationDbContext CreateDbContext()
        {
            _logger.LogDebug("Создание экземпляра ApplicationDbContext");
            return _factory.CreateDbContext();
        }
    }
}

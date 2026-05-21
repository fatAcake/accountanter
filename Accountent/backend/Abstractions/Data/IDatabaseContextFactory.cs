using backend.Data;

namespace backend.Abstractions.Data
{
    public interface IDatabaseContextFactory
    {
        ApplicationDbContext CreateDbContext();
    }
}

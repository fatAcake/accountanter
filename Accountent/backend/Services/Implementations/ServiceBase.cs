using backend.Abstractions.Data;
using backend.Data;

namespace backend.Services.Implementations
{
    public abstract class ServiceBase<TService>
    {
        protected IDatabaseContextFactory DbFactory { get; }
        protected ILogger<TService> Logger { get; }

        protected ServiceBase(IDatabaseContextFactory dbFactory, ILogger<TService> logger)
        {
            DbFactory = dbFactory;
            Logger = logger;
        }

        protected ApplicationDbContext CreateContext() => DbFactory.CreateDbContext();
    }
}

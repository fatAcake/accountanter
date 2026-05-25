namespace backend.Extensions
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Строка подключения: явный ConnectionStrings:DefaultConnection или сборка из POSTGRES_*.
        /// </summary>
        public static string? ResolveDefaultConnectionString(IConfiguration configuration)
        {
            var direct = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            var user = configuration["POSTGRES_USER"];
            var database = configuration["POSTGRES_DB"];
            var password = configuration["POSTGRES_PASSWORD"];

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(database))
                return null;

            var host = configuration["POSTGRES_HOST"];
            if (string.IsNullOrWhiteSpace(host))
                host = "localhost";

            var port = configuration["POSTGRES_PORT"] ?? "5432";

            return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
        }

        public static void UseResolvedDefaultConnectionString(this WebApplicationBuilder builder)
        {
            var connectionString = ResolveDefaultConnectionString(builder.Configuration);
            if (string.IsNullOrWhiteSpace(connectionString))
                return;

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
            });
        }
    }
}

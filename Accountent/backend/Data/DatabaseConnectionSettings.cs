using backend.Abstractions.Data;

namespace backend.Data
{
    public sealed class DatabaseConnectionSettings : IDatabaseConnectionSettings
    {
        public DatabaseConnectionSettings(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Строка подключения не задана.", nameof(connectionString));

            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }
    }
}

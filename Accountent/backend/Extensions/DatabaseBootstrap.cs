using Npgsql;
using System.Text;

namespace backend.Extensions
{
    public static class DatabaseBootstrap
    {
        public static async Task EnsureDatabaseExistsAsync(
            string connectionString,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;

            if (string.IsNullOrWhiteSpace(databaseName))
                return;

            builder.Database = "postgres";

            try
            {
                await using var connection = new NpgsqlConnection(builder.ConnectionString);
                await connection.OpenAsync(cancellationToken);

                await using var check = new NpgsqlCommand(
                    "SELECT 1 FROM pg_database WHERE datname = @name",
                    connection);
                check.Parameters.AddWithValue("name", databaseName);

                if (await check.ExecuteScalarAsync(cancellationToken) is not null)
                    return;

                var escapedName = databaseName.Replace("\"", "\"\"", StringComparison.Ordinal);
                await using var create = new NpgsqlCommand(
                    $"""CREATE DATABASE "{escapedName}" ENCODING 'UTF8'""",
                    connection);
                await create.ExecuteNonQueryAsync(cancellationToken);

                logger.LogInformation("Создана база данных {Database}", databaseName);
            }
            catch (PostgresException ex) when (ex.SqlState is "42P04")
            {
                // duplicate_database
            }
        }

        public static string DescribeConnection(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var description = new StringBuilder();
            description.Append($"Host={builder.Host};Port={builder.Port};Database={builder.Database};Username={builder.Username}");
            return description.ToString();
        }
    }
}

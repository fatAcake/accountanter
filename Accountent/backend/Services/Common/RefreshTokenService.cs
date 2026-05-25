using backend.Abstractions.Common;

namespace backend.Services.Common
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        public string HashToken(string rawToken) =>
            BCrypt.Net.BCrypt.HashPassword(rawToken);

        public bool VerifyToken(string rawToken, string storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
                return false;

            if (IsBcryptHash(storedValue))
                return BCrypt.Net.BCrypt.Verify(rawToken, storedValue);

            return storedValue == rawToken;
        }

        public bool IsBcryptHash(string? storedValue) =>
            storedValue is not null &&
            storedValue.Length >= 4 &&
            storedValue.StartsWith("$2", StringComparison.Ordinal);
    }
}

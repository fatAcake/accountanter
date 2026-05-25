namespace backend.Abstractions.Common
{
    public interface IRefreshTokenService
    {
        string HashToken(string rawToken);
        bool VerifyToken(string rawToken, string storedValue);
        bool IsBcryptHash(string? storedValue);
    }
}

using backend.Abstractions.Common;
using backend.Models;
using backend.Models.DTOs;

namespace backend.Services.Common
{
    public sealed class EntityMappingService : IEntityMappingService
    {
        public AccountBrief MapAccount(Account account) => new()
        {
            id = account.id,
            number = account.number,
            name = account.name,
        };

        public CounterpartyBrief MapCounterparty(Counterparty counterparty) => new()
        {
            id = counterparty.id,
            name = counterparty.name,
            inn = counterparty.inn,
        };

        public CounterpartyResponse MapCounterpartyResponse(Counterparty counterparty) => new()
        {
            id = counterparty.id,
            name = counterparty.name,
            inn = counterparty.inn,
            contact = counterparty.contact,
            created_at = counterparty.created_at,
            edited_at = counterparty.edited_at,
        };

        public AuthResponse MapAuthResponse(Users user, string accessToken, DateTime accessExpires) => new()
        {
            id = user.id,
            nickname = user.nickname,
            email = user.email,
            role = user.role,
            jwt_token = accessToken,
            jwt_token_expires_at = accessExpires,
            refresh_token = user.refresh_token ?? string.Empty,
            refresh_token_expires_at = user.refresh_token_expires_at ?? DateTime.UtcNow,
        };
    }
}

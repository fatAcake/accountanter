using backend.Models;
using backend.Models.DTOs;

namespace backend.Abstractions.Common
{
    public interface IEntityMappingService
    {
        AccountBrief MapAccount(Account account);
        CounterpartyBrief MapCounterparty(Counterparty counterparty);
        CounterpartyResponse MapCounterpartyResponse(Counterparty counterparty);
        AuthResponse MapAuthResponse(Users user, string accessToken, DateTime accessExpires);
    }
}

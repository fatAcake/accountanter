using backend.Models.DTOs;

namespace backend.Abstractions.Services
{
    public interface ICounterpartyService
    {
        Task<List<CounterpartyResponse>> GetAllAsync(CounterpartyFilter filter);
        Task<CounterpartyResponse?> GetByIdAsync(int id);
        Task<(CounterpartyResponse? counterparty, string? error)> CreateAsync(CreateCounterpartyRequest request);
        Task<(CounterpartyResponse? counterparty, string? error)> UpdateAsync(int id, UpdateCounterpartyRequest request);
        Task<(bool success, string? error)> DeleteAsync(int id);
    }
}

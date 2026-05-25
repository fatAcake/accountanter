using backend.Models.DTOs;
using backend.Models.Results;

namespace backend.Abstractions.Services
{
    public interface ICounterpartyService
    {
        Task<List<CounterpartyResponse>> GetAllAsync(CounterpartyFilter filter);
        Task<CounterpartyResponse?> GetByIdAsync(int id);
        Task<ServiceResult<CounterpartyResponse>> CreateAsync(CreateCounterpartyRequest request);
        Task<ServiceResult<CounterpartyResponse>> UpdateAsync(int id, UpdateCounterpartyRequest request);
        Task<ServiceResult> DeleteAsync(int id);
    }
}

using backend.Models.DTOs;

namespace backend.Abstractions.Services
{
    public interface ITransactionService
    {
        Task<List<TransactionResponse>> GetAllAsync(TransactionFilter filter);
        Task<TransactionResponse?> GetByIdAsync(int id);
        Task<(TransactionResponse? transaction, string? error)> CreateAsync(CreateTransactionRequest request);
        Task<(TransactionResponse? transaction, string? error)> UpdateAsync(int id, UpdateTransactionRequest request);
        Task<(bool success, string? error)> DeleteAsync(int id);
    }
}

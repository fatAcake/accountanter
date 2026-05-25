using backend.Models.DTOs;
using backend.Models.Results;

namespace backend.Abstractions.Services
{
    public interface ITransactionService
    {
        Task<List<TransactionResponse>> GetAllAsync(TransactionFilter filter);
        Task<TransactionResponse?> GetByIdAsync(int id);
        Task<ServiceResult<TransactionResponse>> CreateAsync(CreateTransactionRequest request);
        Task<ServiceResult<TransactionResponse>> UpdateAsync(int id, UpdateTransactionRequest request);
        Task<ServiceResult> DeleteAsync(int id);
        Task<ImportTransactionsResult> ImportAsync(ImportTransactionsRequest request);
    }
}

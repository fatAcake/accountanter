using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Models;
using backend.Models.DTOs;
using backend.Models.Results;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class CounterpartyService : ServiceBase<CounterpartyService>, ICounterpartyService
    {
        private readonly IEntityValidationService _validation;
        private readonly IEntityMappingService _mapper;

        public CounterpartyService(
            IDatabaseContextFactory dbFactory,
            IEntityValidationService validation,
            IEntityMappingService mapper,
            ILogger<CounterpartyService> logger)
            : base(dbFactory, logger)
        {
            _validation = validation;
            _mapper = mapper;
        }

        public async Task<List<CounterpartyResponse>> GetAllAsync(CounterpartyFilter filter)
        {
            Logger.LogDebug("Список контрагентов, search={Search}", filter.search);

            await using var db = CreateContext();
            var query = db.Counterparties.AsNoTracking().Where(c => !c.deleted);

            if (!string.IsNullOrWhiteSpace(filter.search))
            {
                var term = filter.search.Trim().ToLowerInvariant();
                query = query.Where(c =>
                    c.name.ToLower().Contains(term) ||
                    (c.inn != null && c.inn.Contains(term)) ||
                    (c.contact != null && c.contact.ToLower().Contains(term)));
            }

            var items = await query.OrderBy(c => c.name).ToListAsync();
            return items.Select(_mapper.MapCounterpartyResponse).ToList();
        }

        public async Task<CounterpartyResponse?> GetByIdAsync(int id)
        {
            Logger.LogDebug("Контрагент {CounterpartyId}", id);

            await using var db = CreateContext();
            var counterparty = await db.Counterparties
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.id == id && !c.deleted);

            return counterparty is null ? null : _mapper.MapCounterpartyResponse(counterparty);
        }

        public async Task<ServiceResult<CounterpartyResponse>> CreateAsync(
            CreateCounterpartyRequest request)
        {
            Logger.LogInformation("Создание контрагента {Name}", request.name);

            await using var db = CreateContext();
            var inn = NormalizeInn(request.inn);
            var innError = await _validation.ValidateInnUniqueAsync(db, inn);
            if (innError is not null)
                return ServiceResult<CounterpartyResponse>.Fail(innError);

            var entity = new Counterparty
            {
                name = request.name.Trim(),
                inn = inn,
                contact = string.IsNullOrWhiteSpace(request.contact) ? null : request.contact.Trim(),
                created_at = DateTime.UtcNow,
            };

            db.Counterparties.Add(entity);
            await db.SaveChangesAsync();

            Logger.LogInformation("Контрагент {CounterpartyId} создан", entity.id);
            return ServiceResult<CounterpartyResponse>.Ok(_mapper.MapCounterpartyResponse(entity));
        }

        public async Task<ServiceResult<CounterpartyResponse>> UpdateAsync(
            int id, UpdateCounterpartyRequest request)
        {
            Logger.LogInformation("Обновление контрагента {CounterpartyId}", id);

            await using var db = CreateContext();
            var entity = await db.Counterparties.FirstOrDefaultAsync(c => c.id == id && !c.deleted);
            if (entity is null)
                return ServiceResult<CounterpartyResponse>.Fail(
                    ServiceErrorCode.NotFound,
                    "Контрагент не найден.");

            var inn = NormalizeInn(request.inn);
            var innError = await _validation.ValidateInnUniqueAsync(db, inn, excludeId: id);
            if (innError is not null)
                return ServiceResult<CounterpartyResponse>.Fail(innError);

            entity.name = request.name.Trim();
            entity.inn = inn;
            entity.contact = string.IsNullOrWhiteSpace(request.contact) ? null : request.contact.Trim();
            entity.edited_at = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return ServiceResult<CounterpartyResponse>.Ok(_mapper.MapCounterpartyResponse(entity));
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            Logger.LogInformation("Удаление контрагента {CounterpartyId}", id);

            await using var db = CreateContext();
            var entity = await db.Counterparties.FirstOrDefaultAsync(c => c.id == id && !c.deleted);
            if (entity is null)
                return ServiceResult.Fail(ServiceErrorCode.NotFound, "Контрагент не найден.");

            var usedInTransactions = await db.Transactions.AnyAsync(t =>
                !t.deleted && t.counterparty_id == id);

            var usedInLines = await db.TransactionLines.AnyAsync(l =>
                l.counterparty_id == id && !l.transaction.deleted);

            if (usedInTransactions || usedInLines)
                return ServiceResult.Fail(
                    ServiceErrorCode.Conflict,
                    "Нельзя удалить контрагента, используемого в проводках.");

            entity.deleted = true;
            entity.deleted_at = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        private static string? NormalizeInn(string? inn) =>
            string.IsNullOrWhiteSpace(inn) ? null : inn.Trim();
    }
}

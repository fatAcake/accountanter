using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Models;
using backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class ChartOfAccountsService : ServiceBase<ChartOfAccountsService>, IChartOfAccountsService
    {
        public ChartOfAccountsService(
            IDatabaseContextFactory dbFactory,
            ILogger<ChartOfAccountsService> logger)
            : base(dbFactory, logger)
        {
        }

        public async Task<List<AccountResponse>> GetAllAsync(bool flat = false)
        {
            Logger.LogDebug("Получение плана счетов, flat={Flat}", flat);

            await using var db = CreateContext();
            var accounts = await db.Accounts
                .AsNoTracking()
                .Where(a => !a.deleted)
                .OrderBy(a => a.number)
                .ToListAsync();

            return flat
                ? accounts.Select(a => MapToResponse(a)).ToList()
                : BuildTree(accounts);
        }

        public async Task<AccountResponse?> GetByIdAsync(int id)
        {
            Logger.LogDebug("Получение счёта {AccountId}", id);

            await using var db = CreateContext();
            var account = await db.Accounts
                .AsNoTracking()
                .Include(a => a.children.Where(c => !c.deleted))
                .FirstOrDefaultAsync(a => a.id == id && !a.deleted);

            return account is null ? null : MapToResponse(account, includeChildren: true);
        }

        public async Task<(AccountResponse? account, string? error)> CreateAsync(CreateAccountRequest request)
        {
            Logger.LogInformation("Создание счёта {Number}", request.number);

            await using var db = CreateContext();
            var number = request.number.Trim();

            if (await db.Accounts.AnyAsync(a => a.number == number && !a.deleted))
                return (null, "Счёт с таким номером уже существует.");

            var (parent, parentError) = await ResolveParentAsync(db, request.parent_id);
            if (parentError is not null)
                return (null, parentError);

            var account = new Account
            {
                number = number,
                name = request.name.Trim(),
                type = request.type,
                parent_id = parent?.id,
                is_system = false,
                created_at = DateTime.UtcNow,
            };

            db.Accounts.Add(account);
            await db.SaveChangesAsync();

            Logger.LogInformation("Счёт {AccountId} создан", account.id);
            return (MapToResponse(account), null);
        }

        public async Task<(AccountResponse? account, string? error)> UpdateAsync(int id, UpdateAccountRequest request)
        {
            Logger.LogInformation("Обновление счёта {AccountId}", id);

            await using var db = CreateContext();
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.id == id && !a.deleted);
            if (account is null)
                return (null, "Счёт не найден.");

            if (request.parent_id == id)
                return (null, "Счёт не может быть родителем самого себя.");

            var (parent, parentError) = await ResolveParentAsync(db, request.parent_id, excludeId: id);
            if (parentError is not null)
                return (null, parentError);

            if (request.parent_id is not null && await IsDescendantAsync(db, request.parent_id.Value, id))
                return (null, "Нельзя назначить дочерний счёт родителем.");

            account.name = request.name.Trim();
            account.type = request.type;
            account.parent_id = parent?.id;
            account.edited_at = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return (MapToResponse(account), null);
        }

        public async Task<(bool success, string? error)> DeleteAsync(int id)
        {
            Logger.LogInformation("Удаление счёта {AccountId}", id);

            await using var db = CreateContext();
            var account = await db.Accounts
                .Include(a => a.children)
                .FirstOrDefaultAsync(a => a.id == id && !a.deleted);

            if (account is null)
                return (false, "Счёт не найден.");

            if (account.is_system)
                return (false, "Системный счёт из типового плана нельзя удалить.");

            if (account.children.Any(c => !c.deleted))
                return (false, "Нельзя удалить счёт, у которого есть дочерние счета.");

            account.deleted = true;
            account.deleted_at = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return (true, null);
        }

        private static async Task<(Account? parent, string? error)> ResolveParentAsync(
            Data.ApplicationDbContext db, int? parentId, int? excludeId = null)
        {
            if (parentId is null)
                return (null, null);

            var parent = await db.Accounts.FirstOrDefaultAsync(a => a.id == parentId && !a.deleted);
            if (parent is null)
                return (null, "Родительский счёт не найден.");

            if (excludeId is not null && parent.id == excludeId)
                return (null, "Некорректный родительский счёт.");

            return (parent, null);
        }

        private static async Task<bool> IsDescendantAsync(
            Data.ApplicationDbContext db, int candidateParentId, int accountId)
        {
            var currentId = candidateParentId;
            while (true)
            {
                if (currentId == accountId)
                    return true;

                var parentId = await db.Accounts
                    .Where(a => a.id == currentId && !a.deleted)
                    .Select(a => a.parent_id)
                    .FirstOrDefaultAsync();

                if (parentId is null)
                    return false;

                currentId = parentId.Value;
            }
        }

        private static List<AccountResponse> BuildTree(List<Account> accounts)
        {
            var map = accounts.ToDictionary(a => a.id, a => MapToResponse(a));
            var roots = new List<AccountResponse>();

            foreach (var account in accounts)
            {
                var node = map[account.id];
                if (account.parent_id is null || !map.TryGetValue(account.parent_id.Value, out var parent))
                {
                    roots.Add(node);
                    continue;
                }

                parent.children.Add(node);
            }

            SortTree(roots);
            return roots;
        }

        private static void SortTree(List<AccountResponse> nodes)
        {
            nodes.Sort((a, b) => string.Compare(a.number, b.number, StringComparison.Ordinal));
            foreach (var child in nodes)
                SortTree(child.children);
        }

        private static AccountResponse MapToResponse(Account account, bool includeChildren = false)
        {
            var response = new AccountResponse
            {
                id = account.id,
                number = account.number,
                name = account.name,
                type = account.type,
                parent_id = account.parent_id,
                is_system = account.is_system,
                is_analytical = account.parent_id is not null,
            };

            if (includeChildren && account.children.Count > 0)
            {
                response.children = account.children
                    .Where(c => !c.deleted)
                    .OrderBy(c => c.number)
                    .Select(c => MapToResponse(c, includeChildren: true))
                    .ToList();
            }

            return response;
        }
    }
}

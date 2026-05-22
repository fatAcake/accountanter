using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace backend.Data
{
    /// <summary>
    /// Демо-данные для разработки. Повторный запуск не дублирует уже загруженные сущности.
    /// Тестовые пароли всех пользователей @accountent.local: <c>Password1!</c>
    /// </summary>
    public static class DatabaseSeed
    {
        public const string DemoPassword = "Password1!";
        public const string DemoEmailDomain = "@accountent.local";

        public static async Task SeedAsync(ApplicationDbContext db, ILogger? logger = null)
        {
            await ChartOfAccountsSeed.SeedAsync(db);

            var accounts = await db.Accounts
                .AsNoTracking()
                .ToDictionaryAsync(a => a.number);

            await SeedExtraAccountsAsync(db, accounts, logger);

            if (!await db.Users.AnyAsync(u => u.email.EndsWith(DemoEmailDomain)))
                await SeedUsersAsync(db, logger);

            if (!await db.Counterparties.AnyAsync(c => !c.deleted))
                await SeedCounterpartiesAsync(db, logger);

            if (!await db.Transactions.AnyAsync(t => !t.deleted))
                await SeedTransactionsAsync(db, accounts, logger);
        }

        private static async Task SeedExtraAccountsAsync(
            ApplicationDbContext db,
            Dictionary<string, Account> accounts,
            ILogger? logger)
        {
            var extras = new (string number, string name, AccountType type, string parentNumber)[]
            {
                ("76.01", "Расчёты с покупателями (аналитика)", AccountType.active_passive, "76"),
                ("76.02", "Расчёты с поставщиками (аналитика)", AccountType.active_passive, "76"),
                ("10.02", "Покупные полуфабрикаты", AccountType.active, "10"),
                ("20.01", "Основное производство — цех №1", AccountType.active, "20"),
            };

            var added = false;
            foreach (var (number, name, type, parentNumber) in extras)
            {
                if (accounts.ContainsKey(number))
                    continue;

                if (!accounts.TryGetValue(parentNumber, out var parent))
                    continue;

                var parentTracked = await db.Accounts.FindAsync(parent.id);
                if (parentTracked is null)
                    continue;

                var account = new Account
                {
                    number = number,
                    name = name,
                    type = type,
                    parent_id = parentTracked.id,
                    is_system = false,
                    created_at = DateTime.UtcNow,
                };
                db.Accounts.Add(account);
                accounts[number] = account;
                added = true;
            }

            if (added)
            {
                await db.SaveChangesAsync();
                logger?.LogInformation("Добавлены дополнительные аналитические счета.");
            }
        }

        private static async Task SeedUsersAsync(ApplicationDbContext db, ILogger? logger)
        {
            var now = DateTime.UtcNow;
            var hash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

            var users = new Users[]
            {
                new()
                {
                    nickname = "Администратор",
                    email = "admin" + DemoEmailDomain,
                    password = hash,
                    role = Roles.admin,
                    registration_date = now.AddMonths(-6),
                },
                new()
                {
                    nickname = "Мария Бухгалтерова",
                    email = "accountant" + DemoEmailDomain,
                    password = hash,
                    role = Roles.accountant,
                    registration_date = now.AddMonths(-5),
                },
                new()
                {
                    nickname = "Иван Проводников",
                    email = "accountant2" + DemoEmailDomain,
                    password = hash,
                    role = Roles.accountant,
                    registration_date = now.AddMonths(-4),
                },
                new()
                {
                    nickname = "Ольга Наблюдателева",
                    email = "observer" + DemoEmailDomain,
                    password = hash,
                    role = Roles.observer,
                    registration_date = now.AddMonths(-3),
                },
                new()
                {
                    nickname = "Пётр Аудитов",
                    email = "observer2" + DemoEmailDomain,
                    password = hash,
                    role = Roles.observer,
                    registration_date = now.AddMonths(-2),
                },
            };

            db.Users.AddRange(users);
            await db.SaveChangesAsync();
            logger?.LogInformation("Создано {Count} демо-пользователей ({Domain}, пароль: {Password}).",
                users.Length, DemoEmailDomain, DemoPassword);
        }

        private static async Task SeedCounterpartiesAsync(ApplicationDbContext db, ILogger? logger)
        {
            var now = DateTime.UtcNow;
            var items = new Counterparty[]
            {
                new() { name = "ООО «Сибирь-Трейд»", inn = "5408123456", contact = "sibir-trade@mail.ru, +7 391 200-11-22", created_at = now.AddMonths(-5) },
                new() { name = "АО «КрасноярскЭнерго»", inn = "2460123456", contact = "buh@krskenergo.ru", created_at = now.AddMonths(-5) },
                new() { name = "ИП Петров А.С.", inn = "540512345678", contact = "+7 923 555-01-01", created_at = now.AddMonths(-4) },
                new() { name = "ООО «СеверЛес»", inn = "5402123456", contact = "sales@severles.ru", created_at = now.AddMonths(-4) },
                new() { name = "ПАО «Ромашка»", inn = "7701234567", contact = "moscow@romashka.ru", created_at = now.AddMonths(-4) },
                new() { name = "ООО «ТехноСнаб»", inn = "5403123456", contact = "order@technosnab.ru, 8-800-100-20-30", created_at = now.AddMonths(-3) },
                new() { name = "ИП Сидорова Е.В.", inn = "540612345678", contact = "ev@sidorova.ru", created_at = now.AddMonths(-3) },
                new() { name = "ООО «АгроПродукт»", inn = "5407123456", contact = "agro@product.ru", created_at = now.AddMonths(-3) },
                new() { name = "Банк «Енисей» (ПАО)", inn = "2465012345", contact = "corp@eniseybank.ru", created_at = now.AddMonths(-2) },
                new() { name = "ООО «МегаСтрой»", inn = "5409123456", contact = "stroy@megastroy.ru", created_at = now.AddMonths(-2) },
                new() { name = "ООО «Вектор Логистик»", inn = "5401123456", contact = "logistics@vector.ru", created_at = now.AddMonths(-2) },
                new() { name = "ИП Козлов Д.И.", inn = "540412345678", contact = null, created_at = now.AddMonths(-1) },
                new() { name = "ООО «Альфа-Консалт»", inn = "5405123456", contact = "info@alfa-consult.ru", created_at = now.AddMonths(-1) },
                new() { name = "ООО «Дельта-Маркет»", inn = "5406123456", contact = "sales@delta-market.ru", created_at = now.AddDays(-20) },
                new() { name = "АО «ГорноПром»", inn = "2461123456", contact = "buh@goroprom.ru, +7 391 333-44-55", created_at = now.AddDays(-10) },
                new() { name = "ООО «УчётПлюс» (аутсорс)", inn = "5408123457", contact = "hello@uchetplus.ru", created_at = now.AddDays(-5) },
            };

            db.Counterparties.AddRange(items);
            await db.SaveChangesAsync();
            logger?.LogInformation("Создано {Count} контрагентов.", items.Length);
        }

        private static async Task SeedTransactionsAsync(
            ApplicationDbContext db,
            Dictionary<string, Account> accountsSnapshot,
            ILogger? logger)
        {
            var accounts = await db.Accounts.ToDictionaryAsync(a => a.number);
            var counterparties = await db.Counterparties
                .Where(c => !c.deleted)
                .OrderBy(c => c.id)
                .ToListAsync();

            if (counterparties.Count == 0)
                return;

            int Cp(int index) => counterparties[index % counterparties.Count].id;

            var transactions = new List<Transaction>();

            // --- Простые проводки (разные месяцы и операции) ---
            transactions.Add(MakeSimple(accounts, Day(2026, 1, 10), "Поступление от покупателя — оплата по счёту №12",
                "51.01", "62.01", 485_000m, Cp(0)));
            transactions.Add(MakeSimple(accounts, Day(2026, 1, 15), "Списание материалов в производство",
                "20", "10.01", 127_350.50m));
            transactions.Add(MakeSimple(accounts, Day(2026, 1, 20), "Оплата поставщику за сырьё",
                "60.01", "51.01", 89_200m, Cp(3)));
            transactions.Add(MakeSimple(accounts, Day(2026, 1, 25), "Начислена заработная плата за январь",
                "70", "51.01", 312_000m));
            transactions.Add(MakeSimple(accounts, Day(2026, 1, 28), "НДС с продаж за январь",
                "68.01", "19", 45_600m));
            transactions.Add(MakeSimple(accounts, Day(2026, 2, 5), "Поступление выручки в кассу",
                "50.01", "90.01", 15_800m));
            transactions.Add(MakeSimple(accounts, Day(2026, 2, 12), "Оплата аренды офиса",
                "26", "51.01", 55_000m, Cp(9)));
            transactions.Add(MakeSimple(accounts, Day(2026, 2, 18), "Покупка канцтоваров",
                "26", "60.01", 4_280.75m, Cp(5)));
            transactions.Add(MakeSimple(accounts, Day(2026, 2, 22), "Возврат подотчётных — излишек",
                "50.01", "71", 2_150m));
            transactions.Add(MakeSimple(accounts, Day(2026, 2, 28), "Закрытие счёта 90 — списание себестоимости",
                "90.02", "43", 198_400m));
            transactions.Add(MakeSimple(accounts, Day(2026, 3, 3), "Поступление на расчётный счёт — аванс",
                "51.01", "62.01", 1_200_000m, Cp(4)));
            transactions.Add(MakeSimple(accounts, Day(2026, 3, 10), "Электроэнергия — счёт КрасноярскЭнерго",
                "26", "60.01", 38_920m, Cp(1)));
            transactions.Add(MakeSimple(accounts, Day(2026, 3, 15), "Выдача подотчётных на командировку",
                "71", "50.01", 18_500m));
            transactions.Add(MakeSimple(accounts, Day(2026, 3, 20), "Начисление амортизации ОС",
                "26", "02", 24_000m));
            transactions.Add(MakeSimple(accounts, Day(2026, 3, 25), "Оплата страховых взносов",
                "69", "51.01", 94_300m));
            transactions.Add(MakeSimple(accounts, Day(2026, 4, 2), "Реализация готовой продукции",
                "62.01", "90.01", 567_890m, Cp(0)));
            transactions.Add(MakeSimple(accounts, Day(2026, 4, 8), "Поступление товара от поставщика",
                "45", "60.01", 156_000m, Cp(5)));
            transactions.Add(MakeSimple(accounts, Day(2026, 4, 14), "Комиссия банка за РКО",
                "91.02", "51.01", 1_890m, Cp(8)));
            transactions.Add(MakeSimple(accounts, Day(2026, 4, 20), "Проценты по краткосрочному кредиту",
                "91.02", "66", 12_450m));
            transactions.Add(MakeSimple(accounts, Day(2026, 4, 25), "Прочие доходы — штраф контрагенту",
                "51.01", "91.01", 5_000m, Cp(10)));
            transactions.Add(MakeSimple(accounts, Day(2026, 5, 5), "Оплата логистики",
                "44", "60.01", 73_200m, Cp(10)));
            transactions.Add(MakeSimple(accounts, Day(2026, 5, 10), "Поступление от ИП Петров",
                "51.01", "62.01", 42_300m, Cp(2)));
            transactions.Add(MakeSimple(accounts, Day(2026, 5, 15), "Списание брака в производстве",
                "94", "20", 8_750m));
            transactions.Add(MakeSimple(accounts, Day(2026, 5, 18), "Резерв под отпуска",
                "26", "96", 28_000m));

            // --- Сложные проводки ---
            transactions.Add(MakeComplex(accounts, Day(2026, 2, 10), "Распределение общехозяйственных расходов",
                [
                    ("26", EntrySide.debit, 15_000m, null),
                    ("20", EntrySide.debit, 22_500m, null),
                    ("44", EntrySide.debit, 7_500m, null),
                    ("70", EntrySide.credit, 45_000m, null),
                ]));
            transactions.Add(MakeComplex(accounts, Day(2026, 3, 31), "Закрытие месяца — распределение 91",
                [
                    ("91.02", EntrySide.debit, 18_200m, null),
                    ("91.02", EntrySide.debit, 6_800m, null),
                    ("60.01", EntrySide.credit, 12_000m, Cp(3)),
                    ("51.01", EntrySide.credit, 13_000m, null),
                ]));
            transactions.Add(MakeComplex(accounts, Day(2026, 4, 30), "Выпуск готовой продукции (сложная)",
                [
                    ("43", EntrySide.debit, 240_000m, null),
                    ("20", EntrySide.credit, 180_000m, null),
                    ("10.01", EntrySide.credit, 45_000m, null),
                    ("70", EntrySide.credit, 15_000m, null),
                ]));
            transactions.Add(MakeComplex(accounts, Day(2026, 5, 20), "Расчёты с несколькими контрагентами",
                [
                    ("62.01", EntrySide.debit, 95_000m, Cp(0)),
                    ("62.01", EntrySide.debit, 38_500m, Cp(4)),
                    ("76.01", EntrySide.debit, 12_000m, Cp(13)),
                    ("90.01", EntrySide.credit, 140_000m, null),
                    ("68.01", EntrySide.credit, 5_500m, null),
                ]));
            transactions.Add(MakeComplex(accounts, Day(2026, 5, 21), "Погашение задолженности поставщикам",
                [
                    ("60.01", EntrySide.debit, 50_000m, Cp(3)),
                    ("60.01", EntrySide.debit, 30_000m, Cp(5)),
                    ("60.01", EntrySide.debit, 20_000m, Cp(7)),
                    ("51.01", EntrySide.credit, 100_000m, null),
                ]));

            db.Transactions.AddRange(transactions);
            await db.SaveChangesAsync();
            logger?.LogInformation("Создано {Count} демо-проводок ({Simple} простых, {Complex} сложных).",
                transactions.Count,
                transactions.Count(t => !t.is_complex),
                transactions.Count(t => t.is_complex));
        }

        private static DateTime Day(int year, int month, int day) =>
            DateTime.SpecifyKind(new DateTime(year, month, day, 12, 0, 0), DateTimeKind.Utc);

        private static Transaction MakeSimple(
            Dictionary<string, Account> accounts,
            DateTime date,
            string description,
            string debitNumber,
            string creditNumber,
            decimal amount,
            int? counterpartyId = null)
        {
            var debit = accounts[debitNumber];
            var credit = accounts[creditNumber];

            var tx = new Transaction
            {
                date = date,
                description = description,
                is_complex = false,
                debit_account_id = debit.id,
                credit_account_id = credit.id,
                amount = amount,
                counterparty_id = counterpartyId,
                created_at = date,
            };

            tx.lines.Add(new TransactionLine
            {
                account_id = debit.id,
                amount = amount,
                side = EntrySide.debit,
                counterparty_id = counterpartyId,
            });
            tx.lines.Add(new TransactionLine
            {
                account_id = credit.id,
                amount = amount,
                side = EntrySide.credit,
            });

            return tx;
        }

        private static Transaction MakeComplex(
            Dictionary<string, Account> accounts,
            DateTime date,
            string description,
            (string accountNumber, EntrySide side, decimal amount, int? counterpartyId)[] lines)
        {
            var tx = new Transaction
            {
                date = date,
                description = description,
                is_complex = true,
                created_at = date,
            };

            decimal debitTotal = 0;
            decimal creditTotal = 0;

            foreach (var (accountNumber, side, amount, cpId) in lines)
            {
                var account = accounts[accountNumber];
                tx.lines.Add(new TransactionLine
                {
                    account_id = account.id,
                    amount = amount,
                    side = side,
                    counterparty_id = cpId,
                });

                if (side == EntrySide.debit)
                    debitTotal += amount;
                else
                    creditTotal += amount;
            }

            tx.amount = debitTotal;
            return tx;
        }
    }
}

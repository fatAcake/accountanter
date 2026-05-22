using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    internal static class ChartOfAccountsSeed
    {
        public static IReadOnlyList<(string number, string name, AccountType type, string? parentNumber)> Data { get; } =
        [
            ("01", "Основные средства", AccountType.active, null),
            ("01.01", "Основные средства в организации", AccountType.active, "01"),
            ("02", "Амортизация основных средств", AccountType.passive, null),
            ("04", "Нематериальные активы", AccountType.active, null),
            ("05", "Амортизация нематериальных активов", AccountType.passive, null),
            ("10", "Материалы", AccountType.active, null),
            ("10.01", "Сырье и материалы", AccountType.active, "10"),
            ("19", "НДС по приобретенным ценностям", AccountType.active, null),
            ("20", "Основное производство", AccountType.active, null),
            ("26", "Общехозяйственные расходы", AccountType.active, null),
            ("44", "Расходы на продажу", AccountType.active, null),
            ("43", "Готовая продукция", AccountType.active, null),
            ("45", "Товары", AccountType.active, null),
            ("50", "Касса", AccountType.active, null),
            ("50.01", "Касса организации", AccountType.active, "50"),
            ("51", "Расчетные счета", AccountType.active, null),
            ("51.01", "Расчетные счета", AccountType.active, "51"),
            ("52", "Валютные счета", AccountType.active, null),
            ("57", "Переводы в пути", AccountType.active, null),
            ("58", "Финансовые вложения", AccountType.active, null),
            ("60", "Расчеты с поставщиками и подрядчиками", AccountType.active_passive, null),
            ("60.01", "Расчеты с поставщиками", AccountType.active_passive, "60"),
            ("62", "Расчеты с покупателями и заказчиками", AccountType.active_passive, null),
            ("62.01", "Расчеты с покупателями", AccountType.active_passive, "62"),
            ("66", "Расчеты по краткосрочным кредитам и займам", AccountType.passive, null),
            ("67", "Расчеты по долгосрочным кредитам и займам", AccountType.passive, null),
            ("68", "Расчеты по налогам и сборам", AccountType.active_passive, null),
            ("68.01", "НДС", AccountType.active_passive, "68"),
            ("69", "Расчеты по социальному страхованию", AccountType.passive, null),
            ("70", "Расчеты с персоналом по оплате труда", AccountType.passive, null),
            ("71", "Расчеты с подотчетными лицами", AccountType.active_passive, null),
            ("75", "Расчеты с учредителями", AccountType.passive, null),
            ("76", "Расчеты с разными дебиторами и кредиторами", AccountType.active_passive, null),
            ("80", "Уставный капитал", AccountType.passive, null),
            ("82", "Резервный капитал", AccountType.passive, null),
            ("83", "Добавочный капитал", AccountType.passive, null),
            ("84", "Нераспределенная прибыль (непокрытый убыток)", AccountType.active_passive, null),
            ("90", "Продажи", AccountType.active_passive, null),
            ("90.01", "Выручка", AccountType.active_passive, "90"),
            ("90.02", "Себестоимость продаж", AccountType.active_passive, "90"),
            ("91", "Прочие доходы и расходы", AccountType.active_passive, null),
            ("91.01", "Прочие доходы", AccountType.active_passive, "91"),
            ("91.02", "Прочие расходы", AccountType.active_passive, "91"),
            ("94", "Недостачи и потери от порчи ценностей", AccountType.active, null),
            ("96", "Резервы предстоящих расходов", AccountType.passive, null),
            ("97", "Расходы будущих периодов", AccountType.active, null),
            ("98", "Доходы будущих периодов", AccountType.passive, null),
            ("99", "Прибыли и убытки", AccountType.active_passive, null),
        ];

        public static async Task SeedAsync(ApplicationDbContext db)
        {
            if (await db.Accounts.AnyAsync())
                return;

            var byNumber = new Dictionary<string, Account>();

            foreach (var (number, name, type, _) in Data)
            {
                var account = new Account
                {
                    number = number,
                    name = name,
                    type = type,
                    is_system = true,
                    created_at = DateTime.UtcNow,
                };
                db.Accounts.Add(account);
                byNumber[number] = account;
            }

            await db.SaveChangesAsync();

            foreach (var (number, _, _, parentNumber) in Data)
            {
                if (parentNumber is null)
                    continue;
                byNumber[number].parent_id = byNumber[parentNumber].id;
            }

            await db.SaveChangesAsync();
        }
    }
}

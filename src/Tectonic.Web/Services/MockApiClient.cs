using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

// Canned data so the UI works before the API is up. The real rules live in the API.
// Price increase/decrease and duplicate charge are faked here and respect the UI settings
// (rule switches, email master switch, critical-only, daily summary) so the demo behaves like the real thing.
// Messages are rendered in the language chosen in Settings, as the API would do.
public class MockApiClient(UiState ui, Loc loc) : IApiClient
{
    private static readonly DateTime Today = DateTime.Today;

    private sealed record MockNotification(int Id, NotificationType Type, string Company, DateTime CreatedAt,
        NotificationDelivery Delivery, Func<Loc, string> Message);

    private readonly List<Transaction> _transactions =
    [
        new() { Id = 1, Type = TransactionType.Income,  Company = "Employer NV", Amount = 2800m,  Date = Today.AddMonths(-2).AddDays(-3) },
        new() { Id = 2, Type = TransactionType.Expense, Company = "Netflix",     Amount = 13.99m, Date = Today.AddMonths(-2) },
        new() { Id = 3, Type = TransactionType.Expense, Company = "Netflix",     Amount = 13.99m, Date = Today.AddMonths(-1) },
        new() { Id = 4, Type = TransactionType.Expense, Company = "Proximus",    Amount = 45m,    Date = Today.AddMonths(-1).AddDays(2) },
        new() { Id = 5, Type = TransactionType.Expense, Company = "Proximus",    Amount = 49.50m, Date = Today.AddDays(-5) },
        new() { Id = 6, Type = TransactionType.Expense, Company = "Spotify",     Amount = 10.99m, Date = Today.AddMonths(-1).AddDays(-4) },
        new() { Id = 7, Type = TransactionType.Expense, Company = "Engie",       Amount = 400m,   Date = Today.AddDays(-10) },
    ];

    private readonly List<RecurringExpense> _recurring =
    [
        new() { Id = 1, Company = "Rent – Avenue Louise", Amount = 950m,   DayOfMonth = 1,         AddedAt = Today.AddMonths(-6), IsCritical = true },
        new() { Id = 2, Company = "Engie",                Amount = 85m,    DayOfMonth = 5,         AddedAt = Today.AddMonths(-4), IsCritical = true },
        new() { Id = 3, Company = "Home insurance",       Amount = 34.50m, DayOfMonth = 12,        AddedAt = Today.AddMonths(-8), IsCritical = true },
        new() { Id = 4, Company = "Netflix",              Amount = 13.99m, DayOfMonth = Today.Day, AddedAt = Today.AddMonths(-1) },
        new() { Id = 5, Company = "Spotify",              Amount = 10.99m, DayOfMonth = 26,        AddedAt = Today.AddMonths(-3) },
        new() { Id = 6, Company = "Colruyt",              Amount = 62.40m, DayOfMonth = 20,        AddedAt = Today.AddMonths(-2) },
    ];

    private readonly List<MockNotification> _notifications =
    [
        new(1, NotificationType.RecurringAdded, "Netflix", Today.AddMonths(-1), NotificationDelivery.Instant,
            l => l.F("msg.recAdded", "Netflix")),
        new(2, NotificationType.PriceIncrease, "Proximus", Today.AddDays(-5), NotificationDelivery.Instant,
            l => l.F("msg.priceUp", "Proximus", l.Money(45m), l.Money(49.50m))),
        new(3, NotificationType.RecurringRemoved, "Spotify", Today.AddDays(-2), NotificationDelivery.Instant,
            l => l.F("msg.recRemoved", "Spotify")),
        new(4, NotificationType.PaymentIncomplete, "Engie", Today.AddDays(-3), NotificationDelivery.Instant,
            l => l.F("msg.incomplete", "Engie", l.Money(400m), l.Money(500m), l.Money(100m))),
        new(5, NotificationType.UpcomingPayment, "Rent – Avenue Louise", Today.AddHours(8), NotificationDelivery.Instant,
            l => l.F("msg.upcoming", "Rent – Avenue Louise", l.Money(950m))),
    ];

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        await Task.Delay(300); // simulate latency so loading states are visible
        return [.. _transactions];
    }

    public Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        if (transaction.Type == TransactionType.Expense)
        {
            var sameCompany = _transactions
                .Where(t => t.Type == TransactionType.Expense && t.Company.Equals(transaction.Company, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var duplicate = sameCompany.Any(t => t.Amount == transaction.Amount && Math.Abs((t.Date - transaction.Date).TotalDays) <= 2);
            var previous = sameCompany.OrderBy(t => t.Date).LastOrDefault();

            if (duplicate)
            {
                var amount = transaction.Amount;
                TryNotify(NotificationType.DuplicateCharge, transaction.Company,
                    l => l.F("msg.duplicate", transaction.Company, l.Money(amount)));
            }
            else if (previous is not null && transaction.Amount != previous.Amount)
            {
                var (from, to) = (previous.Amount, transaction.Amount);
                var up = to > from;
                TryNotify(up ? NotificationType.PriceIncrease : NotificationType.PriceDecrease, transaction.Company,
                    l => l.F(up ? "msg.priceUp" : "msg.priceDown", transaction.Company, l.Money(from), l.Money(to)));
            }
        }

        transaction.Id = _transactions.Max(t => t.Id) + 1;
        _transactions.Add(transaction);
        return Task.FromResult(transaction);
    }

    public Task<List<RecurringExpense>> GetRecurringExpensesAsync() => Task.FromResult<List<RecurringExpense>>([.. _recurring]);

    public Task SetCriticalAsync(int recurringExpenseId, bool isCritical)
    {
        var item = _recurring.FirstOrDefault(r => r.Id == recurringExpenseId);
        if (item is not null) item.IsCritical = isCritical;
        return Task.CompletedTask;
    }

    public Task<List<Notification>> GetNotificationsAsync() =>
        Task.FromResult(_notifications.Select(n => new Notification
        {
            Id = n.Id,
            Type = n.Type,
            Company = n.Company,
            CreatedAt = n.CreatedAt,
            Delivery = n.Delivery,
            Message = n.Message(loc),
        }).ToList());

    private void TryNotify(NotificationType type, string company, Func<Loc, string> message)
    {
        var rule = RuleCatalog.For(type);
        if (!ui.NotificationsEnabled || !ui.RulesEnabled[rule.Id]) return;

        var critical = _recurring.Any(r => r.IsCritical && r.Company.Equals(company, StringComparison.OrdinalIgnoreCase));
        if (ui.CriticalOnly && !critical && !rule.AlwaysInstant) return;

        var delivery = ui.DailyDigest && !critical && !rule.AlwaysInstant ? NotificationDelivery.Digest : NotificationDelivery.Instant;
        _notifications.Add(new(_notifications.Max(n => n.Id) + 1, type, company, DateTime.Now, delivery, message));
    }
}

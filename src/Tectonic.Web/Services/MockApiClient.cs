using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

// Canned data so the UI works before the API is up. The real rules live in the API.
public class MockApiClient : IApiClient
{
    private static readonly DateTime Today = DateTime.Today;

    private readonly List<Transaction> _transactions =
    [
        new() { Id = 1, Type = TransactionType.Income,  Company = "Employer NV", Amount = 2800m,  Date = Today.AddMonths(-2).AddDays(-3) },
        new() { Id = 2, Type = TransactionType.Expense, Company = "Netflix",     Amount = 13.99m, Date = Today.AddMonths(-2) },
        new() { Id = 3, Type = TransactionType.Expense, Company = "Netflix",     Amount = 13.99m, Date = Today.AddMonths(-1) },
        new() { Id = 4, Type = TransactionType.Expense, Company = "Proximus",    Amount = 45m,    Date = Today.AddMonths(-1).AddDays(2) },
        new() { Id = 5, Type = TransactionType.Expense, Company = "Proximus",    Amount = 49.50m, Date = Today.AddDays(2 - 7) },
    ];

    private readonly List<RecurringExpense> _recurring =
    [
        new() { Id = 1, Company = "Netflix", Amount = 13.99m, DayOfMonth = Today.Day, AddedAt = Today.AddMonths(-1) },
    ];

    private readonly List<Notification> _notifications =
    [
        new() { Id = 1, Type = NotificationType.RecurringAdded,   Company = "Netflix",  Message = "Netflix was added to your recurring expenses.",        CreatedAt = Today.AddMonths(-1) },
        new() { Id = 2, Type = NotificationType.PriceIncrease,    Company = "Proximus", Message = "Your expense at Proximus went up from €45.00 to €49.50.", CreatedAt = Today.AddDays(-5) },
        new() { Id = 3, Type = NotificationType.RecurringRemoved, Company = "Spotify",  Message = "Spotify was removed from your recurring expenses.",     CreatedAt = Today.AddDays(-2) },
    ];

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        await Task.Delay(300); // simulate latency so loading states are visible
        return [.. _transactions];
    }

    public Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        // Only the price-increase rule is faked here, so the UI's "email sent" toast can be tried without the API.
        var previous = _transactions
            .Where(t => t.Type == TransactionType.Expense && t.Company.Equals(transaction.Company, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Date)
            .LastOrDefault();
        if (transaction.Type == TransactionType.Expense && previous is not null && transaction.Amount > previous.Amount)
        {
            _notifications.Add(new()
            {
                Id = _notifications.Max(n => n.Id) + 1,
                Type = NotificationType.PriceIncrease,
                Company = transaction.Company,
                Message = $"Your expense at {transaction.Company} went up from €{previous.Amount:0.00} to €{transaction.Amount:0.00}.",
                CreatedAt = DateTime.Now,
            });
        }

        transaction.Id = _transactions.Max(t => t.Id) + 1;
        _transactions.Add(transaction);
        return Task.FromResult(transaction);
    }

    public Task<List<RecurringExpense>> GetRecurringExpensesAsync() => Task.FromResult<List<RecurringExpense>>([.. _recurring]);

    public Task<List<Notification>> GetNotificationsAsync() => Task.FromResult<List<Notification>>([.. _notifications]);
}

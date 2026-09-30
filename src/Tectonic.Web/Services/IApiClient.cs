using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

public interface IApiClient
{
    Task<List<Transaction>> GetTransactionsAsync();
    Task<Transaction> AddTransactionAsync(Transaction transaction);
    Task<List<RecurringExpense>> GetRecurringExpensesAsync();
    Task SetCriticalAsync(int recurringExpenseId, bool isCritical);
    Task<List<Notification>> GetNotificationsAsync();
}

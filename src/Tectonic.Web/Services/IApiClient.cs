using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

public interface IApiClient
{
    bool IsMock { get; }
    Task<AuthSession> LoginAsync(string email, string password);
    Task<AuthSession> SignupAsync(string name, string email, string password);
    Task<List<Transaction>> GetTransactionsAsync();
    Task<Transaction> AddTransactionAsync(Transaction transaction);
    Task<List<RecurringExpense>> GetRecurringExpensesAsync();
    Task SetCriticalAsync(int recurringExpenseId, bool isCritical);
    Task<List<Notification>> GetNotificationsAsync();
    Task<DateOnly> GetDemoDateAsync();
    Task<ClockAdvanceResponse> AdvanceDemoDateAsync(DateOnly date);
    Task<List<Lookup>> GetTransactionTypesAsync();
    Task<List<Condition>> GetConditionsAsync();
    Task<List<NotificationChannel>> GetNotificationChannelsAsync();
    Task<List<ConditionSetting>> GetConditionSettingsAsync();
    Task<ConditionSetting> UpdateConditionAsync(int conditionId, bool enabled, int channelId);
}

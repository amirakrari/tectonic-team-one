using System.Net.Http.Json;
using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

public class ApiClient(HttpClient http) : IApiClient
{
    public async Task<List<Transaction>> GetTransactionsAsync() =>
        await http.GetFromJsonAsync<List<Transaction>>("api/transactions") ?? [];

    public async Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        var response = await http.PostAsJsonAsync("api/transactions", transaction);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Transaction>())!;
    }

    public async Task<List<RecurringExpense>> GetRecurringExpensesAsync() =>
        await http.GetFromJsonAsync<List<RecurringExpense>>("api/recurring-expenses") ?? [];

    public async Task SetCriticalAsync(int recurringExpenseId, bool isCritical)
    {
        var response = await http.PatchAsJsonAsync($"api/recurring-expenses/{recurringExpenseId}", new { isCritical });
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<Notification>> GetNotificationsAsync() =>
        await http.GetFromJsonAsync<List<Notification>>("api/notifications") ?? [];
}

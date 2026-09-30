using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

public class ApiClient(HttpClient http, UiState ui) : IApiClient
{
    public bool IsMock => false;

    public Task<AuthSession> LoginAsync(string email, string password) =>
        AuthenticateAsync("login", email.Trim(), email.Trim(), password);

    public Task<AuthSession> SignupAsync(string name, string email, string password) =>
        AuthenticateAsync("signup", name.Trim(), email.Trim(), password);

    private async Task<AuthSession> AuthenticateAsync(string route, string name, string email, string password)
    {
        var token = await SendAsync<TokenResponse>(HttpMethod.Post, $"api/auth/{route}",
            new { email, password }, authenticated: false);
        return new(name, email, token.AccessToken, token.ExpiresAt);
    }

    public async Task<List<Transaction>> GetTransactionsAsync() =>
        (await SendAsync<List<TransactionRecord>>(HttpMethod.Get, "api/transactions")).Select(ToTransaction).ToList();

    public async Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        var response = await SendAsync<TransactionResponse>(HttpMethod.Post, "api/transactions", new
        {
            type = transaction.Type.ToString().ToLowerInvariant(),
            counterpartyKey = transaction.CounterpartyKey,
            counterpartyName = transaction.Company,
            transactionKey = transaction.TransactionKey,
            description = transaction.Description,
            amount = transaction.Amount,
            currency = transaction.Currency,
            date = DateOnly.FromDateTime(transaction.Date)
        });
        return ToTransaction(response.Transaction);
    }

    public async Task<List<RecurringExpense>> GetRecurringExpensesAsync() =>
        (await SendAsync<List<RecurringTransactionRecord>>(HttpMethod.Get, "api/recurring-transactions"))
        .Select(r => new RecurringExpense
        {
            Id = r.Id, Type = r.Type, Company = r.CounterpartyName, TransactionKey = r.TransactionKey,
            Amount = r.LastAmount, DayOfMonth = r.DayOfMonth,
            AddedAt = r.LastPaymentDate.ToDateTime(TimeOnly.MinValue),
            NextExpectedDate = r.NextExpectedDate.ToDateTime(TimeOnly.MinValue)
        }).ToList();

    public Task SetCriticalAsync(int recurringExpenseId, bool isCritical) =>
        throw new NotSupportedException("Critical payment flags are available only in mock mode.");

    public async Task<List<Notification>> GetNotificationsAsync() =>
        (await SendAsync<List<NotificationRecord>>(HttpMethod.Get, "api/notifications")).Select(n => new Notification
        {
            Id = n.Id, Message = n.Body, CreatedAt = n.CreatedAt, DeliveryStatus = n.DeliveryStatus,
            Type = n.Kind switch
            {
                "price-increased" => NotificationType.PriceIncrease,
                "recurring-added" => NotificationType.RecurringAdded,
                "recurring-missing" => NotificationType.RecurringRemoved,
                _ => throw new InvalidOperationException("Unknown API notification condition.")
            }
        }).ToList();

    public async Task<DateOnly> GetDemoDateAsync() =>
        (await SendAsync<ClockDate>(HttpMethod.Get, "api/demo/date")).Date;

    public Task<ClockAdvanceResponse> AdvanceDemoDateAsync(DateOnly date) =>
        SendAsync<ClockAdvanceResponse>(HttpMethod.Put, "api/demo/date", new ClockDate(date));

    public Task<List<Lookup>> GetTransactionTypesAsync() =>
        SendAsync<List<Lookup>>(HttpMethod.Get, "api/transaction-types");

    public Task<List<Condition>> GetConditionsAsync() =>
        SendAsync<List<Condition>>(HttpMethod.Get, "api/conditions");

    public Task<List<NotificationChannel>> GetNotificationChannelsAsync() =>
        SendAsync<List<NotificationChannel>>(HttpMethod.Get, "api/notification-channels");

    public Task<List<ConditionSetting>> GetConditionSettingsAsync() =>
        SendAsync<List<ConditionSetting>>(HttpMethod.Get, "api/me/conditions");

    public Task<ConditionSetting> UpdateConditionAsync(int conditionId, bool enabled, int channelId) =>
        SendAsync<ConditionSetting>(HttpMethod.Put, $"api/me/conditions/{conditionId}",
            new { isEnabled = enabled, channelId });

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, bool authenticated = true)
    {
        using var request = new HttpRequestMessage(method, path);
        if (authenticated)
        {
            if (!ui.SignedIn || ui.Session is not { IsMock: false } session)
            {
                ui.SignOut();
                throw new HttpRequestException("Please log in again.");
            }
            // Circuit state belongs on this request, never on a pooled HTTP handler.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (authenticated) ui.SignOut();
                throw new HttpRequestException("Invalid or expired login. Please log in again.",
                    null, response.StatusCode);
            }
            string message;
            if (response.StatusCode == HttpStatusCode.BadRequest && !authenticated)
                message = string.Join(" ", (await response.Content.ReadFromJsonAsync<AuthErrors>())!.Errors
                    .Select(e => e.Description));
            else
                message = (await response.Content.ReadFromJsonAsync<ApiError>())?.Error
                    ?? $"API request failed ({(int)response.StatusCode}).";
            throw new HttpRequestException(message, null, response.StatusCode);
        }
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static Transaction ToTransaction(TransactionRecord t) => new()
    {
        Id = t.Id, Type = t.Type, Company = t.CounterpartyName, CounterpartyKey = t.CounterpartyKey,
        TransactionKey = t.TransactionKey, Description = t.Description, Currency = t.Currency,
        Amount = t.Amount, Date = t.Date.ToDateTime(TimeOnly.MinValue)
    };
}

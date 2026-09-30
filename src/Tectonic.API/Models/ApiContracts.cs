using ExpenseWatch.Api.Models.Domain;

namespace ExpenseWatch.Api.Models;

public sealed record TransactionInput(
    string Type, string CounterpartyKey, string CounterpartyName, string TransactionKey,
    string Description, decimal Amount, string Currency, DateOnly Date);

public sealed record TransactionRecord(
    int Id, string Type, string CounterpartyKey, string CounterpartyName, string TransactionKey,
    string Description, decimal Amount, string Currency, DateOnly Date)
{
    public static TransactionRecord From(Transaction item, string type) =>
        new(item.Id, type, item.CounterpartyKey, item.CounterpartyName, item.TransactionKey,
            item.Description, item.Amount, item.Currency, item.Date);
}

public sealed record TransactionResponse(TransactionRecord Transaction, NotificationResponse[] Notifications);
public sealed record ClockDate(DateOnly Date);
public sealed record ClockAdvanceResponse(DateOnly Date, NotificationResponse[] Notifications);
public sealed record ApiError(string Error);
public sealed record ConditionSettingInput(bool IsEnabled, int ChannelId);
public sealed record LookupResponse(int Id, string Code, string Name);
public sealed record ChannelResponse(int Id, string Code, string Name, bool IsSupported);
public sealed record ConditionResponse(int Id, string Code, string Name, int[] TransactionTypeIds);
public sealed record ConditionSettingResponse(int ConditionId, bool IsEnabled, int ChannelId);

public sealed record RecurringTransactionResponse(
    int Id, string Type, string CounterpartyKey, string CounterpartyName, string TransactionKey,
    string Currency, int DayOfMonth, DateOnly LastPaymentDate, decimal LastAmount,
    DateOnly NextExpectedDate, bool IsActive)
{
    public static RecurringTransactionResponse From(RecurringTransaction item, Transaction last, string type)
    {
        var month = new DateOnly(last.Date.Year, last.Date.Month, 1).AddMonths(1);
        var expected = new DateOnly(month.Year, month.Month,
            Math.Min(item.DayOfMonth, DateTime.DaysInMonth(month.Year, month.Month)));
        return new(item.Id, type, item.CounterpartyKey, last.CounterpartyName, item.TransactionKey,
            item.Currency, item.DayOfMonth, last.Date, last.Amount, expected, item.IsActive);
    }
}

public sealed record NotificationResponse(
    int Id, string Kind, string Channel, int? TransactionId, int? RecurringTransactionId,
    string? MissingMonth, string Subject, string Body, string RecipientAddress,
    DateOnly OccurredOn, DateTime CreatedAt, string DeliveryStatus)
{
    public static NotificationResponse From(Notification item, string kind, string channel) =>
        new(item.Id, kind, channel, item.TransactionId, item.RecurringTransactionId,
            item.MissingMonth, item.Subject, item.Body, item.RecipientAddress,
            item.OccurredOn, item.CreatedAt, item.DeliveryStatus);
}

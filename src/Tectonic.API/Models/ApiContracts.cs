namespace ExpenseWatch.Api.Models;

public sealed record TransactionInput(
    string Type, string CompanyId, string CompanyName, string? ExpenseKey,
    string Description, decimal Amount, string Currency, DateOnly Date);

public sealed record TransactionResponse(Transaction Transaction, NotificationResponse[] Notifications);
public sealed record ClockDate(DateOnly Date);
public sealed record ClockAdvanceResponse(DateOnly Date, NotificationResponse[] Notifications);
public sealed record ClockError(string Error);

public sealed record RecurringExpenseResponse(
    int Id, string CompanyId, string ExpenseKey, int DayOfMonth,
    DateOnly LastPaymentDate, decimal LastAmount, DateOnly NextExpectedDate, bool Active)
{
    public static RecurringExpenseResponse From(RecurringExpense item) =>
        new(item.Id, item.CompanyId, item.ExpenseKey, item.DayOfMonth,
            item.LastPaymentDate, item.LastAmount, item.NextExpectedDate, item.Active);
}

public sealed record NotificationResponse(
    int Id, string Kind, string CompanyId, string ExpenseKey, int? TransactionId,
    string? MissingMonth, string Subject, string Body, DateOnly CreatedOn, string DeliveryStatus)
{
    public static NotificationResponse From(Notification item) =>
        new(item.Id, item.Kind, item.CompanyId, item.ExpenseKey, item.TransactionId,
            item.MissingMonth, item.Subject, item.Body, item.CreatedOn, item.DeliveryStatus);
}

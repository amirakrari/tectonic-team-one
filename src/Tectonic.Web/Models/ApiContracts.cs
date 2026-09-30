namespace Tectonic.Web.Models;

public record AuthSession(string Name, string Email, string AccessToken, DateTimeOffset ExpiresAt, bool IsMock = false);
public record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);
public record ApiError(string Error);
public record AuthValidationError(string Code, string Description);
public record AuthErrors(AuthValidationError[] Errors);
public record TransactionRecord(int Id, TransactionType Type, string CounterpartyKey, string CounterpartyName,
    string TransactionKey, string Description, decimal Amount, string Currency, DateOnly Date);
public record TransactionResponse(TransactionRecord Transaction, NotificationRecord[] Notifications);
public record RecurringTransactionRecord(int Id, TransactionType Type, string CounterpartyKey,
    string CounterpartyName, string TransactionKey, string Currency, int DayOfMonth,
    DateOnly LastPaymentDate, decimal LastAmount, DateOnly NextExpectedDate, bool IsActive);
public record NotificationRecord(int Id, string Kind, string Channel, int? TransactionId,
    int? RecurringTransactionId, string? MissingMonth, string Subject, string Body,
    string RecipientAddress, DateOnly OccurredOn, DateTime CreatedAt, string DeliveryStatus);
public record ClockDate(DateOnly Date);
public record ClockAdvanceResponse(DateOnly Date, NotificationRecord[] Notifications);
public record Lookup(int Id, string Code, string Name);
public record Condition(int Id, string Code, string Name, int[] TransactionTypeIds);
public record NotificationChannel(int Id, string Code, string Name, bool IsSupported);
public record ConditionSetting(int ConditionId, bool IsEnabled, int ChannelId);

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Models.Domain;

[Index(nameof(UserId), nameof(CreatedAt))]
public sealed class Notification
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public IdentityUser? User { get; set; }
    public int ConditionId { get; set; }
    public Condition? Condition { get; set; }
    public int ChannelId { get; set; }
    public NotificationChannel? Channel { get; set; }
    public int? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public int? RecurringTransactionId { get; set; }
    public RecurringTransaction? RecurringTransaction { get; set; }
    public string? MissingMonth { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public required string RecipientAddress { get; set; }
    public DateOnly OccurredOn { get; set; }
    /// <summary>The actual UTC creation time, independent of the logical demo date.</summary>
    public DateTime CreatedAt { get; set; }
    public string DeliveryStatus { get; set; } = "pending";
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Models.Domain;

[Index(nameof(UserId), nameof(TransactionTypeId), nameof(CounterpartyKey),
    nameof(TransactionKey), nameof(Currency), IsUnique = true)]
public sealed class RecurringTransaction
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public IdentityUser? User { get; set; }
    public int TransactionTypeId { get; set; }
    public TransactionType? TransactionType { get; set; }
    public required string CounterpartyKey { get; set; }
    public required string TransactionKey { get; set; }
    public required string Currency { get; set; }
    public int DayOfMonth { get; set; }
    public int LastTransactionId { get; set; }
    public Transaction? LastTransaction { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? RemovedOn { get; set; }
}

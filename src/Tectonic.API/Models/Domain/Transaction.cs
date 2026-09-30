using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Models.Domain;

[Index(nameof(UserId), nameof(TransactionTypeId), nameof(CounterpartyKey),
    nameof(TransactionKey), nameof(Currency), nameof(Date))]
public sealed class Transaction
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public IdentityUser? User { get; set; }
    public int TransactionTypeId { get; set; }
    public TransactionType? TransactionType { get; set; }
    public required string CounterpartyKey { get; set; }
    public required string CounterpartyName { get; set; }
    public required string TransactionKey { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public DateOnly Date { get; set; }
}

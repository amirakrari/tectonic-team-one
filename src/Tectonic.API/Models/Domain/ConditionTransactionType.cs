using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Models.Domain;

[PrimaryKey(nameof(ConditionId), nameof(TransactionTypeId))]
public sealed class ConditionTransactionType
{
    public int ConditionId { get; set; }
    public Condition? Condition { get; set; }
    public int TransactionTypeId { get; set; }
    public TransactionType? TransactionType { get; set; }
}

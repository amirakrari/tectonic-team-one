using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Models.Domain;

[Index(nameof(Code), IsUnique = true)]
public sealed class Condition
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public ICollection<ConditionTransactionType> TransactionTypes { get; set; } = [];
}

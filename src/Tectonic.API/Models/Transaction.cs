namespace ExpenseWatch.Api.Models;

public sealed class Transaction
{
    public int Id { get; set; }
    public required string Type { get; set; }
    public required string CompanyId { get; set; }
    public required string CompanyName { get; set; }
    public string? ExpenseKey { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public DateOnly Date { get; set; }
}

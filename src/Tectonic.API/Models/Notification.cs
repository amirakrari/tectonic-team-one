namespace ExpenseWatch.Api.Models;

public sealed class Notification
{
    public int Id { get; set; }
    public required string Kind { get; set; }
    public required string CompanyId { get; set; }
    public required string ExpenseKey { get; set; }
    public int? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public string? MissingMonth { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public DateOnly CreatedOn { get; set; }
    public string DeliveryStatus { get; set; } = "pending";
}

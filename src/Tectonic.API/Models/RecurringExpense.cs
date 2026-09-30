namespace ExpenseWatch.Api.Models;

public sealed class RecurringExpense
{
    public int Id { get; set; }
    public required string CompanyId { get; set; }
    public required string ExpenseKey { get; set; }
    public int DayOfMonth { get; set; }
    public DateOnly LastPaymentDate { get; set; }
    public decimal LastAmount { get; set; }
    public DateOnly NextExpectedDate { get; set; }
    public bool Active { get; set; }
    public DateOnly? RemovedOn { get; set; }
}

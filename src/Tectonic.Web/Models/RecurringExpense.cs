namespace Tectonic.Web.Models;

public class RecurringExpense
{
    public int Id { get; set; }
    public string Company { get; set; } = "";
    public decimal Amount { get; set; }
    public int DayOfMonth { get; set; }
    public DateTime AddedAt { get; set; }
}

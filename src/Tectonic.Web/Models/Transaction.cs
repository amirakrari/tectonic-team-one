using System.Text.Json.Serialization;

namespace Tectonic.Web.Models;

[JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
public enum TransactionType { Expense, Income }

public class Transaction
{
    public int Id { get; set; }
    public TransactionType Type { get; set; }
    public string Company { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

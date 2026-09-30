using System.Text.Json.Serialization;

namespace Tectonic.Web.Models;

[JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
public enum TransactionType { Expense, Income }

public class Transaction
{
    public int Id { get; set; }
    public TransactionType Type { get; set; }
    public string Company { get; set; } = "";
    public string CounterpartyKey { get; set; } = "";
    public string TransactionKey { get; set; } = "";
    public string Description { get; set; } = "";
    public string Currency { get; set; } = "EUR";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

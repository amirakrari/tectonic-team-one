using System.Text.Json.Serialization;

namespace Tectonic.Web.Models;

[JsonConverter(typeof(JsonStringEnumConverter<NotificationType>))]
public enum NotificationType { PriceIncrease, RecurringAdded, RecurringRemoved }

public class Notification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Company { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

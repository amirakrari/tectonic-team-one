using System.Text.Json.Serialization;

namespace Tectonic.Web.Models;

[JsonConverter(typeof(JsonStringEnumConverter<NotificationType>))]
public enum NotificationType
{
    PriceIncrease,
    PriceDecrease,
    RecurringAdded,
    RecurringRemoved,
    PaymentIncomplete,
    DuplicateCharge,
    UpcomingPayment,
}

[JsonConverter(typeof(JsonStringEnumConverter<NotificationDelivery>))]
public enum NotificationDelivery { Instant, Digest }

public class Notification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Company { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    // Digest = held back for the daily summary email (regular alerts when "Daily summary" is on).
    public NotificationDelivery Delivery { get; set; }
}

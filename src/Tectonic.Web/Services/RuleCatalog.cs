using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

// Name and description come from Texts ("rule.{Id}" and "rule.{Id}.desc"). Icon is a KbcIcon name.
// AlwaysInstant rules skip the critical-only filter and the daily summary (possible fraud or money due).
public record RuleInfo(string Id, NotificationType Type, string Icon, bool EnabledByDefault, bool AlwaysInstant = false);

// Single source for every rule shown in the UI (Rules, Settings, Notifications, email preview).
public static class RuleCatalog
{
    public static readonly IReadOnlyList<RuleInfo> All =
    [
        new("price-increase", NotificationType.PriceIncrease, "trending-up", true),
        new("recurring-added", NotificationType.RecurringAdded, "repeat", true),
        new("recurring-removed", NotificationType.RecurringRemoved, "calendar-x", true),
        new("price-decrease", NotificationType.PriceDecrease, "trending-down", true),
        new("payment-incomplete", NotificationType.PaymentIncomplete, "hourglass", true),
        new("duplicate-charge", NotificationType.DuplicateCharge, "copy", true, AlwaysInstant: true),
        new("upcoming-payment", NotificationType.UpcomingPayment, "calendar-clock", true, AlwaysInstant: true),
    ];

    public static RuleInfo For(NotificationType type) => All.First(r => r.Type == type);
}

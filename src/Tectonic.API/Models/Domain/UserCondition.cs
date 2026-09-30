using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Models.Domain;

[PrimaryKey(nameof(UserId), nameof(ConditionId))]
public sealed class UserCondition
{
    public required string UserId { get; set; }
    public IdentityUser? User { get; set; }
    public int ConditionId { get; set; }
    public Condition? Condition { get; set; }
    public bool IsEnabled { get; set; }
    public int ChannelId { get; set; }
    public NotificationChannel? Channel { get; set; }
}

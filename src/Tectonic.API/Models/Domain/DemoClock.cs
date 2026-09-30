using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Models.Domain;

[PrimaryKey(nameof(UserId))]
public sealed class DemoClock
{
    public required string UserId { get; set; }
    public IdentityUser? User { get; set; }
    public DateOnly Date { get; set; }
}

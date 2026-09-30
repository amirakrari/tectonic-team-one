using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Models.Domain;

[Index(nameof(Code), IsUnique = true)]
public sealed class NotificationChannel
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsSupported { get; set; }
}

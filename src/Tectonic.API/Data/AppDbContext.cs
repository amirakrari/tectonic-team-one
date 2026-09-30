using ExpenseWatch.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DemoClock> DemoClocks => Set<DemoClock>();
}

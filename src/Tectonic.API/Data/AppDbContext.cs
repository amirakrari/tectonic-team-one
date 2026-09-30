using ExpenseWatch.Api.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DemoClock> DemoClocks => Set<DemoClock>();
    public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();
    public DbSet<Condition> Conditions => Set<Condition>();
    public DbSet<ConditionTransactionType> ConditionTransactionTypes => Set<ConditionTransactionType>();
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();
    public DbSet<UserCondition> UserConditions => Set<UserCondition>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<TransactionType>().Property(t => t.Id).ValueGeneratedNever();
        model.Entity<Condition>().Property(c => c.Id).ValueGeneratedNever();
        model.Entity<NotificationChannel>().Property(c => c.Id).ValueGeneratedNever();
        model.Entity<RecurringTransaction>().HasOne(r => r.LastTransaction).WithMany()
            .HasForeignKey(r => r.LastTransactionId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Notification>().HasOne(n => n.Transaction).WithMany()
            .HasForeignKey(n => n.TransactionId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Notification>().HasOne(n => n.RecurringTransaction).WithMany()
            .HasForeignKey(n => n.RecurringTransactionId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TransactionType>().HasData(
            new TransactionType { Id = 1, Code = "expense", Name = "Expense" },
            new TransactionType { Id = 2, Code = "income", Name = "Income" });
        model.Entity<Condition>().HasData(
            new Condition { Id = 1, Code = "price-increased", Name = "Price increased" },
            new Condition { Id = 2, Code = "recurring-added", Name = "Recurring transaction added" },
            new Condition { Id = 3, Code = "recurring-missing", Name = "Recurring transaction missing" });
        model.Entity<ConditionTransactionType>().HasData(
            new ConditionTransactionType { ConditionId = 1, TransactionTypeId = 1 },
            new ConditionTransactionType { ConditionId = 2, TransactionTypeId = 1 },
            new ConditionTransactionType { ConditionId = 2, TransactionTypeId = 2 },
            new ConditionTransactionType { ConditionId = 3, TransactionTypeId = 1 },
            new ConditionTransactionType { ConditionId = 3, TransactionTypeId = 2 });
        model.Entity<NotificationChannel>().HasData(
            new NotificationChannel { Id = 1, Code = "email", Name = "Email", IsSupported = true },
            new NotificationChannel { Id = 2, Code = "sms", Name = "SMS", IsSupported = false },
            new NotificationChannel { Id = 3, Code = "in-app", Name = "In-app", IsSupported = false });
    }
}

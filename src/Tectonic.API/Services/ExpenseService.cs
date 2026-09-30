using System.Globalization;
using ExpenseWatch.Api.Data;
using ExpenseWatch.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Services;

public sealed class ExpenseService(AppDbContext db, EmailSender email)
{
    public async Task<TransactionResponse> IngestAsync(TransactionInput input)
    {
        var clock = await db.DemoClocks.SingleAsync(c => c.Id == 1);
        var transaction = new Transaction
        {
            Type = input.Type,
            CompanyId = input.CompanyId,
            CompanyName = input.CompanyName,
            ExpenseKey = input.ExpenseKey,
            Description = input.Description,
            Amount = input.Amount,
            Currency = input.Currency,
            Date = input.Date
        };
        var generated = new List<Notification>();
        db.Transactions.Add(transaction);

        if (transaction.Type == "expense")
        {
            var previous = await db.Transactions
                .Where(t => t.Type == "expense" && t.CompanyId == input.CompanyId
                    && t.ExpenseKey == input.ExpenseKey && t.Currency == input.Currency
                    && t.Date < input.Date)
                .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
                .FirstOrDefaultAsync();
            if (previous is not null && input.Amount > previous.Amount)
            {
                generated.Add(NewNotification("price-increased", transaction, clock.Date,
                    $"Price increased at {input.CompanyName}",
                    $"{input.CompanyName}: {input.Description} increased from "
                    + $"{input.Currency} {Money(previous.Amount)} to {input.Currency} {Money(input.Amount)}"
                    + $" (increase: {input.Currency} {Money(input.Amount - previous.Amount)})."));
            }

            var recurring = await db.RecurringExpenses.SingleOrDefaultAsync(r =>
                r.CompanyId == input.CompanyId && r.ExpenseKey == input.ExpenseKey);
            if (recurring is { Active: true })
            {
                recurring.LastPaymentDate = input.Date;
                recurring.LastAmount = input.Amount;
                recurring.NextExpectedDate = NextExpected(input.Date, recurring.DayOfMonth);
            }
            else if (previous is not null
                && Month(previous.Date).AddMonths(1) == Month(input.Date)
                && previous.Date.Day == input.Date.Day
                && (recurring?.RemovedOn is null
                    || (previous.Date >= recurring.RemovedOn.Value
                        && input.Date >= recurring.RemovedOn.Value)))
            {
                if (recurring is null)
                {
                    recurring = new RecurringExpense
                    {
                        CompanyId = input.CompanyId,
                        ExpenseKey = input.ExpenseKey!
                    };
                    db.RecurringExpenses.Add(recurring);
                }
                recurring.Active = true;
                recurring.RemovedOn = null;
                recurring.DayOfMonth = input.Date.Day;
                recurring.LastPaymentDate = input.Date;
                recurring.LastAmount = input.Amount;
                recurring.NextExpectedDate = NextExpected(input.Date, recurring.DayOfMonth);
                generated.Add(NewNotification("recurring-added", transaction, clock.Date,
                    $"Recurring expense added at {input.CompanyName}",
                    $"{input.CompanyName}: {input.Description} ({input.Currency} {Money(input.Amount)})"
                    + " was added to your recurring expenses."));
            }
        }

        db.Notifications.AddRange(generated);
        await db.SaveChangesAsync();
        await DeliverAsync(generated);
        return new TransactionResponse(transaction, generated.Select(NotificationResponse.From).ToArray());
    }

    public async Task<ClockAdvanceResponse> AdvanceAsync(DemoClock clock, DateOnly date)
    {
        var generated = new List<Notification>();
        var active = await db.RecurringExpenses.Where(r => r.Active).OrderBy(r => r.Id).ToListAsync();
        foreach (var recurring in active)
        {
            var expectedMonth = Month(recurring.LastPaymentDate).AddMonths(1);
            while (expectedMonth.AddMonths(1) <= date)
            {
                var end = expectedMonth.AddMonths(1);
                var payment = await db.Transactions
                    .Where(t => t.Type == "expense" && t.CompanyId == recurring.CompanyId
                        && t.ExpenseKey == recurring.ExpenseKey && t.Date >= expectedMonth && t.Date < end)
                    .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
                    .FirstOrDefaultAsync();
                if (payment is not null)
                {
                    recurring.LastPaymentDate = payment.Date;
                    recurring.LastAmount = payment.Amount;
                    recurring.NextExpectedDate = NextExpected(payment.Date, recurring.DayOfMonth);
                    expectedMonth = Month(payment.Date).AddMonths(1);
                    continue;
                }

                var last = await db.Transactions
                    .Where(t => t.Type == "expense" && t.CompanyId == recurring.CompanyId
                        && t.ExpenseKey == recurring.ExpenseKey)
                    .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).FirstAsync();
                recurring.Active = false;
                recurring.RemovedOn = date;
                generated.Add(new Notification
                {
                    Kind = "recurring-removed",
                    CompanyId = recurring.CompanyId,
                    ExpenseKey = recurring.ExpenseKey,
                    MissingMonth = expectedMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    Subject = $"Missing recurring expense at {last.CompanyName}",
                    Body = $"No {last.CompanyName}: {last.Description} payment "
                        + $"({last.Currency} {Money(recurring.LastAmount)}) was recorded for "
                        + $"{expectedMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture)}."
                        + " It was removed from your recurring expenses.",
                    CreatedOn = date
                });
                break;
            }
        }
        clock.Date = date;
        db.Notifications.AddRange(generated);
        await db.SaveChangesAsync();
        await DeliverAsync(generated);
        return new ClockAdvanceResponse(date, generated.Select(NotificationResponse.From).ToArray());
    }

    private async Task DeliverAsync(List<Notification> generated)
    {
        foreach (var notification in generated)
        {
            notification.DeliveryStatus = email.Send(notification) ? "sent" : "failed";
            await db.SaveChangesAsync();
        }
    }

    private static Notification NewNotification(
        string kind, Transaction transaction, DateOnly date, string subject, string body) =>
        new()
        {
            Kind = kind,
            CompanyId = transaction.CompanyId,
            ExpenseKey = transaction.ExpenseKey!,
            Transaction = transaction,
            Subject = subject,
            Body = body,
            CreatedOn = date
        };

    private static DateOnly Month(DateOnly date) => new(date.Year, date.Month, 1);

    private static DateOnly NextExpected(DateOnly payment, int originalDay)
    {
        var month = Month(payment).AddMonths(1);
        return new DateOnly(month.Year, month.Month,
            Math.Min(originalDay, DateTime.DaysInMonth(month.Year, month.Month)));
    }

    private static string Money(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);
}

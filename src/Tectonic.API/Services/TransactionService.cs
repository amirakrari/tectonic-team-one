using System.Globalization;
using ExpenseWatch.Api.Data;
using ExpenseWatch.Api.Models;
using ExpenseWatch.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Services;

public sealed class TransactionService(AppDbContext db, EmailSender email, IConfiguration configuration)
{
    public async Task<TransactionResponse> IngestAsync(
        string userId, TransactionInput input, TransactionType type, DemoClock clock)
    {
        var transaction = new Transaction
        {
            UserId = userId, TransactionTypeId = type.Id, TransactionType = type,
            CounterpartyKey = input.CounterpartyKey, CounterpartyName = input.CounterpartyName,
            TransactionKey = input.TransactionKey, Description = input.Description,
            Amount = input.Amount, Currency = input.Currency, Date = input.Date
        };
        var previous = await db.Transactions.Where(t => t.UserId == userId
            && t.TransactionTypeId == type.Id && t.CounterpartyKey == input.CounterpartyKey
            && t.TransactionKey == input.TransactionKey && t.Currency == input.Currency
            && t.Date < input.Date).OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync();
        var recurring = await db.RecurringTransactions.SingleOrDefaultAsync(r => r.UserId == userId
            && r.TransactionTypeId == type.Id && r.CounterpartyKey == input.CounterpartyKey
            && r.TransactionKey == input.TransactionKey && r.Currency == input.Currency);
        var generated = new List<Notification>();
        db.Transactions.Add(transaction);
        if (type.Code == "expense" && previous is not null && input.Amount > previous.Amount)
        {
            await AddNotificationAsync(generated, "price-increased", type.Id, userId, clock.Date,
                $"Price increased at {input.CounterpartyName}",
                $"{input.CounterpartyName}: {input.Description} increased from EUR {Money(previous.Amount)}"
                + $" to EUR {Money(input.Amount)} (increase: EUR {Money(input.Amount - previous.Amount)}).",
                transaction);
        }
        if (recurring is { IsActive: true })
        {
            recurring.LastTransaction = transaction;
        }
        else if (previous is not null
            && Month(previous.Date).AddMonths(1) == Month(input.Date)
            && previous.Date.Day == input.Date.Day
            && (recurring?.RemovedOn is null
                || (previous.Date >= recurring.RemovedOn.Value && input.Date >= recurring.RemovedOn.Value)))
        {
            if (recurring is null)
            {
                recurring = new RecurringTransaction
                {
                    UserId = userId, TransactionTypeId = type.Id, CounterpartyKey = input.CounterpartyKey,
                    TransactionKey = input.TransactionKey, Currency = input.Currency
                };
                db.RecurringTransactions.Add(recurring);
            }
            recurring.LastTransaction = transaction;
            recurring.DayOfMonth = input.Date.Day;
            recurring.IsActive = true;
            recurring.RemovedOn = null;
            await AddNotificationAsync(generated, "recurring-added", type.Id, userId, clock.Date,
                $"Recurring {type.Code} added at {input.CounterpartyName}",
                $"{input.CounterpartyName}: {input.Description} (EUR {Money(input.Amount)})"
                + $" was added to your recurring {type.Code} transactions.",
                transaction, recurring);
        }
        db.Notifications.AddRange(generated);
        await db.SaveChangesAsync();
        await DeliverAsync(generated);
        return new TransactionResponse(TransactionRecord.From(transaction, type.Code),
            await NotificationResponsesAsync(generated));
    }

    public async Task<ClockAdvanceResponse> AdvanceAsync(string userId, DemoClock clock, DateOnly date)
    {
        var generated = new List<Notification>();
        var active = await db.RecurringTransactions.Where(r => r.UserId == userId && r.IsActive)
            .OrderBy(r => r.Id).ToListAsync();
        var types = await db.TransactionTypes.ToDictionaryAsync(t => t.Id, t => t.Code);
        foreach (var recurring in active)
        {
            var last = await db.Transactions.SingleAsync(t => t.Id == recurring.LastTransactionId
                && t.UserId == userId);
            var expected = Month(last.Date).AddMonths(1);
            while (expected.AddMonths(1) <= date)
            {
                var end = expected.AddMonths(1);
                var payment = await db.Transactions.Where(t => t.UserId == userId
                    && t.TransactionTypeId == recurring.TransactionTypeId
                    && t.CounterpartyKey == recurring.CounterpartyKey
                    && t.TransactionKey == recurring.TransactionKey && t.Currency == recurring.Currency
                    && t.Date >= expected && t.Date < end)
                    .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).FirstOrDefaultAsync();
                if (payment is not null)
                {
                    recurring.LastTransaction = payment;
                    last = payment;
                    expected = Month(last.Date).AddMonths(1);
                    continue;
                }
                recurring.IsActive = false;
                recurring.RemovedOn = date;
                var type = types[recurring.TransactionTypeId];
                await AddNotificationAsync(generated, "recurring-missing", recurring.TransactionTypeId,
                    userId, date, $"Missing recurring {type} at {last.CounterpartyName}",
                    $"No {last.CounterpartyName}: {last.Description} {type}"
                    + $" (EUR {Money(last.Amount)}) was recorded for "
                    + $"{expected.ToString("MMMM yyyy", CultureInfo.InvariantCulture)}."
                    + " It was removed from your recurring transactions.",
                    recurring: recurring, missingMonth: expected.ToString("yyyy-MM", CultureInfo.InvariantCulture));
                break;
            }
        }
        clock.Date = date;
        db.Notifications.AddRange(generated);
        await db.SaveChangesAsync();
        await DeliverAsync(generated);
        return new ClockAdvanceResponse(date, await NotificationResponsesAsync(generated));
    }

    private async Task AddNotificationAsync(List<Notification> generated, string kind, int typeId,
        string userId, DateOnly date, string subject, string body, Transaction? transaction = null,
        RecurringTransaction? recurring = null, string? missingMonth = null)
    {
        var conditionId = await db.UserConditions
            .Where(s => s.UserId == userId && s.IsEnabled && s.ChannelId == 1)
            .Join(db.Conditions.Where(c => c.Code == kind), s => s.ConditionId, c => c.Id, (s, c) => c.Id)
            .Join(db.ConditionTransactionTypes.Where(c => c.TransactionTypeId == typeId),
                id => id, c => c.ConditionId, (id, c) => id).FirstOrDefaultAsync();
        if (conditionId == 0)
        {
            return;
        }
        generated.Add(new Notification
        {
            UserId = userId, ConditionId = conditionId, ChannelId = 1,
            Transaction = transaction, RecurringTransaction = recurring, MissingMonth = missingMonth,
            Subject = subject, Body = body, RecipientAddress = configuration["Email:Recipient"] ?? "",
            OccurredOn = date, CreatedAt = DateTime.UtcNow
        });
    }

    private async Task DeliverAsync(List<Notification> generated)
    {
        foreach (var notification in generated)
        {
            notification.DeliveryStatus = email.Send(notification) ? "sent" : "failed";
            await db.SaveChangesAsync();
        }
    }

    private async Task<NotificationResponse[]> NotificationResponsesAsync(List<Notification> generated)
    {
        var kinds = await db.Conditions.ToDictionaryAsync(c => c.Id, c => c.Code);
        return generated.Select(n => NotificationResponse.From(n, kinds[n.ConditionId], "email")).ToArray();
    }

    private static DateOnly Month(DateOnly date) => new(date.Year, date.Month, 1);
    private static string Money(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);
}

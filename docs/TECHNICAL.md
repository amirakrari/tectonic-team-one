# Architecture and domain model

[README](../README.md) | [API reference](API.md) | [Operations](OPERATIONS.md)

This guide describes the implemented projects, not the earlier proposed
shared-data contract. Source files remain the authority for exact behavior.

## Project boundaries

| Project | Responsibility |
|---|---|
| `src/Tectonic.API` | Minimal API, Identity/JWT, normalized persistence, rules, email and hosted simulator |
| `src/Tectonic.Web` | Blazor Interactive Server UI, MudBlazor components and real/mock API clients |

The API is one .NET 10 project with direct EF Core access. The webapp calls it
over HTTP; it does not access the SQLite database.

```text
Blazor pages -> IApiClient -> authenticated API routes
                                 |
                                 v
                         AppDbContext / SQLite
                                 |
                         TransactionService
                                 |
                    state + pending notification save
                                 |
                             EmailSender
                                 |
                         persisted delivery result

TransactionSimulator -> the same authenticated HTTP routes
```

There is no bank connector, generic rule language, queue, distributed scheduler,
separate simulator process or background notification-delivery worker.

## Identity and ownership

`Data/AppDbContext.cs` inherits `IdentityDbContext<IdentityUser>`. Standard
Identity user/role tables share the integrated database with domain tables.
Signup assigns `User`, creates preferences and a clock, and commits before
issuing a token. There is no second custom user/role entity.

JWT `sub` is the owner key. API reads, writes, stream matching, preference changes
and clock advancement are scoped to that key. Token validation also checks that
the account exists in this store. Old signed tokens do not make legacy-only users
members of the new database.

The `User` role is included in issued JWT claims; this is not a scoped
permissions/RBAC platform. No Admin management surface exists.

## Normalized domain

Entities live in `src/Tectonic.API/Models/Domain`.

| Entity | Key and important relationships |
|---|---|
| `TransactionType` | Fixed integer ID, unique code; expense/income |
| `Condition` | Fixed integer ID, unique code |
| `ConditionTransactionType` | Composite PK `(ConditionId, TransactionTypeId)`; FK to both lookups |
| `NotificationChannel` | Fixed ID/code and support flag |
| `UserCondition` | Composite PK `(UserId, ConditionId)`; selected channel and enabled flag |
| `Transaction` | User/type FKs; counterparty/stream identity, amount/currency/date |
| `RecurringTransaction` | User/type FKs; original day, last transaction, active/removal state |
| `Notification` | User/condition/channel FKs; optional source transaction/recurrence links |
| `DemoClock` | User ID is PK/FK; separate logical date per user |

```text
Identity user
  +-- UserConditions -> Conditions -> ConditionTransactionTypes -> TransactionTypes
  |       +-- NotificationChannels
  +-- Transactions -> TransactionTypes
  +-- RecurringTransactions -> LastTransaction
  +-- Notifications -> Condition / Channel / optional source records
  +-- DemoClock
```

Lookup seeds are fixed IDs, not user-editable registries. The five applicability
pairs are `(1,1)`, `(2,1)`, `(2,2)`, `(3,1)`, `(3,2)`.
No column stores a delimited or JSON list of applicable transaction types.

### Stream identity

```text
UserId + TransactionTypeId + CounterpartyKey + TransactionKey + Currency
```

The recurring stream index is unique. Transaction uniqueness adds `Date`, giving
one record per stream/day. This supports the sequential demo and restart skip.
It is not a general bank-ingestion replay or concurrent-correction protocol.

Counterparty is payee for expense and payer for income. Its display name is not
a matching identifier. `TransactionKey` distinguishes internet/mobile plans at
the same counterparty, or salary/bonus streams from the same employer.

The last payment amount/date and displayed next expected date are derived from
`LastTransactionId` and original `DayOfMonth`; they are not duplicate stored state.
`RemovedOn` fences rediscovery after an inactive period.

## Rule processing

`Services/TransactionService.cs` handles state changes. It matches earlier
history by the full owner/stream identity. Amount comparison happens after
materialization in C#, avoiding SQLite decimal ordering/comparison limitations.

1. **Price increase:** expense only, immediately previous earlier transaction;
   equal or lower amount does not alert.
2. **Recurring added:** two matching numeric days in consecutive calendar months,
   for either income or expense.
3. **Recurring missing:** earliest expected month must fully end before it can
   be declared unpaid. Deactivate once and name the earliest gap.

Active streams accept a payment anywhere in the expected month without changing
the original recurrence day. Expected dates clamp to a shorter month's final day.
Large clock jumps do not generate repeated missing alerts. Inactive streams need
a fresh qualifying pair on/after removal to reactivate.

Tracking is independent of notification preferences. An alert is generated only
when the condition/type join exists and the owner's condition is enabled on the
supported email channel.

## Persistence and delivery order

Transaction/clock changes, recurring state and pending notifications are saved
together before email. Linked source IDs are filled by EF. Each delivery outcome
is then saved separately.

`Services/EmailSender.cs` uses either the configured Mailpit HTTP send endpoint
or SMTP. The notification retains rendered subject/body and recipient address
as a historical snapshot. Recipient comes from presenter configuration, not
unverified account email.

Logical dates belong to the demo. Notification `CreatedAt` and JWT expiry use
actual UTC. Crashes can leave pending records; no outbox or automatic retry exists.

## Simulator lifecycle

`Services/TransactionSimulator.cs` is a singleton hosted `BackgroundService`.
It uses `IHttpClientFactory`, waits for `ApplicationStarted`, and consumes
`PeriodicTimer` sequentially. It never injects a scoped DbContext or bypasses
endpoint rules.

Login/signup yields a bearer token. Each tick reads persisted clock/history,
generates stable daily identities with ordinary seeded randomness, skips already
stored rows, awaits POSTs, and advances the date only after success.
Token renewal, HTTP failure and shutdown are explicit lifecycle boundaries.
See the [operations guide](OPERATIONS.md#simulator) for settings and recovery.

## Web integration

`Services/IApiClient.cs` abstracts real versus canned data:

- `ApiClient`: sends bearer-authorized HTTP requests and adapts final API records
  into UI models. Reads `/api/recurring-transactions`, not the historical route.
- `MockApiClient`: supplies canned data when mock mode is selected.

UI model names such as `RecurringExpense` are not EF entities or endpoint names.
Critical-payment flags are mock-only. The real backend does not implement a
critical-payment PATCH route; documentation must not invent one.

Because API calls originate from the Interactive Server web process, the configured
API URL must be reachable from that process/container. Direct browser API calls
can separately require the exact-origin CORS setting.

## Schema lifecycle and verification limits

The integrated store is `tectonic.db`. `EnsureCreated` initializes a new schema;
it does not migrate existing files. Legacy auth/expense files are not converted.
No schema reset, import or user deletion happens automatically.

Native API checks recorded on 2026-09-30 covered isolation, rules, preferences,
delivery outcomes and simulator progression/renewal/restart. This guide does not
claim automated test coverage, production security assurance, current web browser
QA or a container image check. Documentation-only verification is described in
the README.

# Expense Watch - complete hackathon project briefing

## Current API correction for recording

Use [README.md](README.md) as the current contract. The discussion below includes
historical shared-data architecture and is not the final implementation contract.
The integrated API uses all nine `Models/Domain` entities in one Identity-enabled
AppDbContext and `tectonic.db`. Transactions, recurring income/expenses, clocks,
notifications and enabled-condition preferences belong to individual users.
Condition/type applicability is a normalized join; only email is supported.

Use the newer recording API on port 5081, with `counterpartyKey`,
`counterpartyName`, `transactionKey` and `/api/recurring-transactions`, not the
old port 5080 contract. Advance the presenter clock before each transaction;
January 5 and February 5 must have identical stream keys. Use a separate presenter
account or disable the simulator. The simulator makes sequential authenticated
HTTP calls and advances only its dedicated user's clock. Final runtime evidence
is recorded in `dev/active/domain-simulator/context.md`, not inferred from the
historical claims below.

> **Audience:** Team members preparing the PowerPoint, pitch, and demonstration
> **Status:** Repository-grounded briefing; implemented features and proposals are distinguished below
> **Owner:** Tectonic Team One
> **Last Verified:** 2026-09-30, documentation and source inspection
> **Source Anchors:** [README](README.md), [project](src/Tectonic.API/Tectonic.API.csproj), [API host](src/Tectonic.API/Program.cs), [expense rules](src/Tectonic.API/Services/ExpenseService.cs)

This document brings together the product idea, technical implementation, domain
model, API contract, development history, demonstration, and presentation material.
It is self-contained so a teammate can prepare the slides without reading the code
or the private development notes.

## 1. Executive summary

**Expense Watch turns simulated financial transactions into understandable email
alerts about changing and recurring expenses.**

The problem is straightforward: people can overlook a subscription price increase,
forget which expenses recur, or lose track of a payment that no longer appears.
The prototype stores transaction history, compares expenses against earlier
payments, and explains the detected changes by email.

Three business behaviors are implemented:

1. Detect when an expense becomes more expensive.
2. Discover an expense that repeats monthly.
3. Remove an expense from the active recurring list when an expected month ends
   without a matching payment.

The backend is a single .NET 10 ASP.NET Core Minimal API project, with EF Core
SQLite persistence, Identity signup/login, JWT bearer access, SMTP email, and
interactive Swagger documentation.

**The demo uses invented data. It does not connect to KBC or another bank, move
money, or cancel subscriptions.** A missing-payment alert means a transaction
was not recorded, not that a subscription was confirmed cancelled.

The repository records successful builds and manual demonstrations, including
three emails captured in Mailpit. Those are historical verification records;
this briefing was checked against the source, without rerunning the application.

### A pitch the presenter can use

> "Expense Watch helps people notice changes in their regular expenses. Our
> prototype compares simulated transactions with payment history, identifies
> monthly expenses, and emails clear explanations when a price rises or an
> expected payment is missing. We built a working authenticated API with
> persistent storage and email delivery, and we can demonstrate several months
> of activity in minutes."

## 2. Product context and intended value

### The user problem

A transaction list contains individual payments, but recognizing a pattern
requires comparing them over time. A higher internet bill may be easy to miss.
A recurring charge may not be recognized until several payments have occurred.
A missing payment may go unnoticed without an explicit check.

Expense Watch adds simple explanations to that history:

| Situation | Information given to the user |
|---|---|
| Internet changes from EUR 45.00 to EUR 49.00 | The company, expense, old amount, new amount, and EUR 4.00 increase |
| Internet is paid on January 5 and February 5 | The expense has been added to the recurring list |
| No internet payment appears during March | After March ends, the expense is removed from the active recurring list and March is named in an alert |

Potential users are people who want visibility into regular bills and
subscriptions. This is the product motivation, not evidence of market research,
customer adoption, measured savings, or validated demand.

### What the hackathon demonstrates

The prototype demonstrates the path from transaction input to stored history,
rule evaluation, persisted notification, and actual email transport.

It stores both incomes and expenses, but the current alerts apply only to
expenses. Income supports a more realistic transaction history; it does not
trigger income analysis, salary-change alerts, forecasting, or budgeting.

### Meaning of the bank scenario

The README imagines a stream of KBC transactions. In the working prototype, a
presenter supplies JSON through `POST /api/transactions`, one transaction per
request. This represents an ingestion boundary that could eventually receive
bank data, but there is no implemented bank integration, webhook subscription,
streaming protocol, or broker.

## 3. Current feature status

| Capability | Current status |
|---|---|
| Store invented income and expense transactions | Implemented |
| Return transaction history in date and ID order | Implemented |
| Detect expense price increases | Implemented |
| Discover monthly recurring expenses | Implemented |
| Remove missing recurring expenses after a complete unpaid month | Implemented |
| List active recurring expenses | Implemented |
| Persist notifications and delivery status | Implemented |
| Attempt SMTP delivery to one configured mailbox | Implemented |
| Manually advance a persisted logical demo date | Implemented |
| Preserve history, clock, and accounts across restart | Implemented; historically demonstrated |
| Signup/login with Identity and JWTs | Implemented |
| Require bearer authentication on business endpoints | Implemented |
| Interactive Swagger with bearer Authorize | Implemented |
| Optional exact-origin CORS for a separate webapp | Implemented |
| Product frontend, dashboard, or mobile app | Not implemented in this repository |
| Real KBC or other bank connection | Not implemented |
| Automatic endless randomized transaction generator | Discussed proposal, not implemented |
| Per-user financial datasets and clocks | Represented in alternate entity classes, not integrated |
| User-configurable rules or notification channels | Represented in alternate entity classes, not integrated |
| Container build/deployment configuration | Dockerfile and Podman run guide present; README records build/live verification as pending |
| Automated test suite or CI/CD | Not present |

Authentication controls admission to the API. It does **not** partition financial
data: every authenticated demo account accesses the same transactions, recurring
expenses, notifications, logical clock, and configured email recipient.

## 4. Hackathon constraints and development history

The README describes a four-hour total hackathon budget and approximately
2.5 hours for coding the original API. The authentication plan estimates an
additional 45-60 coding minutes. These are planning budgets, not measured
development times.

The repository's timeboxed policy favors a complete working demonstration:

- One .NET project and direct framework capabilities.
- Plain entity classes and request/response records.
- Direct EF Core access instead of repositories or layered infrastructure.
- Ordinary C# conditions instead of a generic rules engine.
- `EnsureCreated()` instead of a database migration workflow.
- Final Release build and manual happy-path demonstration.
- No automated tests unless specifically requested; no production-readiness claim.

Two completed workstreams are recorded under `dev/active/`:

| Workstream | Deliverable and recorded state |
|---|---|
| `hackathon-api` | Core API, persistence, three expense rules, SMTP, Swagger, documentation, and final demonstration; seven tasks marked complete |
| `basic-jwt-auth` | Identity persistence, signup/login, JWT issuance and validation, protection of six business routes, Swagger authorization, optional CORS, and final demonstration; six tasks marked complete |

Each workstream has a `plan.md`, `tasks.md`, and `context.md`. These are local
development artifacts, not a prerequisite for understanding this briefing.

Some plan sections describe their initial baseline rather than today's code:
the original API excluded authentication and proposed root-level source files.
Later approved changes introduced authentication and moved the single project
to `src/Tectonic.API/`. The current source and README resolve these historical
differences.

The referenced external `hackathon-stack-decisions.md` is not in this checkout.
Its reported decisions are available through the README and plans; the original
external report was not available for this briefing.

## 5. Technical stack

| Concern | Technology or decision | Purpose |
|---|---|---|
| Language | C# | API, entities, rules, and services |
| Runtime | .NET 10, target `net10.0` | Application runtime |
| HTTP API | ASP.NET Core Minimal APIs | Direct route handlers in `Program.cs` |
| Persistence | Entity Framework Core SQLite 10.0.12 | Entity storage and LINQ queries |
| Financial database | SQLite `hackathon.db` | Transactions, recurrence, notifications, logical clock |
| Account database | SQLite `auth.db` | Separate Identity user storage |
| Schema initialization | EF Core `EnsureCreatedAsync()` | Create schemas for a new database; no migrations |
| Authentication store | ASP.NET Core Identity EF integration 10.0.12 | Account creation and password hashing/verification |
| API authentication | ASP.NET Core JwtBearer 10.0.12 | Validate JWTs before protected handlers execute |
| Tokens | HS256 JWT, 60-minute lifetime | Bearer access using real UTC |
| API documentation | Swashbuckle.AspNetCore 10.2.3 | OpenAPI and interactive Swagger |
| Local configuration | DotNetEnv 3.2.0 | Load an ignored local `.env` |
| JSON | Built-in System.Text.Json | Camel-case request and response bodies |
| Email | Built-in `System.Net.Mail.SmtpClient` | Plain-text SMTP delivery |
| Demo inbox | Mailpit | Capture and inspect email locally |
| Logging | Default `ILogger` | Safe notification IDs, kinds, and transport outcomes |
| Container packaging | Multi-stage .NET 10 Dockerfile; Podman run guide | Single API image with external runtime configuration and persisted SQLite volume; verification pending |
| Repository license | GNU Affero General Public License v3 | License supplied in `LICENSE` |

Mailpit is demonstration tooling, not a separate application worker or an
implemented internet-mail provider. Local historical verification used Mailpit
v1.31.3.

There is no implemented Quartz scheduler, message queue, outbox, CQRS layer,
domain-event pipeline, repository abstraction, Aspire setup, or telemetry stack.
Rules execute during requests.

### Why this stack fits the prototype

Minimal APIs keep the HTTP contract close to its implementation. SQLite avoids
a separate database server. EF Core maps the small stored model. Swagger allows
the backend to be demonstrated without a product frontend. Identity avoids
custom password hashing. A logical clock avoids waiting months to demonstrate
monthly behavior.

The two database files have distinct purposes. Identity was added separately so
the existing financial schema did not need to be replaced or migrated;
`EnsureCreated()` does not evolve an existing database schema.

## 6. Architecture and execution flow

```text
Presenter / Swagger / future web client
                  |
             HTTP + JSON
                  |
        ASP.NET Core Minimal API
          /                  \
Anonymous signup/login    Protected business routes
        |                        |
 Identity UserManager       ExpenseService / read queries
        |                        |
     auth.db                 hackathon.db
        |                        |
    JWT issuance       Persist pending notifications
                                 |
                             EmailSender
                                 |
                         SMTP / local Mailpit
                                 |
                       Persist sent/failed status
```

Signup/login operate on Identity accounts. Business routes require a valid
JWT and operate on the shared financial dataset. SMTP is attempted after
business changes have been saved.

### Transaction ingestion

1. Authentication and authorization run before the handler.
2. The handler binds JSON to `TransactionInput`.
3. `ExpenseService.IngestAsync` reads the logical clock and builds the transaction.
4. For expenses, it loads earlier matching history and evaluates price and recurrence.
5. Transaction, recurrence changes, and pending notifications are saved together.
6. Each generated notification is sent independently through SMTP.
7. Delivery status is saved and the request returns `201` with the transaction
   and generated notification responses.

Income is saved without expense-rule notifications. One expense may trigger
both a price-increase and a recurring-added notification.

### Clock advancement

1. A bearer-authorized request supplies a new logical date.
2. A backward date is rejected with `400`; an equal date is allowed.
3. `ExpenseService.AdvanceAsync` checks active recurrence against fully elapsed
   expected months.
4. It saves the clock, any deactivations, and pending removal notifications.
5. It attempts emails and returns the date and newly generated notifications.

### Source ownership

| File or folder | Responsibility |
|---|---|
| `Program.cs` | Configuration, dependency registration, initialization, middleware, endpoints, Swagger |
| `Models/` | Active entities and public JSON contracts |
| `Data/AppDbContext.cs` | Four financial/demo entity sets |
| `Data/AuthDbContext.cs` | Roleless Identity user store |
| `Services/ExpenseService.cs` | Business rules, persistence ordering, delivery outcomes |
| `Services/EmailSender.cs` | Concrete SMTP transport |
| `Services/JwtTokenService.cs` | Signed token creation |
| `appsettings.json` | Non-secret defaults |
| Root `.env.example` | Non-secret local Mailpit configuration template |
| `Dockerfile` and root `.dockerignore` | API image definition and restricted build context |
| `Models/Domain/` | Alternate domain definitions, currently unwired |

## 7. Active domain entities

The running API uses `ExpenseWatch.Api.Models`, not
`ExpenseWatch.Api.Models.Domain`.

### Transaction

Represents one incoming or outgoing mock payment.

| Field | Type and meaning |
|---|---|
| `Id` | Integer assigned on storage |
| `Type` | String: `income` or `expense` by demo convention |
| `CompanyId` | Stable identifier of payer or recipient |
| `CompanyName` | Display name of payer or recipient |
| `ExpenseKey` | Nullable string; stable expense identity, null for income |
| `Description` | Display description |
| `Amount` | Decimal; positive, two fractional digits by demo convention |
| `Currency` | String; EUR in the demo |
| `Date` | `DateOnly`, represented as `YYYY-MM-DD` |

For expenses, `CompanyId` plus `ExpenseKey` distinguishes one obligation from
another. An internet plan and mobile plan at the same company have different
keys. Company names and descriptions are display text, not matching identifiers.
Price comparison also matches currency.

For income, company fields describe the payer. There is no separate bank account,
IBAN, account balance, category entity, or transfer destination entity.

### RecurringExpense

Represents an expense recognized as monthly.

| Field | Meaning |
|---|---|
| `Id` | Stored integer identifier |
| `CompanyId`, `ExpenseKey` | Identity of the expense |
| `DayOfMonth` | Original recurrence day |
| `LastPaymentDate`, `LastAmount` | Latest matching payment information |
| `NextExpectedDate` | Expected date in the following month |
| `Active` | Whether it appears in the active recurring list |
| `RemovedOn` | Nullable internal logical date used to fence rediscovery |

An inactive row may remain for history and later be reused. `RemovedOn` is not
included in the public active-recurring response.

### Notification

Represents an explanation generated by a rule and the outcome of its email attempt.

| Field | Meaning |
|---|---|
| `Id` | Stored integer identifier |
| `Kind` | `price-increased`, `recurring-added`, or `recurring-removed` |
| `CompanyId`, `ExpenseKey` | Affected expense identity |
| `TransactionId` | Optional link to the triggering transaction |
| `Transaction` | Internal EF navigation, not part of the notification response |
| `MissingMonth` | Optional `YYYY-MM` for a removal alert |
| `Subject`, `Body` | Plain-text email content |
| `CreatedOn` | Logical demo date |
| `DeliveryStatus` | `pending`, `sent`, or `failed` |

There is no user-specific recipient field in the active notification model.
Recipient selection comes from application configuration.

### DemoClock

One row with integer `Id = 1` and a `DateOnly Date`. A fresh database starts at
`2026-01-01`; restart preserves the existing date. It lets the presenter simulate
months in minutes, including periods with no transactions.

### IdentityUser

The framework's standard Identity account supplies user identity, email,
username, password-hash storage, and account persistence. Signup uses the trimmed
email as username and requires a unique email. The project defines no custom
user class or roles.

The token's user ID does not currently establish ownership of financial records.

## 8. Alternate domain model: present in code, not integrated

`Models/Domain/` contains nine additional classes. They describe a more
user-specific and configurable model, but neither current database context
registers them and the API services do not use them. Their existence does not
prove an implemented feature or an approved migration.

| Entity | Fields and represented purpose |
|---|---|
| `Transaction` | `Id`, `UserId`/`User`, `TransactionTypeId`/`TransactionType`, `CounterpartyKey`, `CounterpartyName`, `TransactionKey`, `Description`, `Amount`, `Currency`, `Date`; user-linked transaction with an index over user/type/counterparty/key/currency/date |
| `RecurringTransaction` | `Id`, user and type links, `CounterpartyKey`, `TransactionKey`, `Currency`, `DayOfMonth`, `LastTransactionId`/`LastTransaction`, `IsActive`, `RemovedOn`; unique indexed identity across user/type/counterparty/key/currency |
| `TransactionType` | `Id`, unique `Code`, `Name`, `Conditions`; transaction-type catalog |
| `Condition` | `Id`, unique `Code`, `Name`, `TransactionTypes`; condition catalog |
| `ConditionTransactionType` | `ConditionId`/`Condition`, `TransactionTypeId`/`TransactionType`; composite-key join associating conditions with types |
| `UserCondition` | `UserId`/`User`, `ConditionId`/`Condition`, `IsEnabled`, `ChannelId`/`Channel`; composite-key per-user condition preference |
| `NotificationChannel` | `Id`, unique `Code`, `Name`, `IsSupported`; channel catalog |
| `Notification` | `Id`, user/condition/channel links, optional transaction and recurring-transaction links, `MissingMonth`, `Subject`, `Body`, `RecipientAddress`, `OccurredOn`, UTC `CreatedAt`, `DeliveryStatus`; user-linked notification |
| `DemoClock` | `UserId` primary key, `User`, `Date`; per-user logical date |

Conceptually, these definitions associate users with transactions, recurrence,
preferences, notifications, and clocks. Conditions and transaction types have a
many-to-many association through `ConditionTransactionType`. A preference selects
an enabled condition and a channel; a notification references its condition and
channel and may reference a transaction or recurring transaction.

The active implementation instead uses string transaction types, three hard-coded
rules, one shared clock, one shared dataset, and SMTP to one recipient. There are
no endpoints to configure conditions, select channels, or access a private ledger.
No catalog seed values or alternate-model runtime behavior should be claimed
from these classes alone.

## 9. Business rules and edge semantics

### Price increase

Find the most recent expense on an earlier date with the same company ID,
expense key, and currency. Compare its amount with the new amount.

- No earlier match: no price alert.
- Equal or lower amount: no price alert.
- Higher amount: alert with old amount, new amount, and absolute difference.

Any positive increase counts. The comparison is against the immediately previous
earlier matching expense, not a historical average or maximum. An expense does
not need to be recurring. Loaded decimal amounts are compared in C#, avoiding
unsupported SQLite decimal query operations.

### Recurring discovery

A non-active expense becomes recurring when its latest earlier matching expense
is in the immediately preceding calendar month and both dates have the same
numeric day.

January 5 followed by February 5 qualifies. January 5 followed by March 5 does
not. January 5 followed by February 6 does not. December-to-January works across
the year boundary. The amounts may differ.

Once active, matching payments update the last-payment fields and next expected
date without another recurring-added alert. A payment anywhere in the expected
month counts as present. The original recurrence day is retained; next expected
dates are clamped to month end when necessary.

### Missing recurrence

If the last payment was in February, March is the next expected month. The
expense remains active throughout March, even after its expected day. On April 1,
if no matching expense was recorded anywhere in March, it becomes inactive and
one recurring-removed alert names March.

Repeating the evaluation does not remove it again. A jump over several months
reports the earliest unpaid expected month once. Historical transactions and
notifications remain stored.

An inactive expense can become recurring again only from a fresh qualifying
pair whose dates are both on or after removal. Price comparisons still use
earlier history independently of that rediscovery fence.

### Input conventions

The demo expects chronological, single-submission input on the current logical
date, stable identifiers, EUR, and at most one payment per expense key per month.
These are trusted-input conventions, not comprehensive validation guarantees.
Duplicate ingestion, simultaneous writers, refunds, negative amounts, backdated
corrections, and multiple currencies are outside the demonstrated contract.

## 10. API contract

There are eight JSON API operations: two anonymous authentication operations
and six bearer-protected business operations.

| Method | Route | Result |
|---|---|---|
| POST | `/api/auth/signup` | `201` bearer token response after account creation |
| POST | `/api/auth/login` | `200` bearer token response after password verification |
| POST | `/api/transactions` | `201` with `transaction` and generated `notifications` |
| GET | `/api/transactions` | `200` transaction array ordered by date, then ID |
| GET | `/api/recurring-expenses` | `200` active recurring array ordered by ID |
| GET | `/api/notifications` | `200` notification responses ordered by ID |
| GET | `/api/demo/date` | `200` object containing `date` |
| PUT | `/api/demo/date` | `200` with `date` and newly generated `notifications`; backward date returns `400` |

Swagger UI is at `/swagger` and the document is at `/swagger/v1/swagger.json`.
Both remain anonymous so the presenter can obtain and enter a token.

### Expense example

```json
{
  "type": "expense",
  "companyId": "example-telecom",
  "companyName": "Example Telecom",
  "expenseKey": "home-internet",
  "description": "Monthly internet plan",
  "amount": 45.00,
  "currency": "EUR",
  "date": "2026-01-05"
}
```

### Income example

```json
{
  "type": "income",
  "companyId": "example-employer",
  "companyName": "Example Employer",
  "expenseKey": null,
  "description": "Monthly salary",
  "amount": 2500.00,
  "currency": "EUR",
  "date": "2026-01-25"
}
```

### Authentication and response contracts

Signup/login accept `email` and `password`. Their success response contains
`accessToken`, `tokenType: "Bearer"`, and UTC `expiresAt`. Missing fields return
`400`. Duplicate email or an Identity password-policy failure returns `400`
with an `errors` array. Unknown user and wrong password produce the same `401`
error, `"Invalid email or password."`

Clients send `Authorization: Bearer {accessToken}` on business requests. In
Swagger Authorize, paste only the token. No token means `401` on protected routes.

`TransactionInput` defines the eight transaction input fields. `TransactionResponse`
wraps the stored entity and notification responses. `ClockDate` contains the
date; `ClockAdvanceResponse` contains date and notifications. Dedicated recurring
and notification response records exclude internal navigation/removal fields.
Authentication uses `AuthRequest`, `TokenResponse`, `AuthError`,
`AuthValidationError`, and `AuthErrors`.

## 11. Authentication, privacy, and delivery boundaries

Identity uses its standard password policy: at least six characters, uppercase,
lowercase, a digit, and a non-alphanumeric character. Email uniqueness is required;
email verification is not implemented. Signup email does not become the
expense-email destination.

JWTs use HS256, a 60-minute real-UTC lifetime, and `sub`, `jti`, `iat`, `nbf`,
and `exp` claims. Validation checks signature, issuer, audience, lifetime, and
algorithm, with zero clock skew. Advancing the demo clock does not expire a token.

`Jwt__SigningKey` must be Base64 encoding of at least 32 random bytes. Missing,
malformed, or short keys prevent startup. Reusing a key preserves existing
unexpired tokens across restart; changing it invalidates them.

There are no refresh tokens, server logout/revocation, roles, password reset,
account-management flows, per-user financial isolation, or production abuse
controls. A client can discard its token locally; server validity continues
until expiry or key change.

Optional `WebAppOrigin` enables one exact CORS origin for GET/POST/PUT and
Authorization/Content-Type headers. Wildcard origins and credentialed cookies
are not enabled. This is integration support, not an implemented web application.

Email is sent to a single configured, presenter-controlled recipient. Notifications
are stored before delivery. `sent` means SMTP acceptance, not guaranteed final inbox
delivery; `failed` leaves financial state intact. There are no automatic retries.
A crash may leave a persisted notification `pending`. Each send has a 10-second
timeout, so email transport can increase request latency.

The prototype observes financial activity; it has no payment execution authority.
Use invented identities and transactions for the demo, and keep secrets out of
slides, screenshots, source, and captured logs.

## 12. Demonstration story

Use a fresh separate expense database for the exact counts below. The presenter
must authenticate first, then advance the logical date before posting each day's
transactions.

| Step | Logical date and action | What the audience sees |
|---|---|---|
| 1 | January 5: internet expense EUR 45.00 | One stored payment; no alert yet |
| 2 | January 25: salary income EUR 2,500.00 | Income stored; no expense-rule alert |
| 3 | February 5: same internet expense EUR 49.00 | Price increased by EUR 4.00; expense recognized as recurring; two emails |
| 4 | March 31: advance without a March internet payment | Still active because March has not fully elapsed |
| 5 | April 1: advance again | Recurrence deactivated; one email names the missing March payment |
| 6 | Repeat April 1 and inspect lists | No duplicate removal; three transactions, three notifications, no active recurring item |

All dates in this walkthrough are in 2026. Use the same `companyId` and
`expenseKey` for both internet payments.

For a visual demonstration, show Swagger submission, stored transactions,
the active recurring list after February, and the three messages in Mailpit.
Show the March 31 versus April 1 difference to explain the whole-month rule.

The sample emails explain:

- Example Telecom internet increased from EUR 45.00 to EUR 49.00.
- The internet plan was added to recurring expenses.
- No matching internet payment was recorded for March; the item was removed
  from the active recurring list.

An alternate scenario with a March payment shows that removal is prevented.
Another key at the same company demonstrates independent expense identities.
Equal or cheaper payments demonstrate that only increases trigger price alerts.

## 13. Local operation and configuration

Prerequisites are a .NET 10 SDK, package restore access, a demo SMTP inbox or
compatible relay, and a valid signing key. Run build/application commands from
`src/Tectonic.API/` inside this repository.

If root `.env` is absent, copy `.env.example` there. It supplies local Mailpit
settings without real credentials. Do not overwrite an existing local file.

Start Mailpit in another terminal:

```sh
mailpit --smtp 127.0.0.1:1025 --listen 127.0.0.1:8025
```

From the API project directory:

```sh
dotnet build --configuration Release
export Jwt__SigningKey="$(openssl rand -base64 32)"
dotnet run --no-build --configuration Release --no-launch-profile --urls http://localhost:5000
```

Generate the key once and retain it in the running shell for a restart. Do not
regenerate it if the demonstration depends on existing tokens.

Open `http://localhost:5000/swagger` and `http://localhost:8025`. Signup/login,
authorize Swagger, and execute the walkthrough.

| Environment setting | Meaning and default |
|---|---|
| `ConnectionStrings__Expenses` | `Data Source=hackathon.db` |
| `ConnectionStrings__Identity` | `Data Source=auth.db` |
| `Jwt__SigningKey` | Required secret; no default |
| `Jwt__Issuer` | `expense-watch` |
| `Jwt__Audience` | `expense-watch-web` |
| `WebAppOrigin` | Optional exact browser origin |
| `Email__Host` | Relay host; empty JSON default, `localhost` in demo template |
| `Email__Port` | `587` JSON default; `1025` in demo template |
| `Email__EnableSsl` | `true` JSON default; `false` in demo template |
| `Email__From` | Empty JSON default; `expense-watch@example.test` in template |
| `Email__Recipient` | Empty JSON default; `presenter@example.test` in template |
| `Email__Username`, `Email__Password` | Optional relay credentials, environment only |

DotNetEnv loads the nearest `.env` in the working directory or parents before
host creation. Process environment takes precedence over `.env`, which takes
precedence over JSON defaults.

Database paths are relative to process working directory. The documented working
directory places the default files inside `src/Tectonic.API/`. Startup creates
absent schemas and seeds the clock only if absent; it never erases history.
There is no clock reset endpoint or automatic schema upgrade.

To preserve earlier history while demonstrating a new scenario, select a new
expense database filename with `ConnectionStrings__Expenses`. Do not reset
existing databases automatically.

The existing run guide includes a workstation workaround:
`DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false` disables configuration file
watchers if inotify instances are exhausted. It does not disable initial
configuration or `.env` loading.

### Container configuration added during briefing preparation

The shared checkout gained `src/Tectonic.API/Dockerfile`, root `.dockerignore`,
and a README Podman guide during this document's preparation. The Dockerfile
publishes with the official .NET 10 SDK and runs with the ASP.NET 10 runtime as
the non-root application user on port 8080. It sets both database paths under
`/data/`; the run guide uses a named volume to retain them.

From the repository root, the documented image build command is:

```sh
podman build -f src/Tectonic.API/Dockerfile -t localhost/tectonic-api:dev .
```

The guide publishes API port 8080 to loopback host port 5080, injects `.env` and
the signing key at runtime, and connects SMTP to a Mailpit container by name.
The root build-context filter excludes `.env`, SQLite files, and build output.
Its Mailpit image is pinned to v1.30.0, distinct from the earlier local v1.31.3
verification binary.

This is one API container plus a demonstration inbox, not a separate application
worker. The README marks container build and live verification as pending at
this inspection. See its current container section for the complete network,
volume-ownership, and runtime commands; successful deployment is not asserted here.

## 14. Recorded verification and limits

The following results are recorded in the completed workstream contexts and
README on 2026-09-30. They were not independently repeated while writing this
document.

### Core API

- Release build exited successfully with zero warnings and errors.
- All six business operations were exercised through Swagger.
- Desktop and mobile Swagger rendering were inspected.
- The main walkthrough produced three transactions and three sent notifications.
- Mailpit actually captured the three corresponding emails.
- March 31 did not remove the expense; April 1 removed it once.
- Backward clock advancement was rejected and restart retained history/date.
- Alternate scenarios exercised independent keys, equal/cheaper payments, and
  March payments preventing removal.
- SMTP failure preserved financial state and reported failed notifications.

### Authentication

- Signup/login and required-field, duplicate-email, and weak-password failures
  were observed.
- Unknown user and wrong password returned identical failures.
- All six business routes rejected absent, malformed, tampered, and unsigned
  tokens: 24 rejected requests without financial or email effects.
- Valid tokens preserved the original expense walkthrough and three actual emails.
- Accounts, unexpired tokens, and financial records survived restart.
- A second account saw the same dataset, confirming the shared-data policy.
- Swagger Authorize and exact-origin CORS behavior were observed.
- Missing, malformed, and short signing keys failed startup before listening.
- JWT metadata showed HS256 and a 3,600-second lifetime.

### Unverified or excluded areas

Automated tests were not created or run under the explicit hackathon policy.
Rediscovery, year rollover, and month-end clamping exist in code but were not
separately exercised in the recorded walkthrough. Validly signed expired,
wrong-issuer, and wrong-audience token probes were not separately performed,
although the corresponding validation is configured.

There is no demonstrated assurance for concurrency, replay safety, corrections,
refunds, multi-currency behavior, crash-safe delivery, production deployment,
load capacity, or user-data isolation.
The newly added container setup is source-inspected only; its README verification
gate remained pending when this briefing was prepared.

## 15. Proposed automatic simulation

The team has discussed starting an endless randomized transaction stream with
the API. This remains a proposal; the current application starts without
automatically posting transactions.

The requested deployment constraint is one API container with no separate worker.
The recommended approach is a built-in ASP.NET Core `BackgroundService` in the
same process, using `PeriodicTimer` and an HTTP client to call the actual protected
transaction endpoint after server startup.

A proposed simulator would:

- Use a dedicated demo identity and renew bearer authorization before expiry.
- Generate both income and expenses with plausible positive EUR amounts.
- Select fictional counterparties from a stable catalog.
- Retain expense keys so successive payments can match.
- Await each response before another submission.
- Advance simulated days through the clock endpoint so monthly rules can fire.
- Combine scheduled recurring bills with randomized purchases, prices, income,
  and occasional skipped monthly bills.
- Resume from the persisted logical date and stop with application shutdown.

Pure randomness is insufficient for a dependable presentation: arbitrary keys
and dates rarely produce two consecutive same-day monthly payments. A useful
simulation combines domain-aware scenarios with varied amounts and counterparties.

Quartz.NET is unnecessary for this proposed simple loop. A mocking library is
also unnecessary because the goal is invented input data, not replacement of
dependencies. A small catalog and ordinary random generation would suffice.
Cadence, probabilities, and automatic enablement are proposed configuration
choices, not implemented settings.

The repository now contains an API Dockerfile and container run documentation.
The proposed generator would live inside that API process; it would not add a
worker container. Packaging definitions do not establish successful deployment:
the container build/live verification remains pending in the inspected README.

## 16. Presentation structure and reusable slide material

### Suggested ten-slide outline

| Slide | Suggested title | Main content and visual |
|---|---|---|
| 1 | Expense Watch | One-sentence pitch; Tectonic Team One |
| 2 | Changes hide in transaction history | Higher internet bill, recurring charge, missing monthly payment |
| 3 | Three clear alerts | Price increase, recurrence discovered, expected payment missing |
| 4 | From payment to explanation | Transaction -> comparison -> saved state -> email |
| 5 | Working prototype | Authenticated API, SQLite persistence, Swagger, captured email |
| 6 | January to April in minutes | The EUR 45 -> EUR 49 -> missing-March timeline |
| 7 | Domain model | Transaction, RecurringExpense, Notification, DemoClock, IdentityUser |
| 8 | Technical stack | .NET 10, Minimal APIs, EF Core/SQLite, Identity/JWT, SMTP, Swagger |
| 9 | Demonstration and evidence | Screenshots from the team's actual demo; three email outcomes |
| 10 | Next steps and boundaries | Proposed generator and model integration; no real bank connection today |

Use the active four financial entities for a "what works today" domain slide.
If showing the alternate model, label it "model definitions not yet integrated".
Keep the detailed API/configuration tables in presentation backup slides.

### Claims supported by the project

- "We detect price increases by comparing an expense with its previous occurrence."
- "We discover monthly expenses from consecutive same-day payments."
- "We check a full missing month before removing an active recurrence."
- "We implemented signup/login and bearer-protected business endpoints."
- "The recorded demo persisted data and captured three emails in Mailpit."
- "A logical clock lets us demonstrate months without waiting."

### Claims to avoid

- "We are connected to KBC" or "we receive live bank transactions."
- "We cancel subscriptions" or "a missing payment proves cancellation."
- "Every user has private financial data" or "users can configure rules today."
- "Transactions are automatically generated on startup."
- "The product has AI-powered detection": current rules are ordinary conditions.
- "The dashboard/mobile app is implemented": Swagger is the only interface here.
- "Production-ready", "fully tested", or claims of measured savings or adoption.

### Questions the presenter is likely to receive

**Is the data real?** No. The prototype uses invented transactions submitted to
an ingestion endpoint.

**Why email?** It makes each detected change visible without requiring a custom
frontend. The prototype sends to a single controlled demonstration inbox.

**How is recurrence detected?** Two matching expenses in consecutive calendar
months on the same numeric day. This is intentionally simple, not statistical
pattern recognition.

**Why wait until the month ends?** A payment later in the expected month still
counts; missing the expected day alone should not trigger removal.

**Does a notification failure lose a transaction?** No. Financial state and the
pending notification are saved first; delivery failure is then recorded.

**Are all accounts isolated?** No. They are authenticated accounts sharing one
invented dataset in the current demo.

**Why no Quartz or separate worker?** Current rules run during requests. The
proposed generator only needs a small hosted loop in the API process.

**What would be needed for a real product?** A permitted bank integration,
per-user data ownership, robust ingestion and delivery, broader validation,
security/privacy work, deployment, and verification. These are future engineering
needs, not delivered features or a committed roadmap.

## 17. Sources and reconciliation notes

This briefing consolidates all existing project Markdown documentation discovered
in this checkout, the safe configuration template, and the implementation:

| Source | Contribution |
|---|---|
| [README](README.md) | Product idea, current API contract, demo assumptions, run guide, recorded checks |
| [Repository instructions](AGENTS.md) | Hackathon timebox and verification policy |
| `dev/active/hackathon-api/plan.md` | Original core design, rules, constraints, later layout/configuration amendments |
| `dev/active/hackathon-api/tasks.md` | Completed seven-task implementation ledger |
| `dev/active/hackathon-api/context.md` | Final core verification, dependencies, operational history |
| `dev/active/basic-jwt-auth/plan.md` | Authentication design, boundaries, API and frontend integration contract |
| `dev/active/basic-jwt-auth/tasks.md` | Completed six-task authentication ledger |
| `dev/active/basic-jwt-auth/context.md` | Final auth verification and unexercised cases |
| [Project file](src/Tectonic.API/Tectonic.API.csproj) | Actual runtime and direct dependency versions |
| [Program](src/Tectonic.API/Program.cs) | Actual routes, authorization, configuration, startup, Swagger |
| [Financial context](src/Tectonic.API/Data/AppDbContext.cs) | Which financial entities are actually stored |
| [Identity context](src/Tectonic.API/Data/AuthDbContext.cs) | Separate standard Identity store |
| [Expense service](src/Tectonic.API/Services/ExpenseService.cs) | Actual conditions, calendar semantics, save/send order |
| [Email sender](src/Tectonic.API/Services/EmailSender.cs) | SMTP behavior, timeout, recipient and status boundaries |
| [JWT service](src/Tectonic.API/Services/JwtTokenService.cs) | Issuance algorithm, claims, and lifetime |
| [Models](src/Tectonic.API/Models/) | Active entities, response contracts, and alternate domain definitions |
| [Configuration](src/Tectonic.API/appsettings.json) and [.env.example](.env.example) | Non-secret defaults and local inbox settings |
| [Dockerfile](src/Tectonic.API/Dockerfile) and [.dockerignore](.dockerignore) | Concurrently added container configuration; README records verification pending |
| [License](LICENSE) | AGPL v3 repository license |

No separate `docs/` or `docs/internal/` directory exists in the project.
Development notes are locally excluded and may not accompany another checkout;
their relevant product and verification context is included here rather than
required as external reading.

When historical plans conflict with the current implementation, this document
uses the implementation for current behavior. The automatic generator discussion
is included as a proposal only. This briefing adds documentation and does not
change application behavior, databases, task artifacts, or deployment.

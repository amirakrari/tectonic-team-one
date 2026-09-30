# Tectonic Team One - Expense Watch

An API-only hackathon demo using invented transactions. It tracks recurring
expenses **and income**, detects expense price increases and missing recurring
payments, and emails the presenter-controlled demo mailbox. It never connects
to KBC or controls real payments.

## Stack and scope

One .NET 10 Minimal API in `src/Tectonic.API`, EF Core SQLite, ASP.NET Identity
users/roles, 60-minute JWTs, Swashbuckle Swagger UI and built-in SMTP. A
`BackgroundService` uses `PeriodicTimer` and sequential authenticated HTTP to
simulate accelerated days. No Quartz, mocking library, separate worker, frontend,
migrations, queues, production infrastructure or automated test suites.

The implementation uses the normalized `Models/Domain` entities, not the former
shared-data models. Each account owns its history, recurring streams,
notifications, preferences and logical date. Account email is an unverified login
identifier; it never overrides the configured expense-email recipient.

## Run locally

Requirements: .NET 10 SDK, a Mailpit binary (or your controlled SMTP relay), and
the repository-root `.env` configuration. Do not commit `.env`, credentials or DBs.

```sh
cp .env.example .env
mailpit --smtp 127.0.0.1:1025 --listen 127.0.0.1:8025
```

In another terminal, from the repository root:

```sh
export Jwt__SigningKey="$(openssl rand -base64 32)"
export Simulator__Password="aA1!$(openssl rand -hex 16)"
cd src/Tectonic.API
dotnet build --configuration Release
dotnet run --no-build --configuration Release --no-launch-profile --urls http://localhost:5000
```

Open [Swagger](http://localhost:5000/swagger) and
[Mailpit](http://localhost:8025). Swagger and `/swagger/v1/swagger.json` are public
local demo tools. Signup/login, then paste `accessToken` into **Authorize**.
DotNetEnv loads the nearest `.env` without overriding injected environment values.
For manual-only operation set `Simulator__Enabled=false` before starting.

### Configuration

| Setting | Default / authority |
|---|---|
| `ConnectionStrings:Application` | `Data Source=tectonic.db`, in API working directory |
| `Jwt:Issuer`, `Jwt:Audience` | `expense-watch`, `expense-watch-web` |
| `Jwt__SigningKey` | Environment/ignored `.env`; Base64 of at least 32 random bytes |
| `Email:Host`, `Port`, `EnableSsl` | Demo `.env`: localhost, 1025, false |
| `Email:From`, `Recipient` | Controlled fictional sender/presenter mailbox from `.env` |
| `Email__Username`, `Email__Password` | Optional injected relay credentials only |
| `WebAppOrigin` | Optional exact webapp origin for CORS; no wildcard/cookies |
| `Simulator:Enabled` | true; missing credentials stop the worker, not the API |
| `Simulator:IntervalSeconds` | 3, one logical day per completed batch |
| `Simulator:BaseUrl` | `http://127.0.0.1:5000`; must match listener and be loopback |
| `Simulator:Email` | `simulator@example.test`, dedicated demo account |
| `Simulator__Password` | Injected disposable Identity-compliant password; never tracked |
| `Simulator:RenewBeforeExpirySeconds` | 60; greater than zero and less than 3600 |

Environment overrides use `__`, for example `Simulator__IntervalSeconds=1`.
The stock Identity password policy requires at least six characters including
uppercase, lowercase, digit and non-alphanumeric characters.

### Existing container deployment

The existing `src/Tectonic.API/Dockerfile` publishes the same project and runs
the API as the framework non-root user on port 8080. It uses
`ConnectionStrings__Application=Data Source=/data/tectonic.db` and simulator
loopback `http://127.0.0.1:8080`. Mount writable persistent storage at `/data`.
Inject the signing key, simulator password and reachable SMTP configuration.
`localhost` SMTP means inside that container, not the host.

```sh
podman build -f src/Tectonic.API/Dockerfile -t localhost/tectonic-api:dev .
```

Docker can use the same Dockerfile/root build context. No separate simulator
container is needed. Current image verification is reported separately from
native API verification; do not infer an image check from a .NET build.

## Database and model

`AppDbContext : IdentityDbContext<IdentityUser>` owns standard Identity tables
and nine domain entities:

| Entity | Key / purpose |
|---|---|
| `TransactionType` | Fixed ID/code lookup: 1 expense, 2 income |
| `Condition` | Fixed ID/code lookup: 1 price-increased, 2 recurring-added, 3 recurring-missing |
| `ConditionTransactionType` | Composite `(ConditionId, TransactionTypeId)` applicability FK join |
| `NotificationChannel` | 1 email supported; 2 sms and 3 in-app unsupported |
| `UserCondition` | Composite `(UserId, ConditionId)`, enabled flag and selected channel |
| `Transaction` | User-owned immutable history and counterparty/stream identity |
| `RecurringTransaction` | User-owned stream, original day, last transaction and removal state |
| `Notification` | Owner, condition/channel, source links, rendered message and delivery result |
| `DemoClock` | `UserId` primary key and persisted logical date |

All three conditions apply to expenses. Only recurring-added/missing apply to
income. There is no parsed list of types or dynamic rules language.
Signup atomically creates the account, standard Identity `User` role assignment,
three enabled email settings and a clock beginning `2026-01-01`.
No custom application user/role tables, Admin surface, refresh/logout endpoint,
email verification or password-reset workflow.

This is a fresh integrated schema: legacy `auth.db` and `hackathon.db` are left
untouched and are not imported. Sign up again for this store. `EnsureCreated`
does not upgrade an existing schema. Never delete a DB automatically on startup;
resetting disposable demo data requires explicitly stopping the app and choosing
the exact file to replace.

## HTTP contract

JSON is camel-case. Dates are `YYYY-MM-DD`; amounts are positive, two-decimal EUR.
All `/api` endpoints require a valid JWT **except** signup/login.
Ownership comes from JWT `sub`, not submitted JSON. Signature, HS256 algorithm,
issuer, audience, real UTC lifetime and account existence are checked.

| Method / route | Success |
|---|---|
| POST `/api/auth/signup` | 201 bearer token |
| POST `/api/auth/login` | 200 bearer token |
| POST `/api/transactions` | 201 `{transaction, notifications}` |
| GET `/api/transactions?date=2026-02-05` | 200 own history; date filter optional |
| GET `/api/recurring-transactions` | 200 own active income and expenses |
| GET `/api/notifications` | 200 own rendered notifications/outcomes |
| GET `/api/demo/date` | 200 `{date}` |
| PUT `/api/demo/date` | 200 `{date, notifications}` |
| GET `/api/transaction-types` | 200 seeded IDs/codes/names |
| GET `/api/conditions` | 200 conditions with applicable type IDs |
| GET `/api/notification-channels` | 200 lookup including `isSupported` |
| GET `/api/me/conditions` | 200 own enabled/channel settings |
| PUT `/api/me/conditions/{conditionId}` | 200 updated setting |

Auth body: `{"email":"presenter@example.test","password":"<your disposable password>"}`.
Response: `{accessToken, tokenType:"Bearer", expiresAt}`.
Send `Authorization: Bearer <accessToken>`. No token/password appears in logs.
Login failure is identical for unknown user/wrong password (401). Signup Identity
errors are 400. Expired tokens require login again; local sign-out just discards
the token. Tokens are not server-revoked before expiry; changing the key invalidates them.

Transaction example after advancing that account to January 5:

```json
{
  "type": "expense",
  "counterpartyKey": "example-telecom",
  "counterpartyName": "Example Telecom",
  "transactionKey": "home-internet",
  "description": "Fictional home internet",
  "amount": 45,
  "currency": "EUR",
  "date": "2026-01-05"
}
```

For income use `"type":"income"`, employer counterparty, and a stable key such as
`monthly-salary`. Counterparty is the payee for expenses and payer for income.
Both directions require `transactionKey`; no `userId` input is accepted.
The old company/expenseKey request fields and recurring-expenses route are replaced.
One stream/date can be submitted once; duplicate -> 409. Unknown type, invalid
amount or date unequal to your clock -> 400. Earlier clock -> 400.

Preference body: `{"isEnabled":false,"channelId":1}`.
Unknown condition -> 404; unsupported/unknown channel -> 400.
Disabling a condition suppresses its notifications, not transaction storage or
recurrence tracking. One selected channel per condition; only email is implemented.

## Rules and notifications

Stream identity is user + type + counterpartyKey + transactionKey + currency.
Display name is never the matching key.

- Price increase: expenses only, compared with the immediately previous earlier
  transaction. Any positive increase alerts; EUR 45 -> 49 reports EUR 4.00.
- Recurrence: expenses and income, matching numeric day in consecutive calendar
  months. Equal-day January/February qualifies; a skipped month or changed day does not.
- Once active, a payment anywhere in the expected month keeps it active. Original
  day is retained; the displayed expected date clamps to shorter month ends.
- Missing: only after the expected month has fully ended. March 31 is not missing
  March; April 1 is. Remove once and name the earliest unpaid month on large jumps.
- Reactivation: two fresh qualifying payments after removal. History remains and
  still participates in price comparison. Missing records never imply cancellation.

Business state and pending notifications are saved atomically before SMTP.
Each email independently becomes sent/failed; `sent` means SMTP acceptance.
Use the mailbox to establish receipt. SMTP failure never undoes business data.
Crashes can leave pending notifications; there are no automatic retries/outbox.
Notifications retain condition/channel/source links and a recipient/message snapshot.
`OccurredOn` is logical; `CreatedAt` and token expiry use actual UTC.

## Continuous simulator

The hosted worker waits for `ApplicationStarted`, logs in (or signs up once),
and calls the same protected HTTP routes as the webapp. It does not bypass the
API using a DbContext. Credentials/tokens/HTTP auth bodies are not logged.

Each tick reads its own persisted clock/day history, posts missing daily records
sequentially, then advances one day. Existing stream/date identities are skipped
after a restart. A failed/uncertain request stops the worker without blind replay;
the API remains available. Shutdown cancels the timer and requests.
JWTs are renewed before expiry. Safe `DayCompleted`, `TokenRenewed` and
`SimulatorStopped` events explain progression.

The fictional scenario combines:
- Telecom on day 5: first month of each Jan/Apr/Jul/Oct cycle EUR 45, second EUR 49,
  third omitted. This guarantees discovery/increase and a later missing-month alert.
- Salary on day 25: EUR 2,500 every month, demonstrating recurring income.
- One seeded-random daily purchase/bonus with stable daily key and integer-cent
  amount. Selection is approximately 75% expenses/25% income, not a strict ratio.

Dates are accelerated, not wall-clock waits. The simulator owns writes and clock
advancement for its dedicated identity; do not manually write using that account
while enabled. Other accounts have independent clocks and can be used normally.
History grows while enabled; stop with configuration/shutdown, not automatic cleanup.

## Manual demo and verification

Disable the simulator for manual demonstration. Signup, authorize, advance to
Jan 5 and submit EUR 45 internet; Jan 25 salary; Feb 5 EUR 49 internet; Feb 25
salary. Internet generates price-increased and recurring-added; salary generates
recurring-added only. March 31 produces no missing alert; April 1 without March
payments produces missing alerts, one per stream. Repeat the date: no duplicates.

Use a second account with the same stream keys: history, recurrence, settings,
notifications and date must remain independent. Disable a condition and confirm
state still updates without that alert. SMS/in-app selection must fail.
Restart preserves users/history/clocks in the same store.

Then enable the simulator and observe actual POST/PUT traffic, daily completion,
income and expense rows, the fixed monthly scenario, restart deduplication and
captured Mailpit emails. Observe exact events rather than fixed sleep-based checks.

Final verification evidence is recorded after the implementation gate. Automated
tests are intentionally absent under this repository's hackathon policy.
This trusted-input, single-instance, sequential-writer demo is not production
banking software or a multi-instance scheduling system.

## License

GNU AGPL v3; see [LICENSE](LICENSE). No third-party application implementation
was copied into this project.

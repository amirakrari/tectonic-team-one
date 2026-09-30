# Tectonic Team One - Expense Watch

A hackathon API that turns simulated bank transactions into email alerts about
price increases, newly recurring expenses, and recurring payments that stop.

> **Status:** Implemented .NET 10 API with SQLite persistence, SMTP delivery
> outcomes, and interactive Swagger. Release build and manual demonstration
> verified, including three actual captured emails in local Mailpit.
> **Scope:** Backend API only. All financial data is invented for the demo.

## The idea

People can miss a subscription price increase or lose track of their regular
bills. Expense Watch compares incoming expenses with their history and explains
changes by email.

The demo imagines a stream of KBC income and expense transactions, but **never
connects to KBC**. A presenter submits JSON transactions to a mock ingestion
endpoint. Each request represents one transaction in the stream; there is no
bank connection, webhook subscription, message broker, or streaming protocol.

The API stores both income and expenses. Only expenses trigger the three initial
rules:

| Rule | Trigger | Email |
|---|---|---|
| Price increase | The same expense at the same company costs more than its previous occurrence | "Your expense at Example Telecom increased from EUR 45.00 to EUR 49.00." |
| Recurring expense discovered | The same expense occurs on the same calendar day in two consecutive months | "Example Telecom internet plan was added to your recurring expenses." |
| Recurring expense missing | The next expected payment month has ended without a matching expense | "No Example Telecom internet payment was recorded for March. It was removed from your recurring expenses." |

The last alert describes missing mock data. It does not prove a subscription was
cancelled, and the API never initiates, blocks, or cancels a payment.

## Hackathon scope and stack

The shared **Hackathon Stack Decisions Report**, supplied as
`hackathon-stack-decisions.md`, sets a four-hour total budget with approximately
2.5 hours of coding. This design follows its single-project, demo-first approach.
The API contract and demo defaults below are specific to this idea,
not decisions already made in that report.

| Concern | Decision |
|---|---|
| Runtime | .NET 10 and ASP.NET Core Minimal API |
| Persistence | EF Core 10 with SQLite and one local `hackathon.db` file |
| Database setup | `EnsureCreated()`, without a migrations workflow |
| Structure | One `.csproj`; `Program.cs`, `Models/`, `Data/`, and small `Services/` only as needed |
| Data access | Inject `AppDbContext` directly; use plain classes/records and LINQ |
| JSON and logging | Built-in System.Text.Json and default `ILogger` |
| Rule execution | Synchronous processing during ingestion or demo-date advancement |
| Email | Built-in SMTP sender to one configured demo recipient, with a 10-second timeout |
| Authentication | ASP.NET Identity signup/login with 60-minute HS256 JWT bearer tokens |
| UI | Interactive Swagger API tooling only; no product frontend |

No multi-tenancy, custom input-validation layer,
CQRS, repositories, domain events, queues, outbox, Quartz, containers, Aspire,
telemetry stack, or CI/CD is planned. There is no configurable rules engine:
these are three ordinary conditions in C#.

The repository's [Hackathon Timeboxed Mode](AGENTS.md) excludes automated tests
unless explicitly requested and prohibits running test suites during
implementation. Implement the complete feature first, then run one final build
and manually exercise the demo happy path. This README holds the idea, contract,
and demo guide; there is no separate planning or documentation framework.

## Demo assumptions

These conventions define the implemented demo behavior:

- All authenticated demo users share one invented financial dataset, one
  logical clock, one configured expense-email recipient, and one currency: EUR.
- Amounts are positive decimal values with two fractional digits. `type`
  distinguishes `income` from `expense`; negative amounts are not used.
- Dates use `YYYY-MM-DD` with calendar-date semantics, not timestamps.
- The presenter submits each transaction once, in date order, with stable
  company and expense identifiers. Duplicate imports, backdated corrections,
  refunds, and concurrent submissions are outside the demo.
- The same expense is identified by **`companyId` + `expenseKey`**, not amount
  or company name alone. For example, an internet plan and a mobile plan at the
  same company have different keys. Price changes must not break matching.
- There is at most one payment per expense key in a calendar month in the demo.
- Missing-payment removal happens after the **whole expected month** ends, not
  the day after its expected payment date. A payment later in that month still
  counts as present.

Input is trusted demo data. These are input conventions, not a promised
validation subsystem or production banking behavior.

## API contract

All six business routes are implemented and require a valid bearer token.
Requests and responses use plain JSON with camel-case property names and
lowercase string transaction types.

| Method | Route | Purpose and success response |
|---|---|---|
| `POST` | `/api/transactions` | Store one mock transaction, evaluate expense rules, attempt resulting emails; `201` with the stored transaction and generated notification records |
| `GET` | `/api/transactions` | Return `200` with an array of stored income and expenses in date order |
| `GET` | `/api/recurring-expenses` | Return `200` with an array of active recurring expenses |
| `GET` | `/api/notifications` | Return `200` with an array of generated emails and their delivery status |
| `GET` | `/api/demo/date` | Return `200` with the current logical date, for example `{"date":"2026-01-01"}` |
| `PUT` | `/api/demo/date` | Advance the logical date, evaluate missing payments, and return `200` with `date` and newly generated `notifications` |

### Signup, login and bearer access

`POST /api/auth/signup` and `POST /api/auth/login` are anonymous and accept the
same JSON body:

```json
{"email":"presenter@example.test","password":"<disposable demo password>"}
```

Signup trims the email and creates an Identity-hashed account with username equal
to email. Identity's default password policy requires at least six characters,
a digit, lowercase, uppercase and a non-alphanumeric character. Email must be
unique; it is **unverified** and never changes the expense-email recipient.
Use invented emails and disposable passwords.

Signup returns `201`, login returns `200`, with:

```json
{"accessToken":"<signed JWT>","tokenType":"Bearer","expiresAt":"<UTC ISO-8601 timestamp>"}
```

Missing/empty fields return `400`. Duplicate email or invalid signup returns
`400` with `{"errors":[{"code":"...","description":"..."}]}`. Unknown email and
wrong password return the same `401` body:
`{"error":"Invalid email or password."}`. Failures never return a token.

Send `Authorization: Bearer <accessToken>` on every business request. Without a
valid token, all six business routes return `401` without entering the expense
handler or changing clock/history/email state. Signup/login, Swagger UI and the
OpenAPI document remain anonymous.

Tokens use HS256, real UTC (not the logical demo clock), a 60-minute lifetime,
zero clock skew, and validated signature, issuer, audience, algorithm and expiry.
Claims include user ID `sub`, unique `jti`, and UTC `iat`, `nbf`, `exp`.
The non-secret defaults are issuer `expense-watch` and audience `expense-watch-web`.

The presenter must configure `Jwt__SigningKey`: Base64 of at least 32 random
bytes, in the process environment or ignored root `.env`. There is no startup
fallback; missing, malformed or short keys stop the application safely. For a
temporary local demo, generate once in the terminal used to run the API:

```sh
export Jwt__SigningKey="$(openssl rand -base64 32)"
```

Keep the same key when restarting. Do not put the generated value in tracked
files, notes, logs, screenshots or online JWT tools. Restarting with a different
key invalidates prior tokens. User accounts persist separately in `auth.db`,
created with `EnsureCreated()` without changing the expense schema.

In Swagger, use signup/login **Try it out**, copy `accessToken`, click
**Authorize**, and paste only the token. Swagger adds the Bearer header. A webapp
does the same header addition and returns to login on `401`; prefer keeping
disposable demo tokens in memory rather than URLs.

Optional `WebAppOrigin` (environment `WebAppOrigin`) enables CORS for one exact
origin, for example `http://localhost:5173`, with GET/POST/PUT and
Authorization/Content-Type headers. No origin means no cross-origin grant.
Credentialed cookies and wildcard origins are not enabled.

All accounts access the same mock transactions, notifications and clock. There
are no roles, per-user financial records, refresh/logout/revocation endpoints,
email verification, password reset or account-management flows. Token expiry
requires login again. Client-side logout discards the token; it remains valid
server-side until expiry or key change.

### Transaction JSON

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

`companyId` and `expenseKey` are stable, presenter-supplied matching identifiers.
`companyName` and `description` are display text, not matching keys. A second
company using the same expense key is still a different expense.

For income, use `"type": "income"`, identify the payer in the company fields,
and set `"expenseKey": null`. Income is stored but never used in price
comparisons, recurring-expense detection, or missing-payment matching.

On creation, the API assigns an integer `id`. The response contains
`transaction` (the input fields plus `id`) and `notifications` (an array, empty
when no rule fires). A higher second monthly payment can produce two records:
one for the price increase and one for becoming recurring.

### Minimal stored data

| Record | Fields beyond the transaction JSON above |
|---|---|
| Transaction | `id` |
| Recurring expense | `id`, `companyId`, `expenseKey`, `dayOfMonth`, `lastPaymentDate`, `lastAmount`, `nextExpectedDate`, `active` |
| Notification | `id`, `kind`, `companyId`, `expenseKey`, optional `transactionId`, optional `missingMonth` (`YYYY-MM`), `subject`, `body`, `createdOn`, `deliveryStatus` |
| Demo clock | One stored `date`, initially `2026-01-01` |

Notification kinds are `price-increased`, `recurring-added`, and
`recurring-removed`. Delivery states are `pending`, `sent`, and `failed`.
Dates on generated records use the logical demo date.

Inactive recurring records can remain in SQLite for history, but
`GET /api/recurring-expenses` returns only active ones. Transaction and
notification history remain available after an item leaves the recurring list.
An internal nullable `removedOn` date fences rediscovery: both payments in a
fresh qualifying pair must be on or after removal. It is not exposed in the
active recurring response. The clock is stored as singleton ID 1.

## Rule behavior

### 1. Detect a price increase

For a new expense, find the most recent earlier expense with the same
`companyId`, `expenseKey`, and currency.

- No previous expense: save it without a price alert.
- New amount equals or is below the previous amount: no price alert.
- New amount is greater: create an email with the company, expense description,
  previous amount, new amount, and absolute increase.

Any increase counts; there is no minimum threshold. Compare with the immediately
previous expense, not the historical maximum or average. The expense does not
need to be recurring, and the two payments do not need to be a month apart.
For EUR 45.00 followed by EUR 49.00, the increase is EUR 4.00.

### 2. Discover a recurring expense

For an expense not currently active in the recurring list, compare its date
with the most recent earlier matching expense. Add it when:

1. The earlier expense is in the immediately preceding calendar month.
2. Both dates have the same numeric day of the month.

January 5 followed by February 5 qualifies; January 5 followed by March 5 or
February 6 does not. December followed by January qualifies across a year
boundary. Amounts need not be equal.

On discovery, mark the expense active, remember its day of month, record the
latest payment, and send one `recurring-added` email. Further payments while
active update the last payment and next expected date without another added
email. They can still trigger a price-increase email.

After activation, a matching payment anywhere in the expected month keeps the
item active. Its original day of month remains the expected day. If that day
does not exist in a later month, display the last day of that month as
`nextExpectedDate`; do not permanently change the original day. Initial
detection still requires two identical numeric days.

### 3. Remove a missing recurring expense

If the last matching payment was February 5, the next expected payment is in
March. Keep the item active throughout March, including after March 5. On
April 1, if there was no matching expense anywhere in March:

1. Mark the recurring expense inactive.
2. Remove it from the active recurring list.
3. Send one `recurring-removed` email naming the missing month.

Repeated evaluation does not send another removal email for an inactive item.
If the logical date jumps over several months, check the earliest unpaid
expected month and remove the item once, rather than emailing for every month.

Later payments remain in transaction history and still participate in price
comparisons. An inactive item becomes recurring again only after a fresh pair
of qualifying payments following its removal.

## Logical date and processing order

A manual logical clock makes several months demonstrable in minutes and lets
missing-payment detection run even when no transactions arrive. No real-time
scheduler or server-clock wait is needed.

- The clock begins at `2026-01-01` and is persisted across restarts.
- Submit transactions on the current logical date. Before moving beyond a
  date, submit all payments belonging to it.
- `PUT /api/demo/date` accepts `{"date":"2026-02-05"}`. Advance dates only;
  resetting the demo means starting with a fresh demo database.
- Moving the clock runs the missing-payment check immediately. It evaluates
  only fully elapsed expected months, so advancing to February 5 before
  posting that day's payment does not declare February missing.
- Repeating the same clock date is allowed and must not create duplicate
  removal notifications.
- An earlier date returns `400` with `{"error":"The demo date cannot move backwards."}`
  and does not mutate state.

For transaction ingestion, read earlier matching history, evaluate the price and
recurring rules, save the transaction, state and pending notification records
atomically, then attempt email delivery before returning the response. Income
bypasses the expense rules. For date advancement, persist the new date and
removal decisions, then attempt any removal emails.

There is no generic rule pipeline, background worker, retry system, or message
broker in this scope.

## Email behavior

Send all emails to a single configured, presenter-controlled demo mailbox, not
to an address supplied in each transaction. The built-in SMTP transport supports
STARTTLS through `EnableSsl`; use a compatible relay, not an implicit-TLS-only
port. This choice is not prescribed by the shared stack report.

Persist each notification before attempting delivery. Set its status to `sent`
only when the transport accepts it, or `failed` when sending fails. A delivery
failure does not undo the stored transaction or recurring-state change. The
ingestion response and notification list expose that status. Transport
acceptance is not a guarantee of inbox arrival.

There are no automatic retries or delivery guarantees across a process crash;
a record may remain `pending`. There is no console-preview delivery substitute.
One event matching two rules deliberately produces two emails.

Use ordinary configuration for non-secret settings and environment variables
for email credentials. Do not commit credentials or real banking data. The
brief's example of inline configuration does not override its explicit
no-secrets-in-source rule.

## Manual demo walkthrough

Start with an empty database and logical date `2026-01-01`. Use the same company
and `home-internet` key for all internet payments.

| Step | Action | Expected result |
|---|---|---|
| 1 | Advance to January 5; submit EUR 45.00 internet expense | Stored, no email, no recurring item |
| 2 | Advance to January 25; submit EUR 2,500.00 salary income | Stored, no expense-rule email |
| 3 | Advance to February 5; submit EUR 49.00 internet expense | EUR 4.00 price-increase email and recurring-added email; next expected date March 5 |
| 4 | Advance to March 31 without a March internet payment | Item remains active; no removal email yet |
| 5 | Advance to April 1 | Item leaves active list; one email says March payment was missing |
| 6 | Set April 1 again; inspect notifications | No duplicate removal; exactly three notifications across this walkthrough |

Also demonstrate that another expense key at the same company is independent,
an equal or cheaper payment creates no price alert, and a matching March payment
would prevent the April 1 removal. Use a separate fresh run for alternate
scenarios so the main walkthrough's expected counts remain clear.

### Local run

Install the .NET 10 SDK and allow NuGet restore access. Copy `.env.example` to
`.env` at the repository root if the local file is absent. It configures the
presenter mailbox in Mailpit, without real credentials. Start Mailpit in a
separate terminal using its installed binary:

```sh
mailpit --smtp 127.0.0.1:1025 --listen 127.0.0.1:8025
```

Open [Mailpit](http://localhost:8025) to see captured emails. Mailpit is a local
development inbox, not an internet delivery relay. From the repository root:

```sh
cd src/Tectonic.API
dotnet build --configuration Release
export Jwt__SigningKey="$(openssl rand -base64 32)"
dotnet run --no-build --configuration Release --no-launch-profile --urls http://localhost:5000
```

Open [Swagger UI](http://localhost:5000/swagger) and use **Try it out** on each
route. The document is at `/swagger/v1/swagger.json`; both are available without
a launch profile. Signup/login first, then use **Authorize** for business routes.
Keep this trusted-input demo bound to loopback.

The project pins `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12,
`Swashbuckle.AspNetCore` 10.2.3, and `DotNetEnv` 3.2.0.
Identity.EntityFrameworkCore and Authentication.JwtBearer are pinned to 10.0.12.
Source layout:

```text
.env.example                           # non-secret Mailpit settings template
.env                                   # ignored local configuration
src/Tectonic.API/
  Tectonic.API.csproj
  Program.cs                           # host, persistence, routes, Swagger
  appsettings.json                     # non-secret settings
  Models/                              # stored entities and JSON contracts
  Data/AppDbContext.cs                  # four stored record types
  Data/AuthDbContext.cs                 # separate roleless Identity user store
  Models/AuthContracts.cs               # auth request/response contracts
  Services/JwtTokenService.cs           # HS256 token issuance
  Services/ExpenseService.cs            # rules, atomic saves, delivery outcomes
  Services/EmailSender.cs               # concrete SMTP sender
```

DotNetEnv loads the nearest `.env` in the working directory or its parents
before host creation. Existing process environment variables take precedence
over `.env`, which takes precedence over `appsettings.json`.
The supplied root `.env.example` sets localhost, SMTP port 1025, TLS off,
`expense-watch@example.test` as sender, and `presenter@example.test` as recipient.
These override the relay defaults below:

| Setting | Environment variable | Default |
|---|---|---|
| `ConnectionStrings:Expenses` | `ConnectionStrings__Expenses` | `Data Source=hackathon.db` |
| `ConnectionStrings:Identity` | `ConnectionStrings__Identity` | `Data Source=auth.db` |
| `Jwt:Issuer` | `Jwt__Issuer` | `expense-watch` |
| `Jwt:Audience` | `Jwt__Audience` | `expense-watch-web` |
| `Email:Host` | `Email__Host` | Empty; presenter supplies relay |
| `Email:Port` | `Email__Port` | `587` |
| `Email:EnableSsl` | `Email__EnableSsl` | `true` (STARTTLS) |
| `Email:From` | `Email__From` | Empty; presenter supplies sender |
| `Email:Recipient` | `Email__Recipient` | Empty; presenter-controlled mailbox |

Credentials are read **only** from `Email__Username` and `Email__Password` in the
process environment (including values loaded from ignored `.env`), never from tracked JSON. Configure these locally without
putting values in source or captured command logs. A relay allowing anonymous
local submission needs no credentials. Missing configuration or a send failure
produces `failed` notification statuses without rolling back business state.
Each generated message is attempted independently; failed messages are not retried.

Startup creates `hackathon.db` in `src/Tectonic.API` through EF Core `EnsureCreated()`.
It does not migrate an existing schema. For a fresh demo or a schema change,
stop the application and explicitly recreate only its disposable demo database.
Do not delete unrelated data.
For the alternate scenario, keep the main history and select a fresh separate
file instead:

```sh
ConnectionStrings__Expenses='Data Source=hackathon-alt.db' \
  dotnet run --no-build --configuration Release --no-launch-profile --urls http://localhost:5000
```

SQLite demo files and their WAL/SHM companions are ignored. Startup never erases
data or resets the clock. After the main walkthrough, stop and restart with the
same database: April 1, three transactions and three notifications should remain.
The prototype provides no schema migration or backward compatibility mechanism.

First obtain a token with signup/login and set `ACCESS_TOKEN` in your local shell
without logging it. All six walkthrough steps require its bearer header.
Submit the first transaction as:

```sh
curl -X PUT http://localhost:5000/api/demo/date \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"date":"2026-01-05"}'

curl -X POST http://localhost:5000/api/transactions \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"type":"expense","companyId":"example-telecom","companyName":"Example Telecom","expenseKey":"home-internet","description":"Monthly internet plan","amount":45.00,"currency":"EUR","date":"2026-01-05"}'
```

### Container run with Podman

From the repository root, build the API image:

```sh
podman build -f src/Tectonic.API/Dockerfile -t localhost/tectonic-api:dev .
```

The Dockerfile uses the official .NET 10 SDK for restore/publish and the ASP.NET
runtime for the final image. It runs as the non-root `app` user on port 8080.
Configuration file watching is disabled in the image; runtime environment and
appsettings are loaded at startup. This avoids inotify exhaustion on the host.
The root `.dockerignore` limits context to API files and excludes local build
output, `.env` files and SQLite databases. No signing key or credentials are
build arguments or image contents.

Use the existing root `.env` for runtime settings, and keep the same signing key
for container replacement so existing tokens remain usable. If not already
configured in this terminal, generate it once:

```sh
export Jwt__SigningKey="$(openssl rand -base64 32)"
```

For a new local demo pod with a Mailpit inbox (reuse an existing pod if already
created):

```sh
podman pod create --name tectonic-demo --network pasta \
  -p 127.0.0.1:5080:8080 -p 127.0.0.1:8025:8025
podman run -d --name tectonic-mailpit --pod tectonic-demo \
  docker.io/axllent/mailpit:v1.30.0
podman run -d --name tectonic-api --pod tectonic-demo \
  --env-file .env --env Jwt__SigningKey \
  --env Email__Host=localhost \
  --env Email__Port=1025 --env Email__EnableSsl=false \
  -v tectonic-api-pod-data:/data:U \
  localhost/tectonic-api:dev
```

The two containers share the pod's network namespace, so SMTP uses `localhost`
on port 1025. This avoids reliance on container-name DNS; `pasta` is the rootless
networking backend used here. Use [container Swagger](http://localhost:5080/swagger)
for signup/login and Authorize, and [Mailpit](http://localhost:8025) for emails.
The API and inbox are published only on loopback.

The named volume stores `/data/hackathon.db` and `/data/auth.db`; the image sets
both connection strings to these paths. Podman's `:U` gives this dedicated
volume the non-root application's ownership. Do not use `:U` on unrelated host
directories. Container replacement must reuse the same volume and signing key.
Existing databases on the host are not imported or erased.

Stop/start with `podman stop tectonic-api` and `podman start tectonic-api`.
For removal, stop then remove the named containers/pod; keep `tectonic-api-pod-data`
unless deliberately resetting all disposable users and expense history.
Docker can also build this Dockerfile with the same repository-root context;
the commands above and volume ownership option are for Podman.

The hosted inbox is [mailpit.openislamu.org](https://mailpit.openislamu.org/),
with HTTP/UI port 8025 behind its HTTPS proxy. This is separate from SMTP:
configure `Email__Host` with the SMTP hostname reachable by the deployed API,
`Email__Port` with the SMTP listener port (normally 1025), and
`Email__EnableSsl` for that listener. The public inbox URL does not establish
whether SMTP is exposed. The hosted SMTP endpoint is awaiting confirmation;
the local pod above is the verified setup.

Verified on 2026-09-30 with rootless Podman 6.1.2: the image built/published
successfully, ran as UID 1654, and used writable named-volume SQLite storage.
The final image contains the ASP.NET/.NET 10.0.12 runtimes, no SDK, local `.env`,
host databases or `bin`/`obj` directories. Signup/login, bearer rejection, all
six business routes and the full expense walkthrough worked through port 5080;
exactly three emails arrived in Mailpit. Actual API container replacement
preserved account login, the original JWT, April 1 clock, three transactions and
three sent notifications using the same runtime configuration and named volume.
Dockerfile language-server diagnostics were unavailable; Podman parsed and built
the Dockerfile successfully. No automated test infrastructure was added.

The initial named-bridge SMTP check exposed a host aardvark-dns startup failure;
direct SMTP by container IP succeeded. The documented shared pod avoids that
host DNS dependency. Its live data is in `tectonic-api-pod-data`; the earlier
failed-network check's separate `tectonic-api-data` volume was retained, not erased.

## Demo completion criteria

The implementation is complete when JSON ingestion and list endpoints work,
all three rules produce the expected emails, the logical clock demonstrates a
missing payment without waiting, and the final build and manual walkthrough
succeed. Verify actual receipt in the demo mailbox, not just a notification
record. No UI work or real KBC integration is required.

### Verification limits

Authentication verified on 2026-09-30: Release build with zero warnings/errors;
signup/login, duplicate/weak signup and required-field errors; identical unknown/
wrong-credential failures; all six business routes rejected absent, malformed,
tampered and unsigned tokens without state/email effects. Valid tokens preserved
the original expense walkthrough and three received Mailpit emails. Account
login, original unexpired tokens and expense records survived restart. A second
account saw the same mock dataset. Swagger Authorize, desktop/mobile rendering,
exact-origin CORS and missing/malformed/short signing-key startup failures were
observed. JWT claims showed HS256 and exactly 3,600 real UTC seconds of lifetime.
A validly signed expired-token rejection was not separately exercised; lifetime
validation and zero skew are configured. No production security assurance is claimed.

Verified on 2026-09-30: Release build with zero warnings/errors, all six Swagger
Try it out operations, desktop/mobile Swagger rendering, the six-step walkthrough
with three actual Mailpit emails, restart persistence, backward-clock rejection,
and an alternate database with independent keys, equal/cheaper payments and
March payments preventing removal. SMTP failure also preserved business state.
Automated tests are intentionally absent under the timeboxed policy, not reported
as passing. Rediscovery, year rollover and month-end clamping are implemented but
were not separately exercised in this walkthrough.
This is not production-ready banking software: concurrent requests,
replays, refunds, correction flows, multiple currencies and crash-safe email
delivery remain outside scope.
On workstations that have exhausted inotify instances, prefix the run command
with `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false` to disable configuration
file watchers. This does not disable loading `.env` at startup.

## References

- Shared design authority: `hackathon-stack-decisions.md`, read in full;
  especially sections 3 (stack), 4 (exclusions), 6 (hackathon contract), and 11
  (summary). It is an external planning document, not a file in this repository.
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview?view=aspnetcore-10.0)
- [ASP.NET Core endpoint configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0)
- Repository license: [GNU AGPL v3](LICENSE).

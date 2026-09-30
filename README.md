# Tectonic Team One — Expense Watch

Expense Watch turns fictional financial transactions into alerts about price
increases, newly recurring payments, and recurring payments that stop appearing.
It includes an authenticated API, a Blazor webapp, and an automatic transaction
simulator so several months of activity can be demonstrated in minutes.

**Hackathon prototype:** all financial data is invented. There is no bank
connection, payment execution, or claim of production readiness.

## What you can demonstrate

- **User-owned data:** separate transaction history, recurring streams,
  notifications, alert preferences, and logical date for each account.
- **Recurring expenses and income:** distinguish an internet bill from salary,
  even when different users submit identical stream identifiers.
- **Expense price increases:** a EUR 45 bill becoming EUR 49 produces an alert
  explaining the EUR 4 increase.
- **Missing payments:** evaluate a whole unpaid month before removing a recurring
  stream and notifying the user.
- **Configurable alerts:** enable or disable each condition independently. Email
  is implemented; SMS and in-app channels are unavailable lookup entries.
- **A continuous demo:** an in-process simulator makes real authenticated HTTP
  requests, generates fictional transactions, and advances its own account's date.

The webapp provides login/signup, transactions, recurring payments, notifications,
rules, and settings pages. Swagger offers an alternative way to operate the API.

## Start here

| Goal | Guide |
|---|---|
| Run the API and webapp locally | [Quick start](#quick-start) |
| Record a reliable demonstration | [Five-minute demo](#five-minute-demo) |
| Look up endpoints, JSON, or errors | [API reference](docs/API.md) |
| Configure email, simulation, or containers | [Operations guide](docs/OPERATIONS.md) |
| Understand the code and normalized model | [Technical guide](docs/TECHNICAL.md) |

## Quick start

### 1. Prepare the environment

Install the **.NET 10 SDK** and **OpenSSL**. Commands below use a Bash-compatible
shell and start at the repository root.

```sh
# Preserve an existing .env; copy the example only on the first run.
test -f .env || cp .env.example .env

export Jwt__SigningKey="$(openssl rand -base64 32)"
export Simulator__Enabled=false
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
```

Keep the signing key stable across API restarts. Generating another key invalidates
previous tokens. Store credentials only in process environment or the ignored
`.env`, never in source or screenshots.

Review `.env` before starting:

```dotenv
API_BASE_URL=http://127.0.0.1:5000/
API_USE_MOCK=false
```

The API URL **must end with `/`**. Real mode uses the API and its persistent data;
`API_USE_MOCK=true` uses canned webapp data without calling the API.

The environment example selects a hosted Mailpit demo inbox through
`Email__MailpitUrl`. Use only fictional data there. For a mailbox you control,
configure [local Mailpit or SMTP](docs/OPERATIONS.md#email-delivery).
Notifications go to the configured presenter mailbox, **not** the signup email.

### 2. Build both projects

```sh
dotnet build src/Tectonic.API/Tectonic.API.csproj --configuration Release
dotnet build src/Tectonic.Web/Tectonic.Web.csproj --configuration Release
```

### 3. Start the API

In the prepared terminal:

```sh
cd src/Tectonic.API
dotnet run --no-build --configuration Release --no-launch-profile --urls http://127.0.0.1:5000
```

The API initializes the integrated SQLite store and fixed lookup data. Missing or
invalid `Jwt__SigningKey` prevents startup.

### 4. Start the webapp

In a second terminal, starting at the repository root:

```sh
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
cd src/Tectonic.Web
dotnet run --no-build --configuration Release --no-launch-profile --urls http://127.0.0.1:5001
```

| Surface | Local URL |
|---|---|
| Webapp | <http://127.0.0.1:5001> |
| Swagger UI | <http://127.0.0.1:5000/swagger> |
| OpenAPI document | <http://127.0.0.1:5000/swagger/v1/swagger.json> |

Create a disposable account in the webapp or Swagger. Identity's default password
policy requires at least six characters including uppercase, lowercase, a digit,
and a non-alphanumeric character.

For Swagger, call signup/login and paste the returned `accessToken` into
**Authorize**. All business endpoints require bearer authentication.

## Five-minute demo

Use a **presenter account**, not the simulator account. Keep simulation disabled
for a controlled manual demonstration, or leave it running on its separate account.

The logical clock begins at `2026-01-01`. In Swagger, call
`PUT /api/demo/date` **before** posting each transaction. Transaction dates must
equal that account's current logical date.

Start with this expense:

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

| Step | Action | Expected result |
|---|---|---|
| 1 | Advance to Jan 5; submit the EUR 45 expense | Stored transaction, no alert |
| 2 | Advance to Feb 5; submit the **same stream keys**, amount 49, updated date | Price-increased and recurring-added alerts |
| 3 | Read `/api/recurring-transactions` | Active internet expense, next expected date March 5 |
| 4 | Advance to March 31 without a March payment | No missing-payment alert yet |
| 5 | Advance to April 1 | One recurring-missing alert naming March |
| 6 | Repeat April 1 | No duplicate alert |
| 7 | Inspect notifications and the configured Mailpit inbox | Delivery outcomes and received messages |

Use the same numeric day in consecutive months: Jan 5 and Feb 6 **do not**
establish recurrence. Different `transactionKey` values identify different streams.

To demonstrate income, use `type: "income"`, an employer counterparty,
`transactionKey: "monthly-salary"`, and equal-day Jan/Feb payments. Income can
become recurring but does not trigger the expense price-increase condition.

For user isolation, create a second account with identical stream keys: it starts
with its own empty history and January 1 clock. Disable a condition and show that
recurrence still updates while the corresponding alert is suppressed.

Use the current contract: `counterpartyKey`, `counterpartyName`, `transactionKey`,
and `/api/recurring-transactions`. The old company/expenseKey fields and
`/api/recurring-expenses` route are no longer the API contract.

## Enable the automatic simulator

Stop the API, then configure the **same terminal**:

```sh
export Simulator__Enabled=true
export Simulator__Password="aA1!$(openssl rand -hex 16)"
```

Start the API again with the command above. Generate this password **once** and
keep it for subsequent restarts; changing it does not reset the existing simulator
account's password.

The worker defaults to one simulated day every three seconds. It waits for the
API to start, authenticates, posts a day's transactions sequentially, then advances
its account's date. It resumes from persisted history without replaying existing
stream/date rows.

The scenario contains:

- Internet on day 5: EUR 45, then EUR 49, then a deliberately skipped month.
- Salary on day 25: EUR 2,500 monthly.
- A repeatable randomized daily purchase or bonus.

Watch `DayCompleted`, `TokenRenewed`, and `SimulatorStopped` logs. A failure stops
the worker without disabling the API. To return to manual-only operation, restart
with `Simulator__Enabled=false`. See [simulator settings](docs/OPERATIONS.md#simulator).

## Repository layout

```text
src/
  Tectonic.API/       Minimal API, Identity/JWT, SQLite, rules, SMTP and simulator
  Tectonic.Web/       Blazor Interactive Server webapp and real/mock API clients
docs/
  API.md             Endpoint and JSON reference
  OPERATIONS.md      Configuration, containers, email and recovery
  TECHNICAL.md       Architecture, data model and processing rules
.env.example         Non-secret configuration example
docker-compose.yml   Webapp-only Compose service
Dockerfile           Webapp image
PROJECT_BRIEFING.md  Presentation material; README/docs own the current contract
```

## Persistence and boundaries

- The API uses one `IdentityDbContext` and `tectonic.db`, normally in its working
  directory. A restart retains accounts and domain data.
- Legacy `auth.db` and `hackathon.db` are not imported or automatically deleted.
  Create an account in the integrated store.
- `EnsureCreated` initializes a fresh schema; it does not upgrade an existing one.
- Condition applicability is a normalized `ConditionTransactionType` join.
  There is no parsed string list or generic rules engine.
- Amounts are positive, two-decimal EUR values. Stream identity includes user,
  type, counterparty key, transaction key, and currency.
- Business data is saved before delivery. `sent` means transport acceptance;
  inspect the inbox to prove receipt. Failed delivery does not undo a transaction.
- The demo has no refresh-token, password-reset, verified-email, delivery-retry,
  or distributed-scheduling subsystem.
- Critical-payment flags are available only in the webapp's mock mode, not in
  the real API.

## Containers and troubleshooting

The root Compose file runs **only the webapp**. It needs a reachable API URL;
inside its container, `localhost` refers to that container, not the host API.
Use the [container guide](docs/OPERATIONS.md#containers) rather than assuming
`docker compose up` starts the whole application.

For startup errors, auth failures, missing emails, and simulator recovery, use
the [troubleshooting table](docs/OPERATIONS.md#troubleshooting).

## Verification

The API's native Release build and manual HTTP checks were recorded on
2026-09-30: user isolation, normalized lookups, alert preferences, expense/income
recurrence, month-end removal, duplicate rejection, and simulator renewal/restart.
These are recorded observations, not an automated coverage guarantee.

This documentation update checks source contracts, links, and formatting only;
it does not rerun product builds, browser QA, or container checks. Automated test
suites are intentionally absent under [the hackathon policy](AGENTS.md).

## License

GNU Affero General Public License v3. See [LICENSE](LICENSE).

# Operations and demo runbook

[README](../README.md) | [API reference](API.md) | [Architecture](TECHNICAL.md)

## Local startup

Install the **.NET 10 SDK** and **OpenSSL**. Start at the repository root.
The [environment example](../.env.example) targets the hosted/container demo;
native runs need a local database path and local API URL.

### Prepare and build

```sh
# Preserve an existing .env; copy the example only on the first run.
test -f .env || cp .env.example .env
openssl rand -base64 32
```

On first setup, paste the generated value into `AUTHENTICATION_LOCAL_JWT_KEY`
in the ignored `.env`. Keep that key stable across restarts; do not replace an
existing key just to launch the app. Never commit credentials.

```sh
dotnet build src/Tectonic.API/Tectonic.API.csproj --configuration Release
dotnet build src/Tectonic.Web/Tectonic.Web.csproj --configuration Release
```

Both projects load `.env` without replacing injected environment values.

### Start the API

In the first terminal, from the repository root:

```sh
export ASPNETCORE_ENVIRONMENT=Development
export DATABASE_CONNECTION_STRING="Data Source=tectonic.db"
export SIMULATOR_ENABLED=false
export SIMULATOR_BASE_URL=http://127.0.0.1:5000
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
cd src/Tectonic.API
dotnet run --no-build --configuration Release --no-launch-profile --urls http://127.0.0.1:5000
```

This starts a controlled manual demo. For automatic activity in every account,
set `SIMULATOR_ENABLED=true` and `SIMULATOR_ALL_USERS=true` before starting the
API, or stop it and restart with those settings. No simulator password is needed
in all-user mode.

The API's default database path is relative to its working directory; running
from `src/Tectonic.API` keeps `tectonic.db` there. It is not the container's
`/data/tectonic.db` path. A missing or invalid signing key prevents startup.

### Start the webapp

In a second terminal, from the repository root:

```sh
export ASPNETCORE_ENVIRONMENT=Development
export API_BASE_URL=http://127.0.0.1:5000/
export API_USE_MOCK=false
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
cd src/Tectonic.Web
dotnet run --no-build --configuration Release --no-launch-profile --urls http://127.0.0.1:5001
```

| Surface | Local URL |
|---|---|
| Webapp | <http://127.0.0.1:5001> |
| Swagger | <http://127.0.0.1:5000/swagger> |
| OpenAPI document | <http://127.0.0.1:5000/swagger/v1/swagger.json> |

The trailing slash in `API_BASE_URL` is required. Real mode uses API persistence;
`API_USE_MOCK=true` uses canned webapp data instead.

Create a disposable account. Identity requires at least six password characters,
including uppercase, lowercase, a digit, and a non-alphanumeric character; the
webapp's signup form requires at least eight. In Swagger, log in or sign up and
paste the returned `accessToken` into **Authorize**. Business endpoints require
bearer authentication.

The example sends email to the hosted Mailpit demo inbox. Use fictional data
only, or configure [a local inbox](#local-mailpit).

## Controlled five-minute demo

Stop automatic simulation before controlling an account's clock:
`SIMULATOR_ENABLED=false`. A new account starts at `2026-01-01`.
In Swagger, call `PUT /api/demo/date` before each transaction; its date must
match the account's current logical date.

Use this expense with the transaction endpoint:

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
| 2 | Advance to Feb 5; submit the same stream keys, amount 49, updated date | Price-increased and recurring-added alerts |
| 3 | Read `/api/recurring-transactions` | Active internet expense, next expected date March 5 |
| 4 | Advance to March 31 without a March payment | No missing-payment alert yet |
| 5 | Advance to April 1 | One recurring-missing alert naming March |
| 6 | Repeat April 1 | No duplicate alert |
| 7 | Inspect notifications and the configured Mailpit inbox | Delivery outcomes and received messages |

Match the numeric day in consecutive months: Jan 5 and Feb 6 do not establish
recurrence. Different `transactionKey` values identify different streams.
For salary, use `type: "income"`, an employer counterparty, a stable
`monthly-salary` key, and equal-day January/February payments. Income becomes
recurring but does not trigger expense price-increase alerts.

For isolation, create another account with identical stream keys; its history,
preferences, notifications and clock remain separate. Disabling a condition
suppresses its alerts without disabling recurrence tracking. See the
[API reference](API.md) for the complete contract.

## Configuration reference

The shared `.env` uses SCREAMING_SNAKE_CASE. Both applications load it locally
without replacing injected environment values. Uppercase application settings
take precedence over JSON defaults and older namespaced settings.

| Key | Meaning |
|---|---|
| `DATABASE_CONNECTION_STRING` | Integrated SQLite connection; default `Data Source=tectonic.db` |
| `AUTHENTICATION_LOCAL_JWT_KEY` | Base64 of at least 32 random bytes; required to start API |
| `AUTHENTICATION_LOCAL_JWT_ISSUER` / `AUTHENTICATION_LOCAL_JWT_AUDIENCE` | Defaults `expense-watch` / `expense-watch-web` |
| `WEB_APP_ORIGIN` | Optional exact origin for direct browser API calls |
| `API_BASE_URL` | Webapp's API URL; required in real mode, trailing slash mandatory |
| `API_USE_MOCK` | Webapp canned-data mode; false for API integration |
| `WEB_PORT` | Root Compose web host port; default 8080 |
| `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE` | Set false for a stable demo or exhausted inotify quota |

`.env.example` includes email, simulator, JWT issuer/audience, database, listener,
and optional development demo-login settings. Empty SMTP credentials are valid
for the hosted Mailpit HTTP transport. A simulator password is needed when
`SIMULATOR_ENABLED=true` and `SIMULATOR_ALL_USERS=false`.

### Coolify: same environment for API and Blazor

For the complete Docker Image deployment walkthrough, including image tags,
DNS, port 8080, and both persistent volumes, follow the
[Coolify deployment guide](COOLIFY%20DEPLOYMENT%20GUIDE.md). The checklist below
summarizes how the shared environment settings apply.

1. Use the ignored root `.env`, not `.env.example`: the example deliberately
   contains no secrets. Generate a stable `AUTHENTICATION_LOCAL_JWT_KEY` using
   `openssl rand -base64 32` if the private file does not already contain one.
2. Set `API_BASE_URL` to the API's HTTPS domain or Docker-network URL reachable
   from the Blazor container, with a trailing `/`. Do not use loopback for
   communication between separate containers.
3. Paste the whole `.env` into both Coolify applications' runtime environment.
   Unused application settings are ignored. Blazor uses the JWT returned by API
   login; it does not sign tokens with the shared key.
4. Set each application's container port to **8080** and mount persistent
   writable storage at **/data** on the API. The shared database setting is
   `Data Source=/data/tectonic.db`. Both containers can use 8080 independently.
5. Redeploy images containing these configuration changes. For automatic
   transactions in every account, set `SIMULATOR_ENABLED=true`,
   `SIMULATOR_ALL_USERS=true`, and `SIMULATOR_INTERVAL_SECONDS=3`.
   No simulator password is needed in this mode. Its internal loopback URL stays
   `SIMULATOR_BASE_URL=http://127.0.0.1:8080`, not the public API domain.
   Set `SIMULATOR_ENABLED=false` for manual demos.

For local API runs on port 5000, override `DATABASE_CONNECTION_STRING` with
`Data Source=tectonic.db` and `SIMULATOR_BASE_URL` with
`http://127.0.0.1:5000`. `WEB_APP_ORIGIN` is optional: Blazor Server calls the API
server-side and does not require browser CORS.

### Secrets and restarts

Keep signing key and simulator password in process environment or ignored `.env`.
Do not include them in screenshots, checked-in examples, response logs or slides.
Changing the signing key invalidates existing JWTs; users must log in again.
Changing the simulator environment password does not update its stored Identity
password. Keep it stable for restart/resume.

## Email delivery

All notifications use the configured presenter recipient. Signup email is not
delivery authority.

### Local Mailpit

Start Mailpit in its own terminal:

```sh
mailpit --smtp 127.0.0.1:1025 --listen 127.0.0.1:8025
```

For SMTP delivery, inject:

```sh
export EMAIL_MAILPIT_URL=""
export EMAIL_HOST=127.0.0.1
export EMAIL_PORT=1025
export EMAIL_ENABLE_SSL=false
export EMAIL_FROM=expense-watch@example.test
export EMAIL_RECIPIENT=presenter@example.test
```

Then start the API. Open <http://127.0.0.1:8025> to inspect received messages.

For HTTP delivery to that inbox instead, set:

```sh
export EMAIL_MAILPIT_URL=http://127.0.0.1:8025
```

A nonempty Mailpit URL selects its `/api/v1/send` HTTP endpoint rather than SMTP.
The repository's environment example uses a hosted Mailpit demo service; that
inbox is for invented demo data, not real financial records.

### SMTP relay

Set `EMAIL_HOST`, `EMAIL_PORT`, `EMAIL_ENABLE_SSL`, `EMAIL_FROM` and
`EMAIL_RECIPIENT`. If authentication is needed, inject `EMAIL_USERNAME` and
`EMAIL_PASSWORD`; do not commit them. Leave `EMAIL_MAILPIT_URL` empty.
SMTP uses a bounded timeout. Each notification's outcome is persisted separately.

`pending` can remain after a crash. `failed` does not undo the transaction or
clock change. No automatic retry or replay is performed.

## Simulator

| Key | Default |
|---|---|
| `SIMULATOR_ENABLED` | true |
| `SIMULATOR_ALL_USERS` | false; opt-in generation for every account |
| `SIMULATOR_INTERVAL_SECONDS` | 3 |
| `SIMULATOR_BASE_URL` | `http://127.0.0.1:5000` |
| `SIMULATOR_EMAIL` | `simulator@example.test` |
| `SIMULATOR_PASSWORD` | No default; injected disposable account password |
| `SIMULATOR_RENEW_BEFORE_EXPIRY_SECONDS` | 60 |

The interval must be a positive supported integer. Renewal lead must be greater
than zero and less than 3600 seconds. The base URL must be a loopback HTTP(S)
origin without credentials, path suffix, query or fragment, matching the listener.
Do not disable certificate validation for an HTTPS listener.

### What one tick does

1. Renew the JWT if needed.
2. Read the simulator account's persisted date and that day's transactions.
3. Generate a repeatable daily batch, skipping existing stream/date identities.
4. Await each real `POST /api/transactions`.
5. Advance its clock one day only after all submissions succeed.

The worker waits for `ApplicationStarted`, never overlaps batches, and cancels
on shutdown. It uses login or one signup attempt; incorrect existing credentials
stop it rather than reset the account.

For automatic transactions in newly created real accounts, set
`SIMULATOR_ENABLED=true` and `SIMULATOR_ALL_USERS=true`. Each tick discovers
accounts, issues a user-scoped token, and runs the same authenticated HTTP
transaction/clock flow independently for each account. No user passwords are
retained. This mode advances existing accounts too; do not manually advance or
post transactions while it owns their clocks. Disable simulation for a controlled
manual demo. The default single-account mode still uses the configured simulator
credentials.
If one account's request fails, only that account pauses until API restart;
other accounts continue. Restart reads persisted history and skips stored rows,
rather than immediately replaying an uncertain write.
The real-mode Transactions page refreshes its history and logical date every
three seconds while open, so new persisted transactions appear without reloading
the browser.

For local runs, use `DATABASE_CONNECTION_STRING=Data Source=tectonic.db`,
`API_BASE_URL=http://127.0.0.1:5000/`, and
`SIMULATOR_BASE_URL=http://127.0.0.1:5000`. If the hosted Mailpit domain's IPv6
address is unreachable while IPv4 works, launch the API with
`DOTNET_SYSTEM_NET_DISABLEIPV6=1`; keep certificate validation enabled.

### Guaranteed scenario

| Stream | Schedule |
|---|---|
| Telecom / `home-internet` | Day 5; first month of Jan/Apr/Jul/Oct cycle EUR 45, second EUR 49, third skipped |
| Employer / `monthly-salary` | Day 25, EUR 2,500 monthly |
| Daily purchase/bonus | Date-specific key, ordinary seeded randomness and integer-cent amount |

One-off selection is roughly 75% expense/25% income; scheduled payments mean the
overall mix is not an exact ratio. The fixed spine guarantees observable
recurrence, increase and missing-month cases.

### Recording and recovery

In single-account mode, use a separate presenter account or restart with
`SIMULATOR_ENABLED=false`.
Never manually advance/post on the simulator identity while it is active.
Other users have independent clocks.

To view single-account simulation in the real webapp, use its account, not a
presenter account. All-user mode generates payments for each account instead.
In Development, opt in to `SIMULATOR_DEMO_LOGIN_ENABLED=true`
and inject `SAMPLE_ACCOUNT_NAME`, `SAMPLE_ACCOUNT_EMAIL` and
`SAMPLE_ACCOUNT_PASSWORD` into the webapp process. The email and password must
match the API's simulator credentials. The login page's **Use demo account**
button then authenticates through the real API. This opt-in is ignored outside
Development. Keep the password in process environment or ignored `.env`.

The real-mode payments page updates every three seconds. Click **Refresh
payments** for an immediate update of rows and the current logical date. Simulator
emails are sent when an enabled alert condition fires, not for every transaction;
the first telecom increase and recurrence alerts occur on February 5.

Watch `DayCompleted`, `TokenRenewed` and `SimulatorStopped`.
Missing configuration, failed authentication, timeout or uncertain HTTP outcome
stops the worker while leaving the API usable. Fix the cause and restart with the
same database and credentials; persisted history determines what is skipped.
Do not blindly resubmit a day or delete a DB to recover a failed request.

## Containers

There are two existing image entry points:

- `src/Tectonic.Web/Dockerfile`: **webapp**, listens on 8080.
- `src/Tectonic.API/Dockerfile`: **API**, listens on 8080 inside its container,
  writes `/data/tectonic.db`, and points simulator loopback at port 8080.

The root `docker-compose.yml` runs **only the webapp**. It does not create the API
or Mailpit. Configure `API_BASE_URL` to an API reachable from that web container.
`127.0.0.1` there is the web container itself.

For two images on one Docker network, from the repository root:

```sh
docker build -f src/Tectonic.API/Dockerfile -t expense-watch-api .
docker build -f src/Tectonic.Web/Dockerfile -t expense-watch-web .
docker network create expense-watch-demo
docker volume create expense-watch-data

# Inject AUTHENTICATION_LOCAL_JWT_KEY in this shell first, as in the README.
docker run --name expense-watch-api --network expense-watch-demo \
  -p 127.0.0.1:5000:8080 \
  --env-file .env -e AUTHENTICATION_LOCAL_JWT_KEY -e SIMULATOR_ENABLED=false \
  -v expense-watch-data:/data -d expense-watch-api

docker run --name expense-watch-web --network expense-watch-demo \
  -p 127.0.0.1:8080:8080 \
  -e API_BASE_URL=http://expense-watch-api:8080/ \
  --env-file .env -e API_USE_MOCK=false -d expense-watch-web
```

This example assumes first-time network/container names and the example's
reachable hosted Mailpit URL. Configure a container-reachable SMTP/Mailpit host
if using local capture; container `localhost` is not the host mailbox.
For simulation, inject the password and enable switch into the API container,
not the web container. The API image already selects the correct internal URL.
Mount persistent writable storage at `/data`; do not replace it casually.

These are documented deployment commands, not a claim that this documentation
update built or ran either image.

## Persistence and schema

`tectonic.db` owns accounts, roles and all domain records. Old `auth.db` and
`hackathon.db` remain separate legacy files and are not imported.
`EnsureCreated` creates a fresh schema, not migrations for an existing database.

For an explicitly chosen fresh demo:

1. Stop the API and simulator.
2. Identify the exact configured database and preserve anything needed.
3. Choose a new SQLite filename through `DATABASE_CONNECTION_STRING`.
4. Start the API and sign up again.

Using a new filename avoids accidental deletion. Do not remove another user's
database or an entire container volume to repair a routine login/config problem.

## Troubleshooting

| Symptom | Check / action |
|---|---|
| API fails on signing key | Inject valid Base64 decoding to at least 32 bytes |
| Config watcher/inotify error | Set `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false`; no system-limit edit is needed |
| Webapp fails on API URL | Set trailing-slash `API_BASE_URL`; confirm real/mock mode |
| Webapp cannot reach API in container | Use a reachable hostname/network, not web-container localhost |
| 401 after restart | Signing key changed, token expired, or account exists only in the legacy store; log in/sign up for current store |
| Transaction date returns 400 | Advance that user's clock first; use its current date |
| Repeated transaction returns 409 | That stream/date already exists; inspect history instead of replaying |
| No recurrence | Check owner/type/counterparty/key/currency and same numeric day in consecutive months |
| No missing alert on March 31 | Correct: March must end first; advance to April 1 |
| Alert disabled but recurring list changes | Correct: preference controls notification, not state tracking |
| Email failed or absent | Inspect notification outcome, Mailpit URL vs SMTP selection, recipient and transport reachability |
| Simulator stops at login | Keep its original password; changing an environment variable does not reset Identity |
| No simulator activity | Check enabled flag, password, loopback URL and `SimulatorStopped` reason |
| Critical flag unsupported | Feature is mock-only, not a real API endpoint |

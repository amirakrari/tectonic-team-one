# Operations and demo runbook

[README](../README.md) | [API reference](API.md) | [Architecture](TECHNICAL.md)

## Local startup

Use the [README quick start](../README.md#quick-start) for the two-terminal API/
webapp run. Commands there deliberately pin separate ports and working directories.
Both projects use DotNetEnv without replacing injected environment values.

The API's default database path is relative to its process working directory.
Run from `src/Tectonic.API`, not the repository root, when using that default.

## Configuration reference

Environment settings use ASP.NET's `__` separator. Webapp flat settings
`API_BASE_URL` and `API_USE_MOCK` are also supported.

| Key | Meaning |
|---|---|
| `ConnectionStrings__Application` | Integrated SQLite connection; default `Data Source=tectonic.db` |
| `Jwt__SigningKey` | Base64 of at least 32 random bytes; required to start API |
| `Jwt__Issuer` / `Jwt__Audience` | Defaults `expense-watch` / `expense-watch-web` |
| `WebAppOrigin` | Optional exact origin for direct browser API calls |
| `API_BASE_URL` or `Api__BaseUrl` | Webapp's API URL; required in real mode, trailing slash mandatory |
| `API_USE_MOCK` or `Api__UseMock` | Webapp canned-data mode; false for API integration |
| `WEB_PORT` | Root Compose web host port; default 8080 |
| `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE` | Set false for a stable demo or exhausted inotify quota |

Choose one naming form for the web settings. Namespaced values take precedence
when both forms are supplied.

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
export Email__MailpitUrl=""
export Email__Host=127.0.0.1
export Email__Port=1025
export Email__EnableSsl=false
export Email__From=expense-watch@example.test
export Email__Recipient=presenter@example.test
```

Then start the API. Open <http://127.0.0.1:8025> to inspect received messages.

For HTTP delivery to that inbox instead, set:

```sh
export Email__MailpitUrl=http://127.0.0.1:8025
```

A nonempty Mailpit URL selects its `/api/v1/send` HTTP endpoint rather than SMTP.
The repository's environment example uses a hosted Mailpit demo service; that
inbox is for invented demo data, not real financial records.

### SMTP relay

Set `Email__Host`, `Email__Port`, `Email__EnableSsl`, `Email__From` and
`Email__Recipient`. If authentication is needed, inject `Email__Username` and
`Email__Password`; do not commit them. Leave `Email__MailpitUrl` empty.
SMTP uses a bounded timeout. Each notification's outcome is persisted separately.

`pending` can remain after a crash. `failed` does not undo the transaction or
clock change. No automatic retry or replay is performed.

## Simulator

| Key | Default |
|---|---|
| `Simulator__Enabled` | true |
| `Simulator__IntervalSeconds` | 3 |
| `Simulator__BaseUrl` | `http://127.0.0.1:5000` |
| `Simulator__Email` | `simulator@example.test` |
| `Simulator__Password` | No default; injected disposable account password |
| `Simulator__RenewBeforeExpirySeconds` | 60 |

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

Use a separate presenter account or restart with `Simulator__Enabled=false`.
Never manually advance/post on the simulator identity while it is active.
Other users have independent clocks.

Watch `DayCompleted`, `TokenRenewed` and `SimulatorStopped`.
Missing configuration, failed authentication, timeout or uncertain HTTP outcome
stops the worker while leaving the API usable. Fix the cause and restart with the
same database and credentials; persisted history determines what is skipped.
Do not blindly resubmit a day or delete a DB to recover a failed request.

## Containers

There are two existing image entry points:

- Root `Dockerfile`: **webapp**, listens on 8080.
- `src/Tectonic.API/Dockerfile`: **API**, listens on 8080 inside its container,
  writes `/data/tectonic.db`, and points simulator loopback at port 8080.

The root `docker-compose.yml` runs **only the webapp**. It does not create the API
or Mailpit. Configure `API_BASE_URL` to an API reachable from that web container.
`127.0.0.1` there is the web container itself.

For two images on one Docker network, from the repository root:

```sh
docker build -f src/Tectonic.API/Dockerfile -t expense-watch-api .
docker build -t expense-watch-web .
docker network create expense-watch-demo
docker volume create expense-watch-data

# Inject Jwt__SigningKey in this shell first, as in the README.
docker run --name expense-watch-api --network expense-watch-demo \
  -p 127.0.0.1:5000:8080 \
  --env-file .env -e Jwt__SigningKey -e Simulator__Enabled=false \
  -v expense-watch-data:/data -d expense-watch-api

docker run --name expense-watch-web --network expense-watch-demo \
  -p 127.0.0.1:8080:8080 \
  -e Api__BaseUrl=http://expense-watch-api:8080/ \
  -e Api__UseMock=false -d expense-watch-web
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
3. Choose a new SQLite filename through `ConnectionStrings__Application`.
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

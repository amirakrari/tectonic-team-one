# Coolify deployment guide

[README](../README.md) | [Operations](OPERATIONS.md) | [Environment example](../.env.example)

Deploy Expense Watch as two Docker Image resources in one Coolify project:
an API with persistent SQLite data and a Blazor webapp with persistent
Data Protection keys. Use our public, prebuilt images from GitHub Container
Registry (GHCR); no GitHub account, access token, registry login, or local image
build is required. This guide pins the versions used for the hackathon deployment.

## Deployment overview

| Setting | API | Blazor webapp |
|---|---|---|
| Docker image | `ghcr.io/amirakrari/tectonic-api` | `ghcr.io/amirakrari/tectonic-web` |
| Image tag | `20260930-231050` | `20260930-231326` |
| Ports Exposes | `8080` | `8080` |
| Volume name | `tectonic-data` | `blazor-data-protection` |
| Destination Path | `/data` | `/root/.aspnet/DataProtection-Keys` |
| Team's live address | <https://tectonicapi.openislamu.org/> | <https://tectonicweb.openislamu.org/> |

Both containers use port 8080 independently. Coolify's proxy serves their public
HTTPS domains; you do not need to publish port 8080 directly on the server.

**For your own deployment, use domains you own.** The team's addresses show
the working topology, not domain names you should claim. Replace the webapp's
`API_BASE_URL` with your API's public HTTPS address. Keep the API's
`SIMULATOR_BASE_URL=http://127.0.0.1:8080` unchanged.

## 1. Prepare your server, domains, and image access

You need:

- A working Coolify installation with your deployment server connected.
- Permission to create projects and resources on that server.
- A domain and access to its DNS zone.
- Internet access from the deployment server to `ghcr.io`.
- A generated JWT signing key for your API.
- A reachable Mailpit instance or SMTP relay for email.

### Use our public GHCR images

Both images are public and can be pulled anonymously:

- API: `ghcr.io/amirakrari/tectonic-api:20260930-231050`
- Blazor webapp: `ghcr.io/amirakrari/tectonic-web:20260930-231326`

Select **Docker Image** in Coolify and enter the image name and tag shown in
the overview. Leave registry credentials unset. Coolify pulls these images
directly from our GHCR packages; you do not need to clone the repository, build
the images, or authenticate to GitHub.

### Generate the API signing key

Run this once on a trusted machine:

```sh
openssl rand -base64 32
```

Keep the result private. You will paste it into the API's environment variables.
Use your own generated key, not another deployment's key. Keep it stable across
redeployments; changing it invalidates existing login tokens.

## 2. Create the Coolify project

1. Open **Projects** in Coolify.
2. Choose **Create New Project** or **Add**, and name it `Expense Watch`.
3. Open the project's deployment environment.
4. Choose **Add Resource** and select the connected deployment server.
5. Select **Docker Image** for the first resource. This will be the API.

You will add the webapp as a second resource in the same project after the API
is reachable.

## 3. Configure the API resource

Name the resource `tectonic-api` so it is easy to identify in deployments and
logs.

### Image and networking

Set the image fields separately:

| Field | Value |
|---|---|
| Docker Image | `ghcr.io/amirakrari/tectonic-api` |
| Tag | `20260930-231050` |
| Networking > Ports Exposes | `8080` |

**Replace the default exposed port 80 with 8080.** The API listens on port 8080
inside its container. An incorrect proxy target can produce a bad gateway even
when the container has started.

Save the configuration. When using a Coolify-managed domain, leave direct
host **Ports Mappings** unset unless you deliberately need an additional
host-port mapping.

### Environment variables

1. Open **Environment Variables**.
2. Switch to **Developer View**.
3. Paste the block below.
4. Replace `REPLACE_WITH_GENERATED_BASE64_KEY` with the key you generated.
5. Adjust email settings for your chosen inbox or provider.
6. Save the variables and make sure they are available at **runtime**.

```dotenv
ASPNETCORE_ENVIRONMENT=Production
DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
API_USE_MOCK=false
AUTHENTICATION_LOCAL_JWT_KEY=REPLACE_WITH_GENERATED_BASE64_KEY
AUTHENTICATION_LOCAL_JWT_ISSUER=expense-watch
AUTHENTICATION_LOCAL_JWT_AUDIENCE=expense-watch-web
DATABASE_CONNECTION_STRING=Data Source=/data/tectonic.db
EMAIL_MAILPIT_URL=https://mailpit.openislamu.org
EMAIL_FROM=expense-watch@example.test
EMAIL_RECIPIENT=presenter@example.test
SIMULATOR_ENABLED=true
SIMULATOR_ALL_USERS=true
SIMULATOR_INTERVAL_SECONDS=3
SIMULATOR_BASE_URL=http://127.0.0.1:8080
```

The signing-key placeholder is not a valid key. Replace it before deployment.
When editing an existing resource, preserve any other variables it needs:
Developer View saves the managed variable list, not just the lines you changed.

`API_USE_MOCK` controls Blazor, not the API. It is included here to match the
shared deployment configuration; authentication and simulation on the API do
not depend on that setting.

#### Why the simulator URL is internal

The simulator runs **inside the API container** and calls that same container:

```dotenv
SIMULATOR_BASE_URL=http://127.0.0.1:8080
```

This is not the webapp's API URL. Do not replace it with the public API domain,
the Blazor domain, or a port from your local development machine.

With `SIMULATOR_ENABLED=true` and `SIMULATOR_ALL_USERS=true`, each account,
including new signups, receives fictional transactions through the real
authenticated API. Data persists in SQLite, and each account has its own logical
clock. No `SIMULATOR_PASSWORD` is required in this mode.

Existing accounts advance too. Do not manually post transactions or change
their dates while automatic simulation owns those clocks. For a controlled
manual demo, set `SIMULATOR_ENABLED=false` and redeploy the API.

#### Choose your email transport

The block above uses the team's hosted Mailpit demo inbox:
<https://mailpit.openislamu.org>. Use fictional data there. Alerts are sent to
`EMAIL_RECIPIENT`, not to the email address used to create an account.

You can instead:

- **Deploy your own Mailpit:** create a separate Mailpit service, make its HTTP
  interface reachable from the API container, and set `EMAIL_MAILPIT_URL` to
  that address. Open your own inbox to inspect received messages.
- **Use SMTP:** leave `EMAIL_MAILPIT_URL` empty and configure `EMAIL_HOST`,
  `EMAIL_PORT`, `EMAIL_ENABLE_SSL`, `EMAIL_FROM`, `EMAIL_RECIPIENT`, and, if
  required, `EMAIL_USERNAME` and `EMAIL_PASSWORD`.

A nonempty `EMAIL_MAILPIT_URL` selects HTTP delivery; SMTP host, port, and
credentials are not used in that mode. A separate Mailpit container is not
`localhost` from the API container.

##### SMTP relay configuration

To use an SMTP provider instead of Mailpit, replace the `EMAIL_*` lines in
the API environment block above with this complete email configuration.
**Keep the API's other runtime, signing-key, database, and simulator variables.**
Do not overwrite the whole Developer View with only these email lines.

```dotenv
EMAIL_MAILPIT_URL=
EMAIL_HOST=smtp.example.com
EMAIL_PORT=587
EMAIL_ENABLE_SSL=true
EMAIL_FROM=expense-watch@example.com
EMAIL_RECIPIENT=you@example.com
EMAIL_USERNAME=REPLACE_WITH_SMTP_USERNAME
EMAIL_PASSWORD=REPLACE_WITH_SMTP_PASSWORD
```

Replace the example hostname, addresses, and credential placeholders before
deploying:

| Variable | SMTP configuration |
|---|---|
| `EMAIL_MAILPIT_URL` | Keep empty. A nonempty value selects Mailpit HTTP even when SMTP settings are present. |
| `EMAIL_HOST` | Your provider's SMTP hostname, reachable from the API container. |
| `EMAIL_PORT` | Your provider's SMTP submission port; the example uses 587. |
| `EMAIL_ENABLE_SSL` | `true` requires a STARTTLS-capable SMTP server. The current client does not support implicit TLS/SMTPS on port 465. |
| `EMAIL_FROM` | A sender address approved by your SMTP provider. |
| `EMAIL_RECIPIENT` | The inbox that receives all alert emails; this is not automatically the signup address. |
| `EMAIL_USERNAME` | Your provider's SMTP username. Leave empty only when the relay does not require authentication. |
| `EMAIL_PASSWORD` | Your provider's SMTP password, token, or app password. Leave empty when using an unauthenticated relay. |

Keep actual credentials in Coolify's **API runtime environment**, not in
`.env.example`, screenshots, or the webapp's configuration. Save the changes and
redeploy the API. The webapp does not send email and needs no email variables.

For a local or separate Mailpit **SMTP** service, clear `EMAIL_MAILPIT_URL`,
use its reachable hostname, set `EMAIL_PORT=1025` and `EMAIL_ENABLE_SSL=false`,
and leave both credentials empty. Use those plaintext settings only for that
controlled Mailpit service, not for a public SMTP provider.

After deployment, trigger an enabled alert, check the API's SMTP delivery logs,
and inspect the configured recipient's inbox. `sent` means the SMTP server
accepted the message, not proof it reached the inbox. Failed notifications are
not automatically retried or replayed when you change email settings.

See [`.env.example`](../.env.example) and the
[email delivery guide](OPERATIONS.md#email-delivery) for the settings and local
Mailpit/SMTP examples. The two application images do not include a Mailpit
server.

### Persistent SQLite storage

1. Open **Persistent Storage**.
2. Choose **Add Volume Mount**.
3. Select a named volume rather than a host-directory bind mount.
4. Enter:

   | Field | Value |
   |---|---|
   | Name | `tectonic-data` |
   | Destination Path | `/data` |

5. Save the volume mount. If a **Source Path** field is shown, leave it empty
   for a Coolify-managed named volume.

The connection string points to `/data/tectonic.db`. This volume retains
accounts, transactions, preferences, notifications, and logical clocks across
container replacement. Keep it attached when updating the image.

The API runs as a non-root container user, so the mounted directory must be
writable by that user. If startup reports a SQLite permission error, check the
volume's ownership rather than removing the database.

## 4. Configure DNS and the API domain

For self-hosting, choose two subdomains, for example `api.example.com` and
`app.example.com`, under a domain you control.

In your DNS provider's zone, create records for both names:

| Type | Name | Value | TTL |
|---|---|---|---|
| A | `api` | Your deployment server's public IPv4 address | 300 seconds / 5 minutes, if available |
| A | `app` | Your deployment server's public IPv4 address | 300 seconds / 5 minutes, if available |
| AAAA, optional | `api` | Your deployment server's public IPv6 address | 300 seconds / 5 minutes, if available |
| AAAA, optional | `app` | Your deployment server's public IPv6 address | 300 seconds / 5 minutes, if available |

An **A** record contains IPv4; an **AAAA** record contains IPv6. Add AAAA only
when the server and proxy are reachable over that IPv6 address. Otherwise,
clients or certificate checks may reach a nonworking address.

Some providers expect the short name (`api`); others expect the full hostname.
Use the format your DNS provider requests. TTL controls caching, not guaranteed
propagation time. Existing cached records may remain until their previous TTL
expires.

Ensure public traffic can reach Coolify's proxy on ports **80 and 443**.
Keep **Ports Exposes = 8080** on the API resource; the proxy handles the public
HTTPS connection.

Back in the API resource:

1. Open **General** or the **Domains** field.
2. Enter your API domain with `https://`, such as `https://api.example.com`.
3. Save.

The team's deployment uses `https://tectonicapi.openislamu.org`. With 8080 as the
first exposed port, Coolify routes the domain to the API container on that port.
The public URL does not need an `:8080` suffix.

## 5. Deploy and verify the API

1. Confirm the image, tag, port, environment variables, volume, and domain
   have all been saved.
2. Open **Actions** at the top right and choose **Deploy**. Some Coolify versions
   also show a direct **Deploy** button.
3. Follow the deployment output until the image has been pulled and the
   container is running.
4. Check application logs for `Now listening on` with port 8080.
5. Open your API's `/swagger/` URL.

For the team's deployment:
<https://tectonicapi.openislamu.org/swagger/>.

A rendered Swagger page confirms the API's public route. Test signup/login in
Swagger, then use **Authorize** with the returned `accessToken` to call protected
endpoints. Do not publish tokens or passwords in screenshots.

If an enabled health check requests `/`, it may fail because the API has no
root homepage. Use an existing unauthenticated endpoint such as
`/swagger/v1/swagger.json`, on port 8080.

Deploy the webapp only after the API's public HTTPS endpoint is reachable.

## 6. Create and configure the Blazor resource

In the same Coolify project and environment:

1. Choose **Add Resource**.
2. Select the deployment server and **Docker Image**.
3. Name the resource `tectonic-web`.
4. Set:

   | Field | Value |
   |---|---|
   | Docker Image | `ghcr.io/amirakrari/tectonic-web` |
   | Tag | `20260930-231326` |
   | Networking > Ports Exposes | `8080` |

5. Save. Change the default port 80 to **8080**, just as for the API.

### Environment variables

Open **Environment Variables**, switch to **Developer View**, and paste:

```dotenv
ASPNETCORE_ENVIRONMENT=Production
DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
API_BASE_URL=https://tectonicapi.openislamu.org/
API_USE_MOCK=false
```

**Before saving, replace `API_BASE_URL` with your own deployed API address.**
For example, if your API domain is `https://api.example.com`, use:

```dotenv
API_BASE_URL=https://api.example.com/
```

Keep the trailing `/`. This must be reachable from the Blazor container.
Do not use the webapp domain or `http://127.0.0.1:8080/` here: inside Blazor,
loopback points to Blazor itself, not the API.

Save the variables with runtime availability enabled. Leave
`API_USE_MOCK=false` so signup, login, transactions, and notifications use the
real API. Blazor receives JWTs from API login and does not need the API's signing
key.

### Persist the Data Protection keys

1. Open **Persistent Storage**.
2. Choose **Add Volume Mount** and use a named volume.
3. Enter these values:

   | Field | Value |
   |---|---|
   | Name | `blazor-data-protection` |
   | Destination Path | `/root/.aspnet/DataProtection-Keys` |

4. Leave **Source Path** empty if shown for the named volume.
5. Save.

Use the destination path **exactly as written**, including capitalization.
The supplied web image uses this default ASP.NET Core Data Protection location.
Persisting these keys allows protected browser-session data to remain readable
after container replacement. It does not extend the API token's expiry.

This is a separate volume from `tectonic-data`. Do not put the API database in
the webapp's key directory or share one mount between the two resources.

### Domain and deployment

1. Confirm the webapp's DNS record from step 4 points to the deployment server.
2. Set its **Domains** field to your web address, such as
   `https://app.example.com`.
3. Save all configuration, variables, and storage changes.
4. Open **Actions** at the top right and choose **Deploy**.
5. Follow deployment and application logs until the webapp is running.
6. Open its public HTTPS domain.

The team's webapp is <https://tectonicweb.openislamu.org/>.
Coolify routes its domain to this container's port 8080 independently of the
API resource.

## 7. Verify the complete experience

1. Create a disposable account in the webapp.
2. Open **Payments**. New fictional transactions should appear as simulation
   advances; the real-mode page refreshes approximately every three seconds.
3. Check the API logs for `DayCompleted(...)`. Successful authentication alone
   does not establish that simulation is running.
4. Explore recurring payments, notifications, and alert rules.
5. Open the configured Mailpit inbox and inspect the resulting emails.

A new account's logical date starts on **January 1, 2026**. The scheduled
internet payments on January 5 and February 5 trigger recurrence and a price
increase. At one simulated day every three seconds, the first scheduled
telecom alerts take roughly two minutes, plus processing time.

Email is sent only when an enabled condition fires, not for every transaction
or login. `EMAIL_RECIPIENT` is the delivery address; the signup email does not
replace it. A notification's `sent` status indicates transport acceptance:
inspect the inbox to confirm actual receipt.

## 8. Troubleshooting

| Symptom | What to check |
|---|---|
| Image pull fails | Verify the exact public GHCR image name and tag above, server connectivity to `ghcr.io`, and any stale registry credentials. These packages do not require authentication. |
| Domain returns a bad gateway | Check container logs and set **Ports Exposes = 8080**, not 80. |
| HTTPS domain or certificate fails | Check A/AAAA records, DNS caches, and public reachability of the proxy on ports 80 and 443. |
| API rejects the signing key | Replace the placeholder with Base64 decoding to at least 32 random bytes. |
| SQLite cannot open its database | Check the `tectonic-data` mount at `/data`, the connection string, and directory permissions. |
| Authentication works but no transactions arrive | Check `SIMULATOR_ENABLED=true`, `SIMULATOR_ALL_USERS=true`, and internal `SIMULATOR_BASE_URL=http://127.0.0.1:8080`; inspect `SimulatorStopped` or `UserSimulationStopped`. |
| Only the separate simulator account has activity | The image/settings may use single-account mode. Check `SIMULATOR_ALL_USERS` and deploy an image containing all-user support. |
| Blazor cannot authenticate against the API | Check its public `API_BASE_URL`, trailing slash, HTTPS reachability, and `API_USE_MOCK=false`. |
| Transactions appear but no email arrives | Allow the simulated scenario to reach an alert date; inspect delivery status, recipient, and API mail-delivery logs. |
| Mailpit delivery times out | Test connectivity from the API container. If IPv6 fails while IPv4 works, apply the workaround below. |
| Protected session data fails after redeployment | Check the `blazor-data-protection` mount at exactly `/root/.aspnet/DataProtection-Keys`; expired API tokens still require login. |
| Users must log in after API redeployment | Confirm the signing key did not change and the API database volume is still attached; tokens also expire normally. |

For the previously observed IPv6-only Mailpit timeout, add this **API runtime**
variable only when the same connectivity problem is confirmed:

```dotenv
DOTNET_SYSTEM_NET_DISABLEIPV6=1
```

Save and redeploy the API. Keep HTTPS certificate validation enabled.

## 9. Updating and preserving the deployment

- Keep the API signing key and both persistent volumes when changing image tags.
- Back up the API database with a consistent SQLite backup procedure before
  changing its schema or storage.
- Save configuration changes and redeploy the affected resource to apply them.
- Do not remove a database volume to repair a routine login or simulator issue.
- These image versions are a hackathon prototype. `EnsureCreated` initializes
  a fresh database; it does not migrate an existing database to a changed schema.

## Further reading

- [Environment settings](../.env.example)
- [Operations, email, and simulator details](OPERATIONS.md)
- [API reference](API.md)
- [Architecture](TECHNICAL.md)
- [Coolify application configuration](https://coolify.io/docs/applications/configuration/general)
- [Coolify environment variables](https://coolify.io/docs/applications/configuration/environment-variables)
- [.NET SMTP TLS support](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient.enablessl)

This guide documents the supplied image tags and the repository's current
configuration. Writing it does not perform a deployment or verify a new server's
DNS, registry access, or email transport.

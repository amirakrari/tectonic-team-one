<div align="center">

# Expense Watch

A financial alert webapp that tracks recurring payments and emails you when
bills rise or expected payments go missing.

### Your expenses change. Your inbox tells you when.

A live hackathon prototype by **Tectonic Team One**.

[**Self-host with Coolify: deployment guide**](docs/COOLIFY%20DEPLOYMENT%20GUIDE.md) |
[**Try the webapp**](https://tectonicweb.openislamu.org/) |
[**Open the email inbox**](https://mailpit.openislamu.org) |
[**Explore the live API**](https://tectonicapi.openislamu.org/swagger/)

</div>

**Self-host using our public GHCR images:** `ghcr.io/amirakrari/tectonic-api`
and `ghcr.io/amirakrari/tectonic-web`. No registry login or image build is needed.
Follow the [Coolify deployment guide](docs/COOLIFY%20DEPLOYMENT%20GUIDE.md).

## Experience the demo

| Go here | What you can do |
|---|---|
| **[tectonicweb.openislamu.org](https://tectonicweb.openislamu.org/)** | Create an account, watch transactions appear, explore recurring payments, and choose your alert rules. |
| **[mailpit.openislamu.org](https://mailpit.openislamu.org)** | Open the hosted inbox where the demo's alert emails arrive. |
| **[tectonicapi.openislamu.org/swagger/](https://tectonicapi.openislamu.org/swagger/)** | Read the API documentation and make live authenticated API calls through Swagger. |

The webapp and API use persisted, user-owned data. The financial activity is
fictional: **no bank connection and no real payments**. This is a hackathon
prototype, not a production banking service.

## Why Expense Watch?

A subscription gets more expensive. A monthly bill becomes a recurring habit.
A payment you expected stops showing up. Those changes are easy to miss in a
long transaction history.

Expense Watch turns them into clear notifications: what changed, which payment
it affects, and how much the amount increased. The hackathon demo compresses
months of financial activity into minutes, so you can watch the pattern emerge
and open the resulting email.

## What you can see in action

- **Catch rising bills.** A EUR 45 internet bill becomes EUR 49, and an email
  explains the EUR 4 increase.
- **Discover recurring payments.** Matching monthly payments become recurring
  streams, covering both expenses and income such as salary.
- **Notice missing payments.** When an expected month ends without a payment,
  an alert names the missing month and updates the recurring list.
- **Choose your alerts.** Enable or disable supported conditions while
  transaction history and recurrence tracking continue.
- **Follow your own account.** Each user has separate transactions, recurring
  streams, preferences, notifications, and a logical date.
- **Watch a complete flow.** Generated transactions pass through the
  authenticated API, persist in SQLite, and produce emails in hosted Mailpit.

## Try the story in a few minutes

1. Open the **[webapp](https://tectonicweb.openislamu.org/)** and create a
   disposable account.
2. Go to **Payments**. With all-user simulation enabled, fictional transactions
   arrive automatically and the page updates approximately every three seconds.
3. Explore recurring payments and alert rules as the account's simulated date
   advances.
4. Open **[Mailpit](https://mailpit.openislamu.org)** to read the generated
   alerts. Email goes to the configured demo mailbox, not your signup address.
5. Use **[Swagger](https://tectonicapi.openislamu.org/swagger/)** to explore the
   contract directly: sign up or log in, then paste the returned `accessToken`
   into **Authorize** before calling business endpoints.

**Give the story time to develop.** A new account begins on January 1, 2026.
The scheduled internet payments on January 5 and February 5 demonstrate
recurrence and a price increase. At one simulated day every three seconds,
those first scheduled alerts take roughly two minutes, plus processing time.
Emails fire when an alert condition is met, not for every transaction or login.

If the hosted deployment has simulation disabled, creating an account alone
will not generate transactions. Operators can enable the all-user experience
using [the environment example](.env.example) and
[simulator guide](docs/OPERATIONS.md#simulator).

## Built for the hackathon

The goal is a working demonstration from transaction to insight to email.
The automatic scenario combines an internet bill that rises in price and later
misses a month, monthly salary, and varied daily purchases or bonuses.

| Part | Technology |
|---|---|
| Webapp | Blazor Interactive Server and MudBlazor |
| API | ASP.NET Core 10 Minimal APIs and OpenAPI/Swagger |
| Data | EF Core 10 and SQLite |
| Authentication | ASP.NET Core Identity and JWT |
| Email | Hosted Mailpit HTTP delivery, with optional SMTP |
| Deployment | Separate API and webapp containers on Coolify |

Email is the implemented alert channel. SMS, in-app delivery, and bank
integration are outside the current demo.

## Documentation

The README is the front door. Setup, configuration, API examples, and
implementation details live in [`docs/`](docs/).

| Need | Start here |
|---|---|
| Run the API and webapp locally | [Local startup](docs/OPERATIONS.md#local-startup) |
| Prepare a controlled presentation | [Five-minute manual demo](docs/OPERATIONS.md#controlled-five-minute-demo) |
| Deploy the API and webapp in Coolify | [Coolify deployment guide](docs/COOLIFY%20DEPLOYMENT%20GUIDE.md) |
| Deploy, configure simulation, or troubleshoot email | [Operations guide](docs/OPERATIONS.md) |
| Look up endpoints and request/response bodies | [API reference](docs/API.md) |
| Understand the architecture and domain rules | [Technical guide](docs/TECHNICAL.md) |
| Configure the live demo without committing secrets | [Environment example](.env.example) |
| Prepare the team's pitch | [Project briefing](PROJECT_BRIEFING.md) |

## Contributors

Built together by Tectonic Team One for the hackathon.

| [**amirakrari**](https://github.com/amirakrari) | [**aryanratnaparkh1**](https://github.com/aryanratnaparkh1) | [**Nasserh2006**](https://github.com/Nasserh2006) |
|:---:|:---:|:---:|
| <a href="https://github.com/amirakrari"><img src="https://github.com/amirakrari.png?size=160" width="160" height="160" alt="amirakrari" /></a> | <a href="https://github.com/aryanratnaparkh1"><img src="https://github.com/aryanratnaparkh1.png?size=160" width="160" height="160" alt="aryanratnaparkh1" /></a> | <a href="https://github.com/Nasserh2006"><img src="https://github.com/Nasserh2006.png?size=160" width="160" height="160" alt="Nasserh2006" /></a> |
| Backend developer | Frontend developer | UI/UX designer and team leader |

## License

Expense Watch is licensed under the **GNU Affero General Public License v3
(AGPLv3)**. See [LICENSE](LICENSE) for the full terms.

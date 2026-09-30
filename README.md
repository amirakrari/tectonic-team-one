# tectonic-team-one

Webapp for our hackathon project: **get an email when your expenses change.**

We simulate a stream of KBC incomes and expenses. There's no real KBC connection: during the demo we add mock transactions ourselves. The API runs rules on every transaction and emails the user when one fires. This repo is the **webapp only**. The API is a separate project owned by the other half of the team.

> Hackathon scope: no tests, no auth, no validation, no CI. If it doesn't appear in the demo, it doesn't exist.

## What the rules do (implemented in the API)

| Rule | When it fires | Email |
|---|---|---|
| **Price increase** | An expense at a company costs more than the previous expense at that company | "Your expense at X went up from €A to €B." |
| **New recurring expense** | The same expense happens on the same day of the month, two months in a row | "X was added to your recurring expenses." |
| **Recurring expense stopped** | A recurring expense misses its payment the following month | "X was removed from your recurring expenses." |
| **Price decrease** | A subscription or recurring expense gets cheaper | "Your expense at X went down from €A to €B." |
| **Incomplete payment reminder** | A bill was paid in parts and a part is still open, relative to its due date and split (e.g. €400 of €500 paid) | "You paid €400 of your €500 X bill. €100 is still open." |

| **Possible double charge** | The same company charges the same amount twice within 2 days. Always sent right away. | "X charged you €A twice within 2 days. Was that intended?" |
| **Upcoming critical payment** | 3 days before a critical payment (rent, energy…) is due. Always sent right away. | "Your X payment of €A is due in 3 days." |

**Daily summary** (Settings → Notification channels): regular alerts are bundled into one email at 18:00, and critical alerts and the two "right away" rules still arrive instantly.

**Critical payments only** (a filter, not a rule): when on, only expenses marked *Critical* (rent, energy, insurance…) trigger emails. Groceries and subscriptions are *Regular* by default. You mark expenses on the Recurring expenses page.

## What the webapp does

| Page | Route | Purpose |
|---|---|---|
| Transactions | `/transactions` | Lists all transactions and has a form to **add a mock income/expense** (with a date picker, so you can backdate). After adding, a pop-up shows every email the rules just sent. |
| Recurring expenses | `/recurring` | Current list of recurring expenses, each with a **Critical / Regular** switch |
| Notifications | `/notifications` | Every email sent, with its rule type |
| Rules | `/rules` | The **Critical payments only** filter, then one card per rule: line icon, title, description, on/off switch. Open it by clicking the Rules card on Settings, or from the user menu. |
| Appearance | `/appearance` | **Appearance & Personalization.** Personalize switch, **Dark mode** switch, Themes (6 [uiGradients](https://uigradients.com) gradients; picking one makes it the background of the whole app, with frosted rail, top bar and cards, and recolors links, switches and buttons) and Tone of speech (Young, Professional, Joyful, Factual). Open it by clicking the Appearance card on Settings. |
| Email reminders | `/appearance/email` | Style of the reminder emails: **Tone** (Playful, Informal, Formal, Serious, Panicky), **Design** (Classic, Minimal, Theme), **Text size**, **Emoji**, with a live email preview for three example rules. |
| Login / Sign up | `/login` | Full-screen KBC Brussels login (teal background, white logo). Every other page requires login, and you stay logged in for the browser tab. **Demo only:** sample accounts come from `SampleAccounts` in `appsettings.Development.json`, and there's a *Use demo account* button. Sign-ups live in memory until restart. Log out is in the user menu. |
| Settings | `/` and `/settings` | **Landing page.** A Rules card (list and master email switch), Notification channels (email and daily summary), Appearance (light/dark) and **Language** (English, Français, Nederlands; switches every page, amounts, the rail links to kbcbrussels.be, and the email preview). All of it is stored per browser session only: the API has no settings endpoint yet. |

## Tech stack

| What | Choice |
|---|---|
| Runtime | .NET 10 (LTS) |
| UI framework | Blazor Web App, global **Interactive Server** render mode |
| Component library | [MudBlazor](https://mudblazor.com) 9.x inside a custom **KBC Brussels** shell: official logo, KBC blue `#0097DB` and navy `#0D2A50`, Poppins, thin line icons |
| API access | `HttpClient` behind `IApiClient`, with a mock client for working offline |

More detail, including the API contract, is in [docs/TECHNICAL.md](docs/TECHNICAL.md).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download), or on macOS `brew install --cask dotnet-sdk`. Open a new terminal afterwards and check with `dotnet --version`.
- An IDE: Rider, VS Code with C# Dev Kit, or Visual Studio 2022+
- A trusted HTTPS dev certificate. Run this **once per machine**. It asks for your password and adds the cert to your keychain:

  ```bash
  dotnet dev-certs https --trust
  ```

## Run it

```bash
cd src/Tectonic.Web
dotnet watch
```

Open **https://localhost:7130**. The app is HTTPS-only, with no HTTP port. `dotnet watch` hot-reloads most `.razor` and `.cs` edits.

## Configuration

`src/Tectonic.Web/appsettings.Development.json`:

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7080/",
    "UseMock": true
  }
}
```

- `UseMock: true` means the UI uses `MockApiClient`: canned data, plus a fake price-increase rule so the email pop-up can be tried. Use this until the API is running.
- `UseMock: false` means the UI calls the real API at `BaseUrl`. **Keep the trailing `/`.**
- **Mac users: don't put anything on port 5000.** The AirPlay Receiver already uses it and answers `403 Forbidden`.

## Docker (VPS deploy)

The `Dockerfile` builds and publishes the app, and `docker-compose.yml` runs it on port 8080 over plain HTTP. Put a reverse proxy (Caddy, nginx, Traefik) in front for HTTPS, with WebSockets enabled because Blazor Server needs them.

```bash
cp .env.example .env
```

In `.env` (gitignored):

| Variable | Meaning |
|---|---|
| `API_BASE_URL` | API base URL, **with trailing `/`**. It starts as the placeholder `https://api.example.com/`. Replace it with the real API. |
| `API_USE_MOCK` | `true` = `MockApiClient` (canned data), `false` = `ApiClient` calls `API_BASE_URL` |
| `WEB_PORT` | Host port (default `8080`) |

```bash
docker compose up -d --build
```

Open **http://localhost:8080** (or `http://<vps-ip>:8080`). After you edit `.env`, run `docker compose up -d` again to apply it.

## Demo script

1. Transactions: add an expense `Netflix`, €13.99, dated two months ago.
2. Add the same expense one month ago. A pop-up shows the email: *added to recurring expenses*.
3. Add `Netflix` at €15.99 today. A pop-up shows the email: *price went up*.
4. Show the Recurring expenses and Notifications pages.

## Project structure

```
src/Tectonic.Web/
├── Components/
│   ├── App.razor            # HTML shell: MudBlazor CSS/JS, render mode
│   ├── _Imports.razor       # global @using directives
│   ├── Layout/              # MainLayout (KBC top bar + left rail), NavMenu (logo, Privé switch, sections)
│   ├── Pages/               # Settings (landing), Rules, Transactions, Recurring, Notifications
│   └── Shared/              # KbcLogo, KbcIcon (line icons), KbcSwitch, PriveSwitch, RuleToggle
├── Models/                  # Transaction, RecurringExpense, Notification (mirror the API JSON)
├── Services/                # IApiClient, ApiClient, MockApiClient, UiState, RuleCatalog, AppearanceCatalog
├── wwwroot/                 # app.css (shell, nav rail, settings styles)
└── Program.cs               # DI, culture, pipeline
```

## Adding or changing a rule

All rule copy and icons live in **`Services/RuleCatalog.cs`**. Rules, Settings and Notifications read from it, so a new rule is one entry there, plus a new `NotificationType` value agreed with the API team. Rules 4 and 5 are placeholders (as in the mockup) until the final rules are delivered.

## Languages

Every UI string lives in **`Services/Texts.cs`** (English, French, Dutch), and every example email in `Services/EmailTemplates.cs`. In a component, inject `Loc L` and use `@L["key"]`, `L.F("key", args)` for placeholders, or `L.Money(amount)` for Belgian money formatting. When you add text, add all three languages. A missing key shows up on screen as the key itself.

## Team conventions

- Give each person their own page so you don't conflict in the same file.
- If the API contract changes, update `Models/`, `IApiClient`, **both** clients, and section 3 of the technical doc.
- Commit small changes and push often to `main`.
- Use MudBlazor components and utility classes (`pa-4`, `mt-2`, `d-flex`) before you write custom CSS.

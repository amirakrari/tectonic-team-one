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

## What the webapp does

| Page | Route | Purpose |
|---|---|---|
| Home | `/` | Explains the three rules |
| Transactions | `/transactions` | Lists all transactions and has a form to **add a mock income/expense** (with a date picker, so you can backdate). After adding, a pop-up shows every email the rules just sent. |
| Recurring expenses | `/recurring` | Current list of recurring expenses |
| Notifications | `/notifications` | Every email sent, with its rule type |

## Tech stack

| What | Choice |
|---|---|
| Runtime | .NET 10 (LTS) |
| UI framework | Blazor Web App, global **Interactive Server** render mode |
| Component library | [MudBlazor](https://mudblazor.com) 9.x, default Material theme |
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
│   ├── Layout/              # MainLayout (app bar + drawer), NavMenu
│   └── Pages/               # Home, Transactions, Recurring, Notifications
├── Models/                  # Transaction, RecurringExpense, Notification (mirror the API JSON)
├── Services/                # IApiClient, ApiClient, MockApiClient
├── wwwroot/                 # app.css
└── Program.cs               # DI, culture, pipeline
```

## Team conventions

- Give each person their own page so you don't conflict in the same file.
- If the API contract changes, update `Models/`, `IApiClient`, **both** clients, and section 3 of the technical doc.
- Commit small changes and push often to `main`.
- Use MudBlazor components and utility classes (`pa-4`, `mt-2`, `d-flex`) before you write custom CSS.

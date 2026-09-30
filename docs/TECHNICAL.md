# Technical documentation: webapp

This covers the webapp's architecture, its contract with the API, setup, and the patterns to copy. It follows the team's hackathon stack decisions: .NET 10, Blazor Server, MudBlazor defaults, no tests, no auth, no validation.

Everything here was built and run against .NET SDK 10.0.401 and MudBlazor 9.11.0. It builds with 0 warnings, and all four pages were exercised in a browser in mock mode.

---

## 1. Architecture

```
┌──────────┐  SignalR (WebSocket)  ┌──────────────────────┐  HTTP/JSON   ┌─────────────────────┐
│ Browser  │ ◄───────────────────► │ Tectonic.Web         │ ───────────► │ API (separate repo) │
│          │  UI events / diffs    │ Blazor Server        │  /api/...    │ Minimal API, SQLite │
└──────────┘                       │ MudBlazor            │              │ rules + emails      │
                                   └──────────────────────┘              └─────────────────────┘
```

- **Interactive Server:** all component C# runs on the server. The browser holds a SignalR connection, called a *circuit*, that carries UI events up and DOM diffs back down.
- API calls are **server-to-server**, so there are no CORS issues.
- **The webapp has no business logic.** It posts transactions and displays what the API returns. The API alone decides which rules fire and sends the emails.
- Everything goes through `IApiClient`. Pages never touch URLs or `HttpClient`.

### Key decisions

| Decision | Why |
|---|---|
| Interactivity set **globally** on `Routes` | Every page is interactive, so nobody has to add `@rendermode` per page. |
| **Prerendering off** | Otherwise `OnInitializedAsync` runs twice, causing double API calls and flicker. SEO is irrelevant. |
| **Culture pinned to `en-GB`** | Machines with a Belgian/Dutch locale parse `10.99` as **1099**. Pinning the culture gives everyone dot decimals and `dd/MM/yyyy` dates. |
| `IApiClient` + `MockApiClient` | The stack doc says to avoid interfaces unless they're immediately needed. This one is needed, because the API is built in parallel and the UI can't wait for it. One config flag switches. |
| MudBlazor defaults, no custom CSS | Consistent look with no time spent on design. |

---

## 2. API contract

This is **the webapp's proposal. Agree on it with the API team before coding.** It follows the stack doc's conventions: Minimal API under `/api/...`, entities used directly as request/response bodies, and `int` ids from SQLite.

| Method | Route | Body | Returns |
|---|---|---|---|
| `GET` | `/api/transactions` | – | `Transaction[]` |
| `POST` | `/api/transactions` | `Transaction` (no `id`) | `201` + created `Transaction`. **The rules run synchronously during this call.** |
| `GET` | `/api/recurring-expenses` | – | `RecurringExpense[]` (current list only) |
| `PATCH` | `/api/recurring-expenses/{id}` | `{ "isCritical": true }` | `204`. Marks an expense critical or regular. |
| `GET`/`PUT` | `/api/settings` *(proposed, not called yet)* | `Settings` (below) | The user's settings, so the rules and emails respect them |
| `GET` | `/api/notifications` | – | `Notification[]` (every email sent) |

Because the rules run inside the `POST`, any notifications they create exist by the time the call returns. The UI relies on this to show "email sent" pop-ups right away (see section 5.2).

### JSON shapes

Property names are camelCase (the System.Text.Json default). **Enums are strings**, so the API must register `JsonStringEnumConverter`:

```csharp
// API Program.cs
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

```jsonc
// Transaction
{ "id": 7, "type": "Expense", "company": "Netflix", "amount": 15.99, "date": "2026-09-30T00:00:00" }
// type: "Expense" | "Income"; amount is always positive

// RecurringExpense
{ "id": 1, "company": "Netflix", "amount": 13.99, "dayOfMonth": 30, "addedAt": "2026-08-30T00:00:00", "isCritical": false }

// Notification
{ "id": 4, "type": "PriceIncrease", "company": "Netflix",
  "message": "Your expense at Netflix went up from €13.99 to €15.99.", "createdAt": "2026-09-30T20:01:00" }
// type: "PriceIncrease" | "PriceDecrease" | "RecurringAdded" | "RecurringRemoved" | "PaymentIncomplete"
//       | "DuplicateCharge" | "UpcomingPayment"
// delivery: "Instant" | "Digest" (held for the 18:00 daily summary)

// Settings (proposed): what the UI holds today in UiState, per browser session
{ "email": "john.doe@example.com", "language": "en", "notificationsEnabled": true, "criticalOnly": false, "dailyDigest": false,
  "rules": { "price-increase": true, "price-decrease": true, "recurring-added": true,
             "recurring-removed": true, "payment-incomplete": true, "duplicate-charge": true, "upcoming-payment": true },
  "emailStyle": { "tone": "Formal", "design": "Classic", "textSize": "Regular", "emoji": false } }
// tone: "Playful" | "Informal" | "Formal" | "Serious" | "Panicky"; design: "Classic" | "Minimal" | "Theme"
```

Example email wording for each tone is in `Services/EmailTemplates.cs`. The API team can reuse it.

The `message` text is written by the API, and the UI shows it as is. That way the email and the UI always say the same thing.

### Open questions for the API team

- **"Recurring expense stopped" timing.** Nothing ticks through time in a demo, because scheduling was dropped in the stack doc. A simple option: when any transaction is posted, treat its `date` as "today" and remove any recurring expense whose next expected date has already passed. The UI doesn't need to change for this.
- **Email recipient and settings.** The UI now collects the email address, rule switches, critical-only and email style. Until `/api/settings` exists, these live in the browser session only. The mock client already honors them, so the demo behaves correctly.
- **Incomplete payment data.** The API needs to know a bill's total and due date to spot a missing part. Proposal: optional `billTotal` and `dueDate` on `Transaction` (the first part of a split payment carries them). The API sums payments to that company since the first part, and reminds before `dueDate` if the sum is below `billTotal`. When agreed, the UI adds these two optional fields to the Add transaction form.
- **Critical-only filtering.** The API should skip emails for expenses whose company isn't marked `isCritical` when `criticalOnly` is on. *Possible double charge* and *Upcoming critical payment* are exempt, and so is the daily summary: they are always sent instantly.
- **Language.** The API writes `message` and the emails in the user's `language` (`en`/`fr`/`nl`). Wording for all three languages is in `Services/Texts.cs` (`msg.*`) and `Services/EmailTemplates.cs`.
- **Upcoming critical payment timing.** Like "recurring stopped", this needs a notion of "today". The same approach works: check on every posted transaction, or on app start.
- **Port.** Everything is HTTPS-only. The webapp runs on `https://localhost:7130` and assumes the API is on `https://localhost:7080/`. The API team should set `applicationUrl` in their `launchSettings.json` to `https://localhost:7080`, or we change `Api:BaseUrl` to match whatever they pick. **Don't use port 5000 on a Mac:** the AirPlay Receiver already listens there and answers `403 Forbidden`.

---

## 3. How the project was bootstrapped (reference)

**This is already done:** the project is in `src/Tectonic.Web`, so just pull. These steps show how it was built, in case you need to recreate it.

```bash
# from repo root
dotnet new blazor -n Tectonic.Web -o src/Tectonic.Web \
  --interactivity Server --all-interactive --empty
cd src/Tectonic.Web
dotnet add package MudBlazor
```

This is a single project with no solution file, as the stack doc says. Then make these edits, keeping everything else the template generated.

### 3.1 `Program.cs`

```csharp
using System.Globalization;
using MudBlazor.Services;
using Tectonic.Web.Services;

// Same number/date format on every machine: dot decimals, dd/MM/yyyy dates.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-GB");

var builder = WebApplication.CreateBuilder(args);

// ...after AddRazorComponents().AddInteractiveServerComponents():
builder.Services.AddMudServices();

if (builder.Configuration.GetValue<bool>("Api:UseMock"))
{
    builder.Services.AddScoped<IApiClient, MockApiClient>();
}
else
{
    builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!));
}
```

### 3.2 `Components/_Imports.razor`

Add:

```razor
@using MudBlazor
@using Tectonic.Web.Models
@using Tectonic.Web.Services
```

### 3.3 `Components/App.razor`

1. In `<head>`, above the existing `app.css` link, add:

   ```html
   <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
   <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
   ```

2. In `<body>`, after the existing `<script src="@Assets["_framework/blazor.web.js"]">` tag, add:

   ```html
   <script src="_content/MudBlazor/MudBlazor.min.js"></script>
   ```

3. To turn off prerendering, change `@rendermode="InteractiveServer"` to `@rendermode="RenderMode"` on both `HeadOutlet` and `Routes`, then add this at the bottom of the file:

   ```razor
   @code {
       private static readonly IComponentRenderMode RenderMode =
           new InteractiveServerRenderMode(prerender: false);
   }
   ```

### 3.4 `Components/Layout/MainLayout.razor`

> **Since replaced** by the KBC Brussels shell, built from the design mockups. It has a plain HTML/CSS layout (not `MudLayout`), an 88px left rail (logo, Privé switch, Paiements, Épargne & Placements, Logement, Famille, Mobilité), and a top bar with a back arrow, the page title (via `<SectionOutlet SectionName="page-title" />`) and a user menu. See the file in the repo. The version below is only the minimal starting point.

Replace the whole file. MudBlazor's providers **must** sit inside the interactive tree, which is why they're in the layout. Keep the `blazor-error-ui` div: `MainLayout.razor.css` keeps it hidden until something crashes.

```razor
@inherits LayoutComponentBase

<MudThemeProvider Theme="_theme" IsDarkMode="_darkMode" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.Start"
                       OnClick="() => _drawerOpen = !_drawerOpen" />
        <MudText Typo="Typo.h6">Tectonic</MudText>
        <MudSpacer />
        <MudIconButton Icon="@(_darkMode ? Icons.Material.Filled.LightMode : Icons.Material.Filled.DarkMode)"
                       Color="Color.Inherit" OnClick="() => _darkMode = !_darkMode" />
    </MudAppBar>

    <MudDrawer @bind-Open="_drawerOpen" ClipMode="DrawerClipMode.Always" Elevation="2">
        <NavMenu />
    </MudDrawer>

    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.Large" Class="py-6">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">🗙</span>
</div>

@code {
    private bool _drawerOpen = true;
    private bool _darkMode;

    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight { Primary = "#1E88E5", AppbarBackground = "#1E88E5" },
        PaletteDark  = new PaletteDark  { Primary = "#90CAF9" }
    };
}
```

### 3.5 `Components/Layout/NavMenu.razor` (new file)

```razor
<MudNavMenu>
    <MudNavLink Href="" Match="NavLinkMatch.All" Icon="@Icons.Material.Filled.Home">Home</MudNavLink>
    <MudNavLink Href="transactions" Icon="@Icons.Material.Filled.ReceiptLong">Transactions</MudNavLink>
    <MudNavLink Href="recurring" Icon="@Icons.Material.Filled.EventRepeat">Recurring expenses</MudNavLink>
    <MudNavLink Href="notifications" Icon="@Icons.Material.Filled.Notifications">Notifications</MudNavLink>
</MudNavMenu>
```

### 3.6 `Properties/launchSettings.json` (HTTPS only)

Delete the `http` profile, and remove the `http://` URL from the `https` profile:

```json
"https": {
  "commandName": "Project",
  "dotnetRunMessages": true,
  "launchBrowser": true,
  "applicationUrl": "https://localhost:7130",
  "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
}
```

### 3.7 `appsettings.Development.json`

Add an `Api` section next to the generated `Logging` section:

```json
"Api": {
  "BaseUrl": "https://localhost:7080/",
  "UseMock": true
}
```

Next, add the models and services from section 4 and the pages from section 5. The project won't compile until `IApiClient` and both clients exist, because `Program.cs` references them. Run `dotnet build` once at the end, then commit.

---

## 4. Models and API client

### 4.1 `Models/`

The models are plain classes that mirror the JSON in section 2. They're classes rather than records because MudBlazor form fields bind to settable properties. The `[JsonConverter]` on each enum makes the client read and write enums as strings. It also still accepts numbers, in case the API forgets the converter.

```csharp
// Models/Transaction.cs
using System.Text.Json.Serialization;

namespace Tectonic.Web.Models;

[JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
public enum TransactionType { Expense, Income }

public class Transaction
{
    public int Id { get; set; }
    public TransactionType Type { get; set; }
    public string Company { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}
```

```csharp
// Models/RecurringExpense.cs
namespace Tectonic.Web.Models;

public class RecurringExpense
{
    public int Id { get; set; }
    public string Company { get; set; } = "";
    public decimal Amount { get; set; }
    public int DayOfMonth { get; set; }
    public DateTime AddedAt { get; set; }
}
```

```csharp
// Models/Notification.cs
using System.Text.Json.Serialization;

namespace Tectonic.Web.Models;

[JsonConverter(typeof(JsonStringEnumConverter<NotificationType>))]
public enum NotificationType { PriceIncrease, RecurringAdded, RecurringRemoved }

public class Notification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Company { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
```

### 4.2 `Services/IApiClient.cs`

```csharp
using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

public interface IApiClient
{
    Task<List<Transaction>> GetTransactionsAsync();
    Task<Transaction> AddTransactionAsync(Transaction transaction);
    Task<List<RecurringExpense>> GetRecurringExpensesAsync();
    Task<List<Notification>> GetNotificationsAsync();
}
```

### 4.3 `Services/ApiClient.cs`

```csharp
using System.Net.Http.Json;
using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

public class ApiClient(HttpClient http) : IApiClient
{
    public async Task<List<Transaction>> GetTransactionsAsync() =>
        await http.GetFromJsonAsync<List<Transaction>>("api/transactions") ?? [];

    public async Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        var response = await http.PostAsJsonAsync("api/transactions", transaction);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Transaction>())!;
    }

    public async Task<List<RecurringExpense>> GetRecurringExpensesAsync() =>
        await http.GetFromJsonAsync<List<RecurringExpense>>("api/recurring-expenses") ?? [];

    public async Task<List<Notification>> GetNotificationsAsync() =>
        await http.GetFromJsonAsync<List<Notification>>("api/notifications") ?? [];
}
```

> Relative paths resolve against `BaseAddress`, so `BaseUrl` **must end in `/`**. Also, don't start paths with `/`, because that drops any path in the base URL.

### 4.4 `Services/MockApiClient.cs`

The mock returns canned data that covers all three notification types. It also fakes **only** the price-increase rule, so the "email sent" pop-up can be demoed before the API exists. The real rules stay in the API.

```csharp
using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

// Canned data so the UI works before the API is up. The real rules live in the API.
public class MockApiClient : IApiClient
{
    private static readonly DateTime Today = DateTime.Today;

    private readonly List<Transaction> _transactions =
    [
        new() { Id = 1, Type = TransactionType.Income,  Company = "Employer NV", Amount = 2800m,  Date = Today.AddMonths(-2).AddDays(-3) },
        new() { Id = 2, Type = TransactionType.Expense, Company = "Netflix",     Amount = 13.99m, Date = Today.AddMonths(-2) },
        new() { Id = 3, Type = TransactionType.Expense, Company = "Netflix",     Amount = 13.99m, Date = Today.AddMonths(-1) },
        new() { Id = 4, Type = TransactionType.Expense, Company = "Proximus",    Amount = 45m,    Date = Today.AddMonths(-1).AddDays(2) },
        new() { Id = 5, Type = TransactionType.Expense, Company = "Proximus",    Amount = 49.50m, Date = Today.AddDays(-5) },
    ];

    private readonly List<RecurringExpense> _recurring =
    [
        new() { Id = 1, Company = "Netflix", Amount = 13.99m, DayOfMonth = Today.Day, AddedAt = Today.AddMonths(-1) },
    ];

    private readonly List<Notification> _notifications =
    [
        new() { Id = 1, Type = NotificationType.RecurringAdded,   Company = "Netflix",  Message = "Netflix was added to your recurring expenses.",           CreatedAt = Today.AddMonths(-1) },
        new() { Id = 2, Type = NotificationType.PriceIncrease,    Company = "Proximus", Message = "Your expense at Proximus went up from €45.00 to €49.50.", CreatedAt = Today.AddDays(-5) },
        new() { Id = 3, Type = NotificationType.RecurringRemoved, Company = "Spotify",  Message = "Spotify was removed from your recurring expenses.",       CreatedAt = Today.AddDays(-2) },
    ];

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        await Task.Delay(300); // simulate latency so loading states are visible
        return [.. _transactions];
    }

    public Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        // Only the price-increase rule is faked here, so the UI's "email sent" toast can be tried without the API.
        var previous = _transactions
            .Where(t => t.Type == TransactionType.Expense && t.Company.Equals(transaction.Company, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Date)
            .LastOrDefault();
        if (transaction.Type == TransactionType.Expense && previous is not null && transaction.Amount > previous.Amount)
        {
            _notifications.Add(new()
            {
                Id = _notifications.Max(n => n.Id) + 1,
                Type = NotificationType.PriceIncrease,
                Company = transaction.Company,
                Message = $"Your expense at {transaction.Company} went up from €{previous.Amount:0.00} to €{transaction.Amount:0.00}.",
                CreatedAt = DateTime.Now,
            });
        }

        transaction.Id = _transactions.Max(t => t.Id) + 1;
        _transactions.Add(transaction);
        return Task.FromResult(transaction);
    }

    public Task<List<RecurringExpense>> GetRecurringExpensesAsync() => Task.FromResult<List<RecurringExpense>>([.. _recurring]);

    public Task<List<Notification>> GetNotificationsAsync() => Task.FromResult<List<Notification>>([.. _notifications]);
}
```

Mock data lives per circuit (it's a `Scoped` service), so refreshing the browser resets it.

---

## 5. Pages

| Page | File | Shows |
|---|---|---|
| Transactions | `Pages/Transactions.razor` | Add form (`MudSelect` for type, `MudTextField`, `MudNumericField`, `MudDatePicker`) above a `MudDataGrid`, newest first |
| Recurring expenses | `Pages/Recurring.razor` | `MudDataGrid`: company, amount, day of month, since. `MudAlert` when empty |
| Notifications | `Pages/Notifications.razor` | A `MudPaper` row per email: icon, message, date, and a `MudChip` for the rule type |
| Rules | `Pages/Rules.razor` | One shadowed card per rule from `RuleCatalog`: `KbcIcon`, title, description, `RuleToggle` |
| Settings | `Pages/Settings.razor` | Rules card (one `MudSwitch` per rule), Notification channels and Appearance cards with inline Edit, then Save/Cancel. The state lives in `UiState` (scoped per circuit) and isn't persisted. |

**Page conventions from the design pass:**

- Each page sets its top-bar title with `<SectionContent SectionName="page-title">…</SectionContent>`.
- The page heading is `<MudText Typo="Typo.h5" Class="page-heading">`.
- Surfaces are `Outlined="true"` with no elevation (cards, grids, papers).
- **Match the mockups.** Structure, colors and style follow the provided KBC designs. Only fix spacing or content, and don't restyle.
- **Icons:** use `<KbcIcon Name="…" />`, thin 1.5px line icons on a 24px grid, with paths adapted from Lucide (ISC). Add new icons to the dictionary in `Components/Shared/KbcIcon.razor`. Don't mix in Material icons.
- **Switches:** use `<KbcSwitch>` (blue track, white knob), not `MudSwitch`.
- **Brand:** the logo is the official `wwwroot/img/kbc-brussels-logo.svg` from kbcbrussels.be. The colors are taken from it: KBC blue `#0097DB` (links, switches, active nav, primary buttons) and KBC navy `#0D2A50` (nav labels). Shell colors are CSS variables in `app.css` (`--kbc-*`), with a dark-mode override on `.shell--dark`.
- **Themes and accent:** `AppearanceCatalog.Themes` defines each theme's gradient (from uiGradients, MIT) and accent color. With a theme active, `.shell--themed` paints the gradient behind the whole app (`--kbc-page-bg`). The rail, top bar and `.kbc-card`s become frosted panels, and page headings on the gradient turn white. Every gradient starts dark at the top-left so those headings stay readable. The layout sets `--kbc-accent` on `.shell` and rebuilds the MudBlazor theme with the same `Primary`. Use `var(--kbc-accent)` for fills and `var(--kbc-accent-text)` for accent-colored text (it lightens the accent in dark mode so it stays readable). Never hard-code KBC blue in components.
- **Dark mode:** toggled from Appearance (Dark mode card), the Settings Appearance card (Edit), or the user menu. It uses neutral greys (`#1C1C1E` background, `#2A2A2D` cards) with light text. The logo is inlined as `<KbcLogo />`, so its navy wordmark switches to white through `--kbc-logo-text`. The blue mark keeps its brand color.
- **Tone of speech** is stored in `UiState.Tone` only. Sending it to the API (so emails use that tone) needs a settings endpoint.
- **Rail links:** Paiements opens our Transactions page. The other sections open the matching kbcbrussels.be pages in a new tab.
- **Rule switches** use `<RuleToggle Rule="…" />`. It updates `UiState` (so every page stays in sync) and confirms with a short pop-up.
- **Custom `MudMenu` activators** must call `context.ToggleAsync`, otherwise the menu never opens (MudBlazor 9).
- Colors come from MudBlazor CSS variables (`var(--mud-palette-…)`), so dark mode works automatically.
- Shared classes live in `wwwroot/app.css`: `settings-card`, `card-title`, `card-actions`, `muted`.

### 5.1 Page pattern

Every data page has the same shape: a progress bar while loading, the data, and a pop-up if the call fails.

```razor
@page "/recurring"
@inject IApiClient Api
@inject ISnackbar Snackbar

<PageTitle>Recurring expenses</PageTitle>

<MudText Typo="Typo.h4" GutterBottom="true">Recurring expenses</MudText>

@if (_items is null)
{
    <MudProgressLinear Color="Color.Primary" Indeterminate="true" />
}
else if (_items.Count == 0)
{
    <MudAlert Severity="Severity.Info">No recurring expenses yet. Add the same expense on the same day two months in a row.</MudAlert>
}
else
{
    <MudDataGrid Items="_items" Dense="true" Hover="true">
        <Columns>
            <PropertyColumn Property="x => x.Company" />
            <PropertyColumn Property="x => x.Amount" Format="€ #,##0.00" />
            <PropertyColumn Property="x => x.DayOfMonth" Title="Day of month" />
            <PropertyColumn Property="x => x.AddedAt" Title="Recurring since" Format="dd/MM/yyyy" />
        </Columns>
    </MudDataGrid>
}

@code {
    private List<RecurringExpense>? _items;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _items = await Api.GetRecurringExpensesAsync();
        }
        catch (Exception ex)
        {
            _items = [];
            Snackbar.Add($"Could not load recurring expenses: {ex.Message}", Severity.Error);
        }
    }
}
```

### 5.2 The "email sent" pop-up (Transactions page)

This is the demo's key moment. When a transaction is added, the page loads the notification ids, posts the transaction, and loads the notifications again. Any new ones are exactly the emails the rules just sent, so each one is shown as a pop-up. The API doesn't need an extra endpoint for this.

```csharp
private async Task AddAsync()
{
    _saving = true;
    try
    {
        // Diff notifications before/after so the demo shows which "emails" the rules fired.
        var before = (await Api.GetNotificationsAsync()).Select(n => n.Id).ToHashSet();

        await Api.AddTransactionAsync(new Transaction
        {
            Type = _new.Type,
            Company = _new.Company.Trim(),
            Amount = _new.Amount,
            Date = _date ?? DateTime.Today,
        });
        Snackbar.Add($"Added {_new.Type.ToString().ToLower()} at {_new.Company}", Severity.Success);

        foreach (var n in (await Api.GetNotificationsAsync()).Where(n => !before.Contains(n.Id)))
        {
            Snackbar.Add($"📧 {n.Message}", Severity.Warning);
        }

        await LoadAsync();
    }
    catch (Exception ex)
    {
        Snackbar.Add($"Could not add transaction: {ex.Message}", Severity.Error);
    }
    finally
    {
        _saving = false;
    }
}
```

The form keeps its values after adding. That's deliberate: during the demo you only change the date or amount between entries. `MudDatePicker` binds `DateTime?` through `@bind-Date`, so the page keeps a separate `_date` field.

### 5.3 MudBlazor quick reference

| Need | Component |
|---|---|
| Table / grid | `MudDataGrid` (`Format` on `PropertyColumn` for € and dates) |
| Form fields | `MudTextField`, `MudNumericField`, `MudSelect`, `MudDatePicker`, `MudSwitch` |
| Layout | `MudGrid` / `MudItem` (12-column), `MudStack`, `MudPaper`, `MudCard` |
| Feedback | `ISnackbar`, `MudAlert`, `MudChip`, `MudProgressLinear` |
| Dialogs | `IDialogService.ShowMessageBoxAsync(...)` (note: MudBlazor 9 names it `...Async`) |
| Charts | `MudChart` (e.g. spending per company, if time allows) |

Utility classes: `pa-4`, `mt-2`, `mb-6`, `d-flex`, `align-center`, `gap-3`, `flex-grow-1`.

---

## 6. Gotchas we hit or expect

| Symptom | Cause / fix |
|---|---|
| `10.99` saved as `1099` | The machine locale uses a decimal comma. Keep the culture line in `Program.cs`. |
| `'X' is a method, which is not valid in the given context` | A helper method in a page is named like a MudBlazor enum (`Color`, `Size`, `Icon`). Rename it, e.g. `ColorOf`. |
| `ShowMessageBox` not found | MudBlazor 9 renamed it `ShowMessageBoxAsync`. |
| Dropdowns / date picker don't open | `<MudPopoverProvider />` is missing from `MainLayout`. |
| Buttons do nothing | Not interactive. Check `@rendermode` on `Routes` in `App.razor`. |
| Double API calls on load | Prerendering is back on (section 3.3). |
| Enum JSON errors (`could not convert "Expense"`) | The API is missing `JsonStringEnumConverter` (section 2). |
| API call 404s | `BaseUrl` is missing its trailing `/`, or the route starts with `/`. |
| `403 Forbidden` / access denied on `localhost:5000` | macOS AirPlay Receiver owns port 5000. Use another port (the API uses 7080), or turn it off in System Settings → General → AirDrop & Handoff → AirPlay Receiver. |
| `localhost:7130` refused to connect | The webapp isn't running. Start it with `dotnet watch` in `src/Tectonic.Web`. There's no HTTP port, so `http://` URLs won't work. |
| Browser warns "Your connection is not private" / API call fails with an SSL error | The dev certificate isn't trusted on this machine. Run `dotnet dev-certs https --trust` once, then restart the browser. |
| "Reconnecting…" overlay | The server restarted (normal with `dotnet watch`). Reload. |
| UI doesn't update after a timer/background callback | Call `await InvokeAsync(StateHasChanged)`. |

---

## 7. Out of scope

Tests, auth, input validation, CI/CD, containers, localization, and custom design tokens are out, per the team's stack decisions. The UI doesn't send emails and doesn't evaluate rules. The API does both.

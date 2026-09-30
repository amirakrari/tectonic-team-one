using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using ExpenseWatch.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseWatch.Api.Services;

public sealed class TransactionSimulator(
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    IHttpClientFactory clients,
    IServiceScopeFactory scopes,
    ILogger<TransactionSimulator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stage = "configuration";
        int? status = null;
        DateOnly? date = null;
        var reason = "failure";

        try
        {
            var settings = configuration.GetSection("Simulator");
            var allUsers = settings.GetValue<bool>("AllUsers");
            var password = settings["Password"];
            var email = settings["Email"] ?? "simulator@example.test";
            if ((!allUsers && (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))) ||
                !int.TryParse(settings["IntervalSeconds"] ?? "3", NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var intervalSeconds) ||
                intervalSeconds <= 0 || intervalSeconds > (uint.MaxValue - 1) / 1000 ||
                !int.TryParse(settings["RenewBeforeExpirySeconds"] ?? "60", NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var renewalSeconds) || renewalSeconds <= 0 ||
                renewalSeconds >= 3600 ||
                !Uri.TryCreate(settings["BaseUrl"] ?? "http://127.0.0.1:5000",
                    UriKind.Absolute, out var baseUrl) ||
                (baseUrl.Scheme != Uri.UriSchemeHttp && baseUrl.Scheme != Uri.UriSchemeHttps) ||
                !baseUrl.IsLoopback || baseUrl.UserInfo.Length != 0 ||
                baseUrl.AbsolutePath != "/" || baseUrl.Query.Length != 0 ||
                baseUrl.Fragment.Length != 0)
            {
                reason = "invalid-settings";
                return;
            }

            stage = "application-start";
            var started = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = lifetime.ApplicationStarted.Register(
                () => started.TrySetResult());
            await started.Task.WaitAsync(stoppingToken);

            using var client = clients.CreateClient("Simulator");
            client.BaseAddress = baseUrl;
            var renewalLead = TimeSpan.FromSeconds(renewalSeconds);
            var credentials = new AuthRequest(email, password ?? "");
            var token = allUsers ? null : await AuthenticateAsync(allowSignup: true);
            var userTokens = new Dictionary<string, TokenResponse>();
            var stoppedUsers = new HashSet<string>();

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (allUsers)
                {
                    using var scope = scopes.CreateScope();
                    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                    var tokens = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
                    foreach (var user in await users.Users.OrderBy(u => u.Id).ToArrayAsync(stoppingToken))
                    {
                        if (stoppedUsers.Contains(user.Id)) continue;
                        try
                        {
                            if (!userTokens.TryGetValue(user.Id, out var session)
                                || DateTimeOffset.UtcNow >= session.ExpiresAt - renewalLead)
                            {
                                session = tokens.Issue(user, await users.GetRolesAsync(user));
                                userTokens[user.Id] = session;
                            }
                            client.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue("Bearer", session.AccessToken);
                            await ProcessDayAsync();
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                            // Do not replay uncertain writes or stop unrelated accounts.
                            stoppedUsers.Add(user.Id);
                            logger.LogWarning(
                                "UserSimulationStopped: user={User}, stage={Stage}, status={Status}, date={Date}",
                                user.Id, stage, status, date);
                        }
                    }
                }
                else
                {
                    if (DateTimeOffset.UtcNow >= token!.ExpiresAt - renewalLead)
                    {
                        token = await AuthenticateAsync(allowSignup: false);
                        logger.LogInformation("TokenRenewed");
                    }
                    await ProcessDayAsync();
                }
            }

            reason = "timer-ended";

            async Task ProcessDayAsync()
            {
                date = null;
                stage = "get-date";
                status = null;
                using var clockResponse = await client.GetAsync("/api/demo/date", stoppingToken);
                RequireSuccess(clockResponse);
                var clock = await clockResponse.Content.ReadFromJsonAsync<ClockDate>(
                    cancellationToken: stoppingToken)
                    ?? throw new InvalidDataException();
                date = clock.Date;
                var dateKey = clock.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                stage = "get-transactions";
                status = null;
                using var historyResponse = await client.GetAsync(
                    $"/api/transactions?date={dateKey}", stoppingToken);
                RequireSuccess(historyResponse);
                var history = await historyResponse.Content.ReadFromJsonAsync<TransactionRecord[]>(
                    cancellationToken: stoppingToken)
                    ?? throw new InvalidDataException();
                var persisted = history.Select(t =>
                    (t.Type, t.CounterpartyKey, t.TransactionKey, t.Currency, t.Date)).ToHashSet();

                stage = "generate";
                status = null;
                var batch = GenerateDay(clock.Date);
                var count = 0;
                foreach (var input in batch)
                {
                    var identity = (input.Type, input.CounterpartyKey, input.TransactionKey,
                        input.Currency, input.Date);
                    if (persisted.Contains(identity))
                    {
                        continue;
                    }

                    stage = "post-transaction";
                    status = null;
                    using var response = await client.PostAsJsonAsync(
                        "/api/transactions", input, stoppingToken);
                    RequireSuccess(response);
                    persisted.Add(identity);
                    count++;
                }

                stage = "advance-date";
                status = null;
                using var advanceResponse = await client.PutAsJsonAsync(
                    "/api/demo/date", new ClockDate(clock.Date.AddDays(1)), stoppingToken);
                RequireSuccess(advanceResponse);
                logger.LogInformation("DayCompleted({Date},{Count})", dateKey, count);
            }

            async Task<TokenResponse> AuthenticateAsync(bool allowSignup)
            {
                stage = allowSignup ? "login" : "renew-login";
                status = null;
                using var login = await client.PostAsJsonAsync(
                    "/api/auth/login", credentials, stoppingToken);
                TokenResponse result;
                if (allowSignup && login.StatusCode == HttpStatusCode.Unauthorized)
                {
                    stage = "signup";
                    status = null;
                    using var signup = await client.PostAsJsonAsync(
                        "/api/auth/signup", credentials, stoppingToken);
                    RequireSuccess(signup);
                    result = await signup.Content.ReadFromJsonAsync<TokenResponse>(
                        cancellationToken: stoppingToken)
                        ?? throw new InvalidDataException();
                }
                else
                {
                    RequireSuccess(login);
                    result = await login.Content.ReadFromJsonAsync<TokenResponse>(
                        cancellationToken: stoppingToken)
                        ?? throw new InvalidDataException();
                }

                if (string.IsNullOrWhiteSpace(result.AccessToken) ||
                    !string.Equals(result.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase) ||
                    result.ExpiresAt <= DateTimeOffset.UtcNow + renewalLead)
                {
                    throw new InvalidDataException();
                }

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", result.AccessToken);
                return result;
            }

            void RequireSuccess(HttpResponseMessage response)
            {
                status = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException();
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            reason = "shutdown";
        }
        catch (OperationCanceledException)
        {
            reason = "request-timeout";
        }
        catch (Exception)
        {
            // Exception text and response bodies may contain credentials or tokens.
            // Stop this worker without faulting the host or replaying uncertain writes.
            reason = "request-or-contract-failure";
        }
        finally
        {
            logger.LogInformation(
                "SimulatorStopped: reason={Reason}, stage={Stage}, status={Status}, date={Date}",
                reason, stage, status, date);
        }
    }

    private static List<TransactionInput> GenerateDay(DateOnly date)
    {
        var batch = new List<TransactionInput>();
        var cycleMonth = (date.Month - 1) % 3;
        if (date.Day == 5 && cycleMonth != 2)
        {
            batch.Add(new TransactionInput("expense", "example-telecom", "Example Telecom",
                "home-internet", "Fictional home internet", cycleMonth == 0 ? 45m : 49m,
                "EUR", date));
        }
        if (date.Day == 25)
        {
            batch.Add(new TransactionInput("income", "example-employer", "Example Employer",
                "monthly-salary", "Fictional monthly salary", 2500m, "EUR", date));
        }

        var random = new Random(date.DayNumber);
        var expense = random.Next(4) != 0;
        var catalog = expense
            ? new[]
            {
                (Key: "example-grocer", Name: "Example Grocer", Description: "Fictional groceries",
                    MinCents: 800, MaxCents: 6500),
                (Key: "example-cafe", Name: "Example Cafe", Description: "Fictional cafe purchase",
                    MinCents: 250, MaxCents: 1800),
                (Key: "example-bookshop", Name: "Example Bookshop", Description: "Fictional books",
                    MinCents: 600, MaxCents: 4000)
            }
            : new[]
            {
                (Key: "example-studio", Name: "Example Studio", Description: "Fictional project bonus",
                    MinCents: 2500, MaxCents: 15000),
                (Key: "example-workshop", Name: "Example Workshop", Description: "Fictional task bonus",
                    MinCents: 1000, MaxCents: 9000)
            };
        var item = catalog[random.Next(catalog.Length)];
        var key = $"{(expense ? "purchase" : "bonus")}-{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        batch.Add(new TransactionInput(expense ? "expense" : "income", item.Key, item.Name,
            key, item.Description, random.Next(item.MinCents, item.MaxCents + 1) / 100m,
            "EUR", date));
        return batch;
    }
}

using System.Security.Claims;
using ExpenseWatch.Api.Data;
using ExpenseWatch.Api.Models;
using ExpenseWatch.Api.Models.Domain;
using ExpenseWatch.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

DotNetEnv.Env.NoClobber().TraversePath().Load();
var builder = WebApplication.CreateBuilder(args);
var encodedKey = Environment.GetEnvironmentVariable("Jwt__SigningKey");
if (string.IsNullOrWhiteSpace(encodedKey))
    throw new InvalidOperationException("Set Jwt__SigningKey to Base64 of at least 32 random bytes.");
byte[] keyBytes;
try { keyBytes = Convert.FromBase64String(encodedKey); }
catch (FormatException) { throw new InvalidOperationException("Jwt__SigningKey must contain valid Base64."); }
if (keyBytes.Length < 32)
    throw new InvalidOperationException("Jwt__SigningKey must decode to at least 32 bytes.");
var signingKey = new SymmetricSecurityKey(keyBytes);
var issuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is required.");
var audience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is required.");
builder.Services.AddSingleton(new JwtTokenService(issuer, audience, signingKey));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Application")));
builder.Services.AddIdentityCore<IdentityUser>(options => options.User.RequireUniqueEmail = true)
    .AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true, IssuerSigningKey = signingKey,
        ValidateIssuer = true, ValidIssuer = issuer,
        ValidateAudience = true, ValidAudience = audience,
        ValidateLifetime = true, RequireSignedTokens = true, RequireExpirationTime = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.Zero, RoleClaimType = "role"
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var id = context.Principal?.FindFirstValue("sub");
            var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
            if (string.IsNullOrWhiteSpace(id) || await users.FindByIdAsync(id) is null)
                context.Fail("Unknown account.");
        }
    };
});
builder.Services.AddAuthorization();
var webAppOrigin = builder.Configuration["WebAppOrigin"];
if (!string.IsNullOrWhiteSpace(webAppOrigin))
    builder.Services.AddCors(options => options.AddPolicy("WebApp", policy =>
        policy.WithOrigins(webAppOrigin).WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT")));
builder.Services.AddScoped<TransactionService>();
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddHttpClient("Mailpit", client => client.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false, UseCookies = false
    });
if (builder.Configuration.GetValue<bool>("Simulator:Enabled"))
{
    builder.Services.AddHttpClient("Simulator", client => client.Timeout = TimeSpan.FromSeconds(60))
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false, UseCookies = false
        });
    builder.Services.AddHostedService<TransactionSimulator>();
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tectonic Expense Watch", Version = "v1",
        Description = "User-owned invented transactions, recurring income/expenses and email alerts."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Paste the accessToken returned by signup or login."
    });
    options.OperationFilter<BearerSecurityOperationFilter>();
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roles.RoleExistsAsync("User"))
    {
        var result = await roles.CreateAsync(new IdentityRole("User"));
        if (!result.Succeeded)
            throw new InvalidOperationException("Could not initialize the User role.");
    }
}
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tectonic API v1"));
if (!string.IsNullOrWhiteSpace(webAppOrigin))
    app.UseCors("WebApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/signup", async Task<IResult>
    (AuthRequest input, UserManager<IdentityUser> users, AppDbContext db, JwtTokenService tokens) =>
{
    if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        return TypedResults.BadRequest(new AuthErrors(
            [new AuthValidationError("RequiredFields", "Email and password are required.")]));
    await using var transaction = await db.Database.BeginTransactionAsync();
    var email = input.Email.Trim();
    var user = new IdentityUser { UserName = email, Email = email };
    var result = await users.CreateAsync(user, input.Password);
    if (result.Succeeded)
        result = await users.AddToRoleAsync(user, "User");
    if (!result.Succeeded)
        return TypedResults.BadRequest(new AuthErrors(result.Errors
            .Select(error => new AuthValidationError(error.Code, error.Description)).ToArray()));
    foreach (var id in await db.Conditions.Select(c => c.Id).ToArrayAsync())
        db.UserConditions.Add(new UserCondition { UserId = user.Id, ConditionId = id, IsEnabled = true, ChannelId = 1 });
    db.DemoClocks.Add(new DemoClock { UserId = user.Id, Date = new DateOnly(2026, 1, 1) });
    await db.SaveChangesAsync();
    await transaction.CommitAsync();
    return TypedResults.Created((string?)null, tokens.Issue(user, await users.GetRolesAsync(user)));
}).AllowAnonymous().WithName("Signup").WithTags("Authentication")
    .WithSummary("Create a demo account, User role, enabled email preferences and its own clock.")
    .Produces<TokenResponse>(201).Produces<AuthErrors>(400);

app.MapPost("/api/auth/login", async Task<IResult>
    (AuthRequest input, UserManager<IdentityUser> users, JwtTokenService tokens) =>
{
    if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        return TypedResults.BadRequest(new AuthErrors(
            [new AuthValidationError("RequiredFields", "Email and password are required.")]));
    var user = await users.FindByEmailAsync(input.Email.Trim());
    if (user is null || !await users.CheckPasswordAsync(user, input.Password))
        return TypedResults.Json(new AuthError("Invalid email or password."), statusCode: 401);
    return TypedResults.Ok(tokens.Issue(user, await users.GetRolesAsync(user)));
}).AllowAnonymous().WithName("Login").WithTags("Authentication")
    .WithSummary("Verify Identity credentials and return a 60-minute JWT.")
    .Produces<TokenResponse>().Produces<AuthErrors>(400).Produces<AuthError>(401);

var api = app.MapGroup("/api").RequireAuthorization();
api.MapPost("/transactions", async Task<IResult>
    (TransactionInput input, ClaimsPrincipal user, AppDbContext db, TransactionService service) =>
{
    if (string.IsNullOrWhiteSpace(input.CounterpartyKey) || string.IsNullOrWhiteSpace(input.CounterpartyName)
        || string.IsNullOrWhiteSpace(input.TransactionKey) || string.IsNullOrWhiteSpace(input.Description)
        || input.Currency != "EUR" || input.Amount <= 0 || decimal.Round(input.Amount, 2) != input.Amount)
        return TypedResults.BadRequest(new ApiError("Provide stream fields and a positive two-decimal EUR amount."));
    var type = await db.TransactionTypes.SingleOrDefaultAsync(t => t.Code == input.Type);
    if (type is null)
        return TypedResults.BadRequest(new ApiError("Unknown transaction type."));
    var owner = Owner(user);
    var clock = await db.DemoClocks.SingleAsync(c => c.UserId == owner);
    if (input.Date != clock.Date)
        return TypedResults.BadRequest(new ApiError("Transaction date must equal your demo date."));
    if (await db.Transactions.AnyAsync(t => t.UserId == owner && t.TransactionTypeId == type.Id
        && t.CounterpartyKey == input.CounterpartyKey && t.TransactionKey == input.TransactionKey
        && t.Currency == input.Currency && t.Date == input.Date))
        return TypedResults.Conflict(new ApiError("Transaction already exists for this stream and date."));
    return TypedResults.Created((string?)null, await service.IngestAsync(owner, input, type, clock));
}).WithName("IngestTransaction").WithTags("Transactions")
    .WithSummary("Store your mock transaction and evaluate enabled alerts.")
    .Produces<TransactionResponse>(201).Produces<ApiError>(400).Produces<ApiError>(409);

api.MapGet("/transactions", async (ClaimsPrincipal user, AppDbContext db, DateOnly? date) =>
{
    var owner = Owner(user);
    var query = db.Transactions.AsNoTracking().Where(t => t.UserId == owner);
    if (date is not null) query = query.Where(t => t.Date == date.Value);
    var items = await query.Join(db.TransactionTypes, t => t.TransactionTypeId, t => t.Id,
        (t, type) => new { Transaction = t, type.Code })
        .OrderBy(x => x.Transaction.Date).ThenBy(x => x.Transaction.Id).ToArrayAsync();
    return TypedResults.Ok(items.Select(x => TransactionRecord.From(x.Transaction, x.Code)).ToArray());
}).WithName("ListTransactions").WithTags("Transactions")
    .WithSummary("List only your transactions; optionally filter one calendar date.");

api.MapGet("/recurring-transactions", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var owner = Owner(user);
    var items = await db.RecurringTransactions.AsNoTracking().Where(r => r.UserId == owner && r.IsActive)
        .Join(db.Transactions.Where(t => t.UserId == owner), r => r.LastTransactionId, t => t.Id,
            (r, t) => new { Recurring = r, Last = t })
        .Join(db.TransactionTypes, x => x.Recurring.TransactionTypeId, t => t.Id,
            (x, type) => new { x.Recurring, x.Last, type.Code })
        .OrderBy(x => x.Recurring.Id).ToArrayAsync();
    return TypedResults.Ok(items.Select(x => RecurringTransactionResponse.From(x.Recurring, x.Last, x.Code)).ToArray());
}).WithName("ListRecurringTransactions").WithTags("Recurring transactions")
    .WithSummary("List your active recurring expenses and income.");

api.MapGet("/notifications", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var owner = Owner(user);
    var items = await db.Notifications.AsNoTracking().Where(n => n.UserId == owner)
        .Join(db.Conditions, n => n.ConditionId, c => c.Id, (n, c) => new { Notification = n, Kind = c.Code })
        .Join(db.NotificationChannels, x => x.Notification.ChannelId, c => c.Id,
            (x, c) => new { x.Notification, x.Kind, Channel = c.Code })
        .OrderBy(x => x.Notification.CreatedAt).ThenBy(x => x.Notification.Id).ToArrayAsync();
    return TypedResults.Ok(items.Select(x => NotificationResponse.From(x.Notification, x.Kind, x.Channel)).ToArray());
}).WithName("ListNotifications").WithTags("Notifications")
    .WithSummary("List only your generated notifications and delivery outcomes.");

api.MapGet("/demo/date", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var owner = Owner(user);
    return TypedResults.Ok(new ClockDate((await db.DemoClocks.AsNoTracking()
        .SingleAsync(c => c.UserId == owner)).Date));
})
    .WithName("GetDemoDate").WithTags("Demo clock").WithSummary("Read your persisted logical date.");

api.MapPut("/demo/date", async Task<IResult>
    (ClockDate input, ClaimsPrincipal user, AppDbContext db, TransactionService service) =>
{
    var owner = Owner(user);
    var clock = await db.DemoClocks.SingleAsync(c => c.UserId == owner);
    if (input.Date < clock.Date)
        return TypedResults.BadRequest(new ApiError("The demo date cannot move backwards."));
    return TypedResults.Ok(await service.AdvanceAsync(owner, clock, input.Date));
}).WithName("AdvanceDemoDate").WithTags("Demo clock")
    .WithSummary("Advance only your clock and evaluate fully elapsed unpaid months.")
    .Produces<ClockAdvanceResponse>().Produces<ApiError>(400);

api.MapGet("/transaction-types", async (AppDbContext db) =>
    TypedResults.Ok(await db.TransactionTypes.AsNoTracking().OrderBy(t => t.Id)
        .Select(t => new LookupResponse(t.Id, t.Code, t.Name)).ToArrayAsync()))
    .WithName("TransactionTypes").WithTags("Lookups").WithSummary("List seeded expense and income types.");

api.MapGet("/conditions", async (AppDbContext db) =>
{
    var items = await db.Conditions.AsNoTracking().OrderBy(c => c.Id).ToArrayAsync();
    var links = await db.ConditionTransactionTypes.AsNoTracking().ToArrayAsync();
    return TypedResults.Ok(items.Select(c => new ConditionResponse(c.Id, c.Code, c.Name,
        links.Where(x => x.ConditionId == c.Id).Select(x => x.TransactionTypeId).Order().ToArray())).ToArray());
}).WithName("Conditions").WithTags("Lookups").WithSummary("List conditions and normalized type applicability.");

api.MapGet("/notification-channels", async (AppDbContext db) =>
    TypedResults.Ok(await db.NotificationChannels.AsNoTracking().OrderBy(c => c.Id)
        .Select(c => new ChannelResponse(c.Id, c.Code, c.Name, c.IsSupported)).ToArrayAsync()))
    .WithName("NotificationChannels").WithTags("Lookups").WithSummary("List channels; only email is supported.");

api.MapGet("/me/conditions", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var owner = Owner(user);
    return TypedResults.Ok(await db.UserConditions.AsNoTracking().Where(s => s.UserId == owner)
        .OrderBy(s => s.ConditionId).Select(s => new ConditionSettingResponse(s.ConditionId, s.IsEnabled, s.ChannelId))
        .ToArrayAsync());
})
    .WithName("MyConditions").WithTags("Preferences").WithSummary("Read your enabled/channel preferences.");

api.MapPut("/me/conditions/{conditionId:int}", async Task<IResult>
    (int conditionId, ConditionSettingInput input, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!await db.Conditions.AnyAsync(c => c.Id == conditionId))
        return TypedResults.NotFound(new ApiError("Condition not found."));
    if (!await db.NotificationChannels.AnyAsync(c => c.Id == input.ChannelId && c.IsSupported))
        return TypedResults.BadRequest(new ApiError("Notification channel is unavailable."));
    var owner = Owner(user);
    var setting = await db.UserConditions.SingleAsync(s => s.UserId == owner && s.ConditionId == conditionId);
    setting.IsEnabled = input.IsEnabled;
    setting.ChannelId = input.ChannelId;
    await db.SaveChangesAsync();
    return TypedResults.Ok(new ConditionSettingResponse(conditionId, setting.IsEnabled, setting.ChannelId));
}).WithName("UpdateMyCondition").WithTags("Preferences")
    .WithSummary("Change your alert preference without stopping recurrence tracking.")
    .Produces<ConditionSettingResponse>().Produces<ApiError>(400).Produces<ApiError>(404);

app.Run();

static string Owner(ClaimsPrincipal user) => user.FindFirstValue("sub")
    ?? throw new InvalidOperationException("Validated user subject is required.");

public sealed class BearerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any()) return;
        operation.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] }
        ];
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
    }
}

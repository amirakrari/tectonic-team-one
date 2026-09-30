using ExpenseWatch.Api.Data;
using ExpenseWatch.Api.Models;
using ExpenseWatch.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
var encodedKey = Environment.GetEnvironmentVariable("Jwt__SigningKey");
if (string.IsNullOrWhiteSpace(encodedKey))
{
    throw new InvalidOperationException("Set Jwt__SigningKey to Base64 of at least 32 random bytes.");
}
byte[] keyBytes;
try
{
    keyBytes = Convert.FromBase64String(encodedKey);
}
catch (FormatException)
{
    throw new InvalidOperationException("Jwt__SigningKey must contain valid Base64.");
}
if (keyBytes.Length < 32)
{
    throw new InvalidOperationException("Jwt__SigningKey must decode to at least 32 bytes.");
}
var signingKey = new SymmetricSecurityKey(keyBytes);
var issuer = builder.Configuration["Jwt:Issuer"]!;
var audience = builder.Configuration["Jwt:Audience"]!;
builder.Services.AddSingleton(new JwtTokenService(issuer, audience, signingKey));
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Identity")));
builder.Services.AddIdentityCore<IdentityUser>(options => options.User.RequireUniqueEmail = true)
    .AddEntityFrameworkStores<AuthDbContext>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        RequireSignedTokens = true,
        RequireExpirationTime = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
var webAppOrigin = builder.Configuration["WebAppOrigin"];
if (!string.IsNullOrWhiteSpace(webAppOrigin))
{
    builder.Services.AddCors(options => options.AddPolicy("WebApp", policy =>
        policy.WithOrigins(webAppOrigin).WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT")));
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Expenses")));
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Expense Watch",
        Version = "v1",
        Description = "Shared mock transactions with Identity signup/login and JWT bearer access."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the accessToken from signup or login."
    });
    options.OperationFilter<BearerSecurityOperationFilter>();
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await authDb.Database.EnsureCreatedAsync();
    await db.Database.EnsureCreatedAsync();
    if (!await db.DemoClocks.AnyAsync(c => c.Id == 1))
    {
        db.DemoClocks.Add(new DemoClock { Id = 1, Date = new DateOnly(2026, 1, 1) });
        await db.SaveChangesAsync();
    }
}

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Watch v1"));
if (!string.IsNullOrWhiteSpace(webAppOrigin))
{
    app.UseCors("WebApp");
}
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/signup", async Task<IResult>
    (AuthRequest input, UserManager<IdentityUser> users, JwtTokenService tokens) =>
{
    if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
    {
        return TypedResults.BadRequest(new AuthErrors(
            [new AuthValidationError("RequiredFields", "Email and password are required.")]));
    }
    var email = input.Email.Trim();
    var user = new IdentityUser { UserName = email, Email = email };
    var result = await users.CreateAsync(user, input.Password);
    if (!result.Succeeded)
    {
        return TypedResults.BadRequest(new AuthErrors(result.Errors
            .Select(error => new AuthValidationError(error.Code, error.Description)).ToArray()));
    }
    return TypedResults.Created((string?)null, tokens.Issue(user));
}).AllowAnonymous().WithName("Signup").WithTags("Authentication")
    .WithSummary("Create an unverified demo account and return a 60-minute JWT.")
    .Produces<TokenResponse>(StatusCodes.Status201Created)
    .Produces<AuthErrors>(StatusCodes.Status400BadRequest);

app.MapPost("/api/auth/login", async Task<IResult>
    (AuthRequest input, UserManager<IdentityUser> users, JwtTokenService tokens) =>
{
    if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
    {
        return TypedResults.BadRequest(new AuthErrors(
            [new AuthValidationError("RequiredFields", "Email and password are required.")]));
    }
    var user = await users.FindByEmailAsync(input.Email.Trim());
    if (user is null || !await users.CheckPasswordAsync(user, input.Password))
    {
        return TypedResults.Json(new AuthError("Invalid email or password."),
            statusCode: StatusCodes.Status401Unauthorized);
    }
    return TypedResults.Ok(tokens.Issue(user));
}).AllowAnonymous().WithName("Login").WithTags("Authentication")
    .WithSummary("Verify demo credentials and return a 60-minute JWT.")
    .Produces<TokenResponse>()
    .Produces<AuthErrors>(StatusCodes.Status400BadRequest)
    .Produces<AuthError>(StatusCodes.Status401Unauthorized);

app.MapPost("/api/transactions", async (TransactionInput input, ExpenseService service) =>
    TypedResults.Created((string?)null, await service.IngestAsync(input)))
    .RequireAuthorization().WithName("IngestTransaction").WithTags("Transactions")
    .WithSummary("Store one invented income or expense and attempt generated emails.");

app.MapGet("/api/transactions", async (AppDbContext db) =>
    TypedResults.Ok(await db.Transactions.AsNoTracking().OrderBy(t => t.Date).ThenBy(t => t.Id).ToArrayAsync()))
    .RequireAuthorization().WithName("ListTransactions").WithTags("Transactions")
    .WithSummary("List stored transactions in date and ID order.");

app.MapGet("/api/recurring-expenses", async (AppDbContext db) =>
{
    var items = await db.RecurringExpenses.AsNoTracking().Where(r => r.Active).OrderBy(r => r.Id).ToArrayAsync();
    return TypedResults.Ok(items.Select(RecurringExpenseResponse.From).ToArray());
}).RequireAuthorization().WithName("ListRecurringExpenses").WithTags("Recurring expenses")
    .WithSummary("List active recurring expenses and their next expected dates.");

app.MapGet("/api/notifications", async (AppDbContext db) =>
{
    var items = await db.Notifications.AsNoTracking().OrderBy(n => n.Id).ToArrayAsync();
    return TypedResults.Ok(items.Select(NotificationResponse.From).ToArray());
}).RequireAuthorization().WithName("ListNotifications").WithTags("Notifications")
    .WithSummary("List persisted notifications and SMTP delivery outcomes.");

app.MapGet("/api/demo/date", async (AppDbContext db) =>
    TypedResults.Ok(new ClockDate((await db.DemoClocks.AsNoTracking().SingleAsync(c => c.Id == 1)).Date)))
    .RequireAuthorization().WithName("GetDemoDate").WithTags("Demo clock")
    .WithSummary("Read the persisted logical date.");

app.MapPut("/api/demo/date", async Task<Results<Ok<ClockAdvanceResponse>, BadRequest<ClockError>>>
    (ClockDate input, AppDbContext db, ExpenseService service) =>
{
    var clock = await db.DemoClocks.SingleAsync(c => c.Id == 1);
    if (input.Date < clock.Date)
    {
        return TypedResults.BadRequest(new ClockError("The demo date cannot move backwards."));
    }
    return TypedResults.Ok(await service.AdvanceAsync(clock, input.Date));
}).RequireAuthorization().WithName("AdvanceDemoDate").WithTags("Demo clock")
    .WithSummary("Advance the clock and remove expenses missing from fully elapsed months.");

app.Run();

public sealed class BearerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
        {
            return;
        }
        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            }
        ];
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
    }
}

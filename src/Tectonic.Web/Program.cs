using System.Globalization;
using MudBlazor.Services;
using Tectonic.Web.Components;
using Tectonic.Web.Services;

// Same number/date format on every machine: dot decimals, dd/MM/yyyy dates.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-GB");

DotNetEnv.Env.NoClobber().TraversePath().Load();
var builder = WebApplication.CreateBuilder(args);
foreach (var (variable, setting) in new (string, string)[]
{
    ("API_BASE_URL", "Api:BaseUrl"),
    ("API_USE_MOCK", "Api:UseMock"),
    ("SIMULATOR_DEMO_LOGIN_ENABLED", "SimulatorDemoLoginEnabled"),
    ("SAMPLE_ACCOUNT_NAME", "SampleAccounts:0:Name"),
    ("SAMPLE_ACCOUNT_EMAIL", "SampleAccounts:0:Email"),
    ("SAMPLE_ACCOUNT_PASSWORD", "SampleAccounts:0:Password")
})
{
    if (Environment.GetEnvironmentVariable(variable) is { } value)
        builder.Configuration[setting] = value;
}
var useMock = builder.Configuration.GetValue<bool>("Api:UseMock");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    // Adding the same payment twice should still show a second "Added" confirmation.
    config.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddScoped<UiState>();
builder.Services.AddScoped<Loc>();
builder.Services.AddSingleton<AccountStore>();

if (useMock)
{
    builder.Services.AddScoped<IApiClient, MockApiClient>();
}
else
{
    var endpoint = builder.Configuration["Api:BaseUrl"];
    if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var apiUrl)
        || (apiUrl.Scheme != "http" && apiUrl.Scheme != "https")
        || !apiUrl.AbsoluteUri.EndsWith('/'))
        throw new InvalidOperationException("Set API_BASE_URL in .env to an HTTP(S) API URL ending in /.");
    builder.Services.AddHttpClient("Api", client => client.BaseAddress = apiUrl)
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false, UseCookies = false
        });
    builder.Services.AddScoped<IApiClient>(services => new ApiClient(
        services.GetRequiredService<IHttpClientFactory>().CreateClient("Api"),
        services.GetRequiredService<UiState>()));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

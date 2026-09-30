using System.Globalization;
using MudBlazor.Services;
using Tectonic.Web.Components;
using Tectonic.Web.Services;

// Same number/date format on every machine: dot decimals, dd/MM/yyyy dates.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-GB");

var builder = WebApplication.CreateBuilder(args);

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

if (builder.Configuration.GetValue<bool>("Api:UseMock"))
{
    builder.Services.AddScoped<IApiClient, MockApiClient>();
}
else
{
    builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!));
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

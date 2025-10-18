using Microsoft.AspNetCore.SignalR;
using ObsidianAI.Web.Components;
using ObsidianAI.Web.Hubs;
using ObsidianAI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR
builder.Services.AddSignalR();

// Register named HttpClient for API communication
builder.Services.AddHttpClient("ObsidianAI.Api", client =>
{
    // Use Aspire service discovery if available, otherwise use direct port
    var apiEndpoint = builder.Configuration.GetConnectionString("api")
                      ?? "http://localhost:5095";

    client.BaseAddress = new Uri(apiEndpoint);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Register ChatService with proper API endpoint configuration
builder.Services.AddHttpClient<IChatService, ChatService>(client =>
{
    // Use Aspire service discovery if available, otherwise use direct port
    var apiEndpoint = builder.Configuration.GetConnectionString("api")
                      ?? "http://localhost:5095";

    client.BaseAddress = new Uri(apiEndpoint);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Register VaultService for raw file operations without LLM processing
builder.Services.AddHttpClient<IVaultService, VaultService>(client =>
{
    // Use Aspire service discovery if available, otherwise use direct port
    var apiEndpoint = builder.Configuration.GetConnectionString("api")
                      ?? "http://localhost:5095";

    client.BaseAddress = new Uri(apiEndpoint);
    client.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // UseExceptionHandler provides Blazor-specific error page for unhandled exceptions
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    // In development, let exceptions bubble up for detailed error pages
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub
app.MapHub<ChatHub>("/chathub");

app.Run();
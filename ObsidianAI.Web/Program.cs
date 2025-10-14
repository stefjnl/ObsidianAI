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

// Register ChatService
builder.Services.AddHttpClient<IChatService, ChatService>(client =>
{
    // The base address will be configured by Aspire service discovery
    // This is a fallback for development when not running through Aspire
    client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:Api"] ?? "https://localhost:7001");
});

// Register ChatService as scoped
builder.Services.AddScoped<IChatService, ChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
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
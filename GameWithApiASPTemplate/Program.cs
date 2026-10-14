using GameWithApiASPTemplate;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
var configuredPathBase = Environment.GetEnvironmentVariable("GAME_PATH_BASE")?.Trim();
var gamePathBase = string.IsNullOrWhiteSpace(configuredPathBase)
    ? PathString.Empty
    : new PathString($"/{configuredPathBase.Trim('/')}");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<API>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = Environment.GetEnvironmentVariable("GAME_COOKIE_NAME")
        ?? "lingvogameos_aspnet_template_session";
    options.Cookie.Path = gamePathBase.HasValue ? gamePathBase.Value : "/";
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (gamePathBase.HasValue)
{
    app.Use((context, next) =>
    {
        context.Request.PathBase = gamePathBase;
        return next();
    });
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// TLS завершается на портале/reverse proxy; контейнер игры слушает внутренний HTTP.
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

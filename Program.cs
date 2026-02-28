using System.Net;
using System.Net.Sockets;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.MapGet("/connection-info", (HttpContext context) =>
{
    var info = BuildConnectionInfo(context);
    return Results.Ok(info);
});

app.MapGet("/info", async (HttpContext context) =>
{
    var info = BuildConnectionInfo(context);
    var payloadObj = new { lanBaseUrls = info.LanBaseUrls };
    var json = JsonSerializer.Serialize(payloadObj, new JsonSerializerOptions { WriteIndented = true });

    try
    {
        var path = Path.Combine(context.RequestServices.GetRequiredService<IHostEnvironment>().ContentRootPath, "info.json");
        await File.WriteAllTextAsync(path, json);
    }
    catch
    {
        // ignore file write errors; still return JSON
    }

    return Results.Content(json, "application/json");
});

// Root: simple page indicating QR is disabled
app.MapGet("/", (HttpContext context) =>
{
    var html = "<html><head><title>nanoserver</title></head><body style=\"font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; padding:32px;\">" +
               "<h2>nanoserver</h2><p>QR functionality is currently disabled. Use <a href=\"/info\">/info</a> to get connection JSON.</p></body></html>";
    return Results.Content(html, "text/html");
});

// /weatherforecast: return LAN base URLs as JSON (simple manual response)
// /weatherforecast removed — not needed

app.Run();

static ConnectionInfo BuildConnectionInfo(HttpContext context)
{
    var request = context.Request;
    var scheme = request.Scheme;
    var port = request.Host.Port ?? (string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80);

    var localIps = GetLocalIpv4Addresses();
    var lanBaseUrls = localIps.Select(ip => $"{scheme}://{ip}:{port}").ToArray();

    return new ConnectionInfo(lanBaseUrls);
}

static string[] GetLocalIpv4Addresses()
{
    return Dns.GetHostEntry(Dns.GetHostName())
        .AddressList
        .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
        .Select(address => address.ToString())
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}

record ConnectionInfo(
    string[] LanBaseUrls);


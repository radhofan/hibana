using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Hibana.Api;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.Configure<WeatherOptions>(builder.Configuration.GetSection("Weather"));
builder.Services.Configure<ProviderOptions>("WeatherProvider", builder.Configuration.GetSection("WeatherProvider"));
builder.Services.Configure<ProviderOptions>("GeocodingProvider", builder.Configuration.GetSection("GeocodingProvider"));
builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoWeatherProvider>(client => ConfigureClient(client, builder.Configuration, "WeatherProvider"));
builder.Services.AddHttpClient<IReverseGeocodingProvider, NominatimLocationProvider>(client => { ConfigureClient(client, builder.Configuration, "GeocodingProvider"); client.DefaultRequestHeaders.UserAgent.ParseAdd("HibanaWeatherExplorer/1.0 (contact: local-development)"); });
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) => await Results.Problem(statusCode: 429, title: "Too many requests", detail: "Please wait before requesting more weather data.", type: "https://hibana.app/problems/rate-limit-exceeded").ExecuteAsync(context.HttpContext);
    options.AddPolicy("weather", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"]).AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseExceptionHandler(error => error.Run(async context => { var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error; var status = exception is ProviderUnavailableException ? 503 : 500; context.Response.StatusCode = status; await Results.Problem(statusCode: status, title: status == 503 ? "Weather data is temporarily unavailable" : "An unexpected error occurred", detail: status == 503 ? "The weather provider could not complete the request." : "The request could not be completed.", type: status == 503 ? "https://hibana.app/problems/weather-provider-unavailable" : "https://hibana.app/problems/unexpected-error").ExecuteAsync(context); }));
app.UseCors(); app.UseRateLimiter();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers().RequireRateLimiting("weather");
app.MapHealthChecks("/health/live"); app.MapHealthChecks("/health/ready");
app.Run();

static void ConfigureClient(HttpClient client, IConfiguration configuration, string section) { var options = configuration.GetSection(section).Get<ProviderOptions>() ?? new(); client.BaseAddress = new Uri(options.BaseUrl); client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds); client.DefaultRequestHeaders.Accept.ParseAdd("application/json"); }
public partial class Program { }

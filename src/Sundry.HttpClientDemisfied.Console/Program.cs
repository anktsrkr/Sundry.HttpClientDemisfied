using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Sundry.HttpClientDemisfied.Console;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICachedTokenService, CachedTokenService>();
builder.Services.AddSingleton<IExternalTokenService, ExternalTokenService>();

builder.Services.AddTransient<TokenRetrievalHandler>();

var httpClientBuilder = builder.Services.AddHttpClient<IWeatherForecast, WeatherForecast>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5298/");
});
    httpClientBuilder.AddResilienceHandler("Retry", (resiliencePipelineBuilder, context) =>
    {
        resiliencePipelineBuilder
            .AddRetry(new HttpRetryStrategyOptions
            {
                ShouldHandle = args => args.Outcome switch
                {
                    { Exception: HttpRequestException } => PredicateResult.True(),
                    { Result.StatusCode: HttpStatusCode.Unauthorized } => PredicateResult.False(),
                    { Result.IsSuccessStatusCode: false } => PredicateResult.True(),
                    _ => PredicateResult.False()
                },
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(10),
                UseJitter = true,
                OnRetry = (outcome) =>
                {
                    Console.WriteLine(outcome.Outcome.Exception == null);
                    Console.WriteLine("Second retry");

                    return ValueTask.CompletedTask;
                }
            })
            .AddRetry(new HttpRetryStrategyOptions
            {
                ShouldHandle = args => args.Outcome switch
                {
                    { Result.StatusCode: HttpStatusCode.Unauthorized } => PredicateResult.True(),
                    _ => PredicateResult.False()
                },
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(10),
                UseJitter = true,
                OnRetry = async (outcome) =>
                {
                    await context.ServiceProvider.GetRequiredService<ICachedTokenService>().RefreshTokenAsync(outcome.Context);
                }
            });
    });

    httpClientBuilder.AddHttpMessageHandler<TokenRetrievalHandler>();

using var host = builder.Build();
await ExemplifyServiceLifetime(host.Services);

await host.RunAsync();
return;

async Task ExemplifyServiceLifetime(IServiceProvider hostProvider)
{
    using var scope = hostProvider.CreateScope();
    var provider = scope.ServiceProvider;
    var weatherForecast = provider.GetRequiredService<IWeatherForecast>();
    var forecasts = await weatherForecast.GetWeatherForecastAsync();
    if (forecasts is not null)
    {
        foreach (var forecast in forecasts)
        {
            Console.WriteLine(forecast);
        }
    }
}
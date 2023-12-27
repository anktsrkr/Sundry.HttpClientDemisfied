using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Sundry.HttpClientDemisfied.Console;

// Weather Forecast Client
public interface IWeatherForecast
{ 
    Task<IEnumerable<Weather>?> GetWeatherForecastAsync();
}

public class  WeatherForecast : IWeatherForecast{
    private readonly HttpClient _httpClient;

    public WeatherForecast(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Weather>?> GetWeatherForecastAsync()
    {
        var response = await _httpClient.GetAsync("/weatherforecast");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<Weather>>();
    }   
}


public record Weather(DateOnly Date, int TemperatureC, string? Summary);
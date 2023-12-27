using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Polly;

namespace Sundry.HttpClientDemisfied.Console;
public record Token
{
    public static Token Empty => new();

    [JsonPropertyName("token_type")]
    public string Scheme { get; set; } = default!;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = default!;

    [JsonPropertyName("expires_in")]
    public double ExpiresIn { get; set; } = default!;
}
public interface ICachedTokenService
{
    ValueTask<Token> GetTokenAsync(ResilienceContext context);
    Task<Token> RefreshTokenAsync(ResilienceContext context);
}

public interface IExternalTokenService
{
    Task<Token> GetTokenAsync();
}

public class ExternalTokenService : IExternalTokenService
{
    public Task<Token> GetTokenAsync()=>  Task.FromResult(new Token()
            { AccessToken = Guid.NewGuid().ToString("N"), ExpiresIn = 3900, Scheme = "Bearer" });
    
}

public class CachedTokenService : ICachedTokenService
{   
    private const string CacheKey = nameof(CachedTokenService);
    private readonly IMemoryCache _memoryCache;
    private readonly IExternalTokenService _externalTokenService;
    public CachedTokenService(IMemoryCache memoryCache, IExternalTokenService externalTokenService)
    {
        _memoryCache = memoryCache;
        _externalTokenService = externalTokenService;
    }
    public async ValueTask<Token> GetTokenAsync(ResilienceContext context)
    {
        if (!_memoryCache.TryGetValue(CacheKey, out Token? cacheValue))
        {
            cacheValue = await RefreshTokenAsync(context);
        }
        return cacheValue!;
    }

    public async Task<Token> RefreshTokenAsync(ResilienceContext context)
    {
        var token = await _externalTokenService.GetTokenAsync();
        
        if (token != Token.Empty)
        {
           var expiresIn = token.ExpiresIn>0?token.ExpiresIn-10:token.ExpiresIn;
           
           _memoryCache.Set(CacheKey, token, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(expiresIn)));        
        }

        context.Properties.Set(new ResiliencePropertyKey<Token>("AccessKey"), token);
        
        return token;
    }
}
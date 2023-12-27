using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Sundry.HttpClientDemisfied.Console;

public class TokenRetrievalHandler : DelegatingHandler
{   
    private readonly ICachedTokenService _cachedTokenService;

    public TokenRetrievalHandler(ICachedTokenService cachedTokenService)
    {
        _cachedTokenService = cachedTokenService;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context =  ResilienceContextPool.Shared.Get(cancellationToken);
        context.Properties.TryGetValue(new ResiliencePropertyKey<Token>("AccessToken"), out var token);
       
        token ??= await _cachedTokenService.GetTokenAsync(context);

        request.Headers.Authorization = new AuthenticationHeaderValue(token.Scheme, token.AccessToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
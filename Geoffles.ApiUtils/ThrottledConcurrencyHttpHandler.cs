using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Geoffles.ApiUtils
{
    /// <summary>
    /// HTTP Throttling handler using a token bucket implementation.
    /// </summary>
    /// This throttle will limit the concurrency of requests, as well as use a token bucket for rate limiting.
    /// A token bucket has a fixed fill rate and capacity. Requests must acquire a token before proceeding. The token bucket is threadless.
    /// 

    public sealed class ThrottledConcurrencyHttpHandler : DelegatingHandler
    {
        private readonly SemaphoreSlim _sem;
        private readonly TokenBucket _bucket;

        public ThrottledConcurrencyHttpHandler(int maxConcurrency, int tokenPeriodMillis, int maxTokens, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            _bucket = new TokenBucket(tokenPeriodMillis, maxTokens);
            _sem = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            await _sem.WaitAsync(cancellationToken);
            await _bucket.WaitTokenAsync(cancellationToken);

            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                _sem.Release();
            }
        }
    }
}

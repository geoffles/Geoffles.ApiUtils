using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Geoffles.ApiUtils
{
    public interface ICacheProvider
    {
        bool Contains(HttpRequestMessage request);
        Task<HttpResponseMessage> this[HttpRequestMessage request] { get; set; }
        bool Cachable(HttpRequestMessage request);

    }

    public class CachedHttpHandler : DelegatingHandler
    {
        private ICacheProvider _cache;
        public CachedHttpHandler(ICacheProvider cacheProvider, HttpMessageHandler innerHandler)
        {
            _cache = cacheProvider;
            InnerHandler = innerHandler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_cache.Contains(request))
            {
                return _cache[request];
            }
            else
            {
                var response = base.SendAsync(request, cancellationToken);
                _cache[request] = response;
                return response;
            }
            
        }
    }
}

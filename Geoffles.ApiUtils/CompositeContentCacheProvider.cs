using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Geoffles.ApiUtils
{
    class CompositeContentCacheProvider : ICacheProvider
    {
        private IList<ICacheProvider> _providers;

        public Task<HttpResponseMessage> this[HttpRequestMessage key] { get => Fetch(key); set => Store(key, value).Wait(); }

        private Task<HttpResponseMessage> Fetch(HttpRequestMessage key)
        {
            var targetProvider = _providers.FirstOrDefault(p => p.Cachable(key));

            if (targetProvider == null)
            {
                throw new ArgumentException("Not Cacheable");
            }

            return targetProvider[key];
        }

        private async Task Store(HttpRequestMessage request, Task<HttpResponseMessage> response)
        {
            var targetProvider = _providers.FirstOrDefault(p => p.Cachable(request));

            if (targetProvider == null)
            {
                throw new ArgumentException("Not Cacheable");
            }

            var r = await response;

            targetProvider[request] = response;
        }

        public bool Cachable(HttpRequestMessage key)
        {
            return _providers.Any(p => p.Cachable(key));
        }

        public bool Contains(HttpRequestMessage key)
        {
            var targetProvider = _providers.FirstOrDefault(p => p.Cachable(key));

            if (targetProvider == null)
            {
                return false;
            }

            return targetProvider.Contains(key);
        }
    }
}

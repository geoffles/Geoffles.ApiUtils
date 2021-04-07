using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Geoffles.ApiUtils
{
    public class InMemoryContentCacheProvider : ICacheProvider
    {
        public interface ISubProvider
        {
            bool Cachable(HttpRequestMessage request);

            object GetKey(HttpRequestMessage request);
        }

        private class DefaultSubProvider : ISubProvider
        {
            public bool Cachable(HttpRequestMessage request)
            {
                return !(request.Headers.CacheControl.NoStore);
            }

            public object GetKey(HttpRequestMessage request)
            {
                return request.RequestUri.ToString();
            }
        }

        private readonly ISubProvider _provider;
        private readonly Dictionary<object, HttpContent> _content = new Dictionary<object, HttpContent>();

        public Task<HttpResponseMessage> this[HttpRequestMessage key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public InMemoryContentCacheProvider(ISubProvider provider)
        {
            _provider = provider;
        }

        public bool Contains(HttpRequestMessage request)
        {
            return _content.ContainsKey(_provider.GetKey(request));
        }

        public bool Cachable(HttpRequestMessage request)
        {
            return _provider.Cachable(request);
        }
    }
}

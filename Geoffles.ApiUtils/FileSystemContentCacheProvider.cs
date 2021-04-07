using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Geoffles.ApiUtils
{
    public class FileSystemContentCacheProvider : ICacheProvider
    {
        public interface ISubProvider
        {
            string GetFilename(HttpRequestMessage request);
            bool Cachable(HttpRequestMessage requestMessage);
        }

        public class DefaultRequestToFileProvider : ISubProvider
        {
            private readonly string _basePath;

            public DefaultRequestToFileProvider(string basePath)
            {
                _basePath = basePath;
            }

            public bool Cachable(HttpRequestMessage requestMessage)
            {
                return !(requestMessage.Headers.CacheControl.NoStore);
            }

            public string GetFilename(HttpRequestMessage request)
            {
                return Path.Combine(_basePath, request.RequestUri.Segments.Last());
            }
        }

        private readonly ISubProvider _provider;

        public FileSystemContentCacheProvider(string basePath) : this(new DefaultRequestToFileProvider(basePath)) {}

        public FileSystemContentCacheProvider(ISubProvider provider)
        {
            _provider = provider;
        }

        public Task<HttpResponseMessage> this[HttpRequestMessage request] { get => Fetch(request); set => Store(request, value).Wait(); }

        public bool Contains(HttpRequestMessage key)
        {
            return File.Exists(_provider.GetFilename(key));
        }

        private async Task<HttpResponseMessage> Fetch(HttpRequestMessage request)
        {
            var cachedFilename = _provider.GetFilename(request);
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            var memStream = new MemoryStream();
            using (var f = new FileStream(cachedFilename, FileMode.Open, FileAccess.Read))
            {
                await f.CopyToAsync(memStream);
            }
            response.Content = new StreamContent(memStream);
            return response;
        }

        private async Task Store(HttpRequestMessage request, Task<HttpResponseMessage> response)
        {
            var cachedFilename = _provider.GetFilename(request);

            using (var f = new FileStream(cachedFilename, FileMode.Create, FileAccess.Write))
            {
                var r = await response;
                await r.Content.CopyToAsync(f);
            }
        }

        public bool Cachable(HttpRequestMessage request)
        {
            return _provider.Cachable(request);
        }
    }
}

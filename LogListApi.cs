using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cats.CertificateTransparency.Services;

namespace Cats.CertificateTransparency.Api
{
    public class LogListApi : ILogListApi
    {
        private readonly string _logListUrl;
        private readonly string _logListSignatureUrl;
        private readonly string _logListPublicKeyUrl;
        private readonly HttpClient _httpClient;

        public LogListApi(string logListUrl, string logListSignatureUrl = null, string logListPublicKeyUrl = null)
        {
            _logListUrl = logListUrl;
            _logListSignatureUrl = logListSignatureUrl;
            _logListPublicKeyUrl = logListPublicKeyUrl;
            _httpClient = new HttpClient();
        }

        public async Task<byte[]> GetLogListPublicKeyAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_logListPublicKeyUrl))
                return null;

            using (var msg = new HttpRequestMessage(HttpMethod.Get, _logListPublicKeyUrl))
            {
                msg.Headers.Add("Cache-Control", "no-cache");
                msg.Headers.Add("Max-Size", "1048576");

                var result = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                result.EnsureSuccessStatusCode();

                var pem = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                return LogListService.ReadPemPublicKey(pem);
            }
        }

        public async Task<byte[]> GetLogListAsync(CancellationToken cancellationToken)
        {
            using (var msg = new HttpRequestMessage(HttpMethod.Get, _logListUrl))
            {
                msg.Headers.Add("Cache-Control", "no-cache");
                msg.Headers.Add("Max-Size", "1048576");

                var result = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                result.EnsureSuccessStatusCode();

                return await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        public async Task<byte[]> GetLogListSignatureAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_logListSignatureUrl))
                return null;

            using (var msg = new HttpRequestMessage(HttpMethod.Get, _logListSignatureUrl))
            {
                msg.Headers.Add("Cache-Control", "no-cache");
                msg.Headers.Add("Max-Size", "512");

                var result = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                result.EnsureSuccessStatusCode();

                return await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        public Task<(byte[], byte[])> GetLogListWithSigAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

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
    public class AppleLogListApi : ILogListApi
    {

       // private readonly string _logListUrl;
        private readonly HttpClient _httpClient;

        public AppleLogListApi()
        {
            _httpClient = new HttpClient();
        }

        public Task<byte[]> GetLogListPublicKeyAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<byte[]>(null);
            //using (var msg = new HttpRequestMessage(HttpMethod.Get, "log_list_pubkey.pem"))
            //{
            //    msg.Headers.Add("Cache-Control", "no-cache");
            //    msg.Headers.Add("Max-Size", "1048576");

            //    var result = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
            //    result.EnsureSuccessStatusCode();

            //    var pem = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            //    return LogListService.ReadPemPublicKey(pem);
            //}
        }

        public async Task<byte[]> GetLogListAsync(CancellationToken cancellationToken)
        {
            using (var msg = new HttpRequestMessage(HttpMethod.Get, "https://valid.apple.com/ct/log_list/current_log_list.json"))
            {
                msg.Headers.Add("Cache-Control", "no-cache");
                msg.Headers.Add("Max-Size", "1048576");

                var result = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                result.EnsureSuccessStatusCode();

                return await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        public Task<byte[]> GetLogListSignatureAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<byte[]>(null);
            //using (var msg = new HttpRequestMessage(HttpMethod.Get, "log_list.sig"))
            //{
            //    msg.Headers.Add("Cache-Control", "no-cache");
            //    msg.Headers.Add("Max-Size", "512");

            //    var result = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
            //    result.EnsureSuccessStatusCode();

            //    return await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            //}
        }

        public async Task<(byte[], byte[])> GetLogListWithSigAsync(CancellationToken cancellationToken)
        {
            using (var msg = new HttpRequestMessage(HttpMethod.Get, "log_list.zip"))
            {
                msg.Headers.Add("Cache-Control", "no-cache");
                msg.Headers.Add("Max-Size", "2097152");

                var result = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                result.EnsureSuccessStatusCode();

                using (var stream = await result.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
                {
                    if (archive.Entries.Count != 2)
                        throw new InvalidOperationException($"Expected 2 files from log list zip, got {archive.Entries.Count}");

                    var logListEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".json"))
                        ?? throw new InvalidDataException($"Could not find log list json entry");

                    var logListSigEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".sig"))
                        ?? throw new InvalidDataException($"Could not find log list signature entry");

                    using (var logListStream = logListEntry.Open())
                    using (var logListSigStream = logListSigEntry.Open())
                    using (var listMs = new MemoryStream())
                    using (var sigMs = new MemoryStream())
                    {
                        await Task.WhenAll(
                            logListStream.CopyToAsync(listMs), //, cancellationToken),
                            logListSigStream.CopyToAsync(sigMs)).ConfigureAwait(false); //, cancellationToken)).ConfigureAwait(false);

                        return (listMs.ToArray(), sigMs.ToArray());
                    }
                }
            }
        }
    }
}

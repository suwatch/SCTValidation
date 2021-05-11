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
    public class GoogleLogListApi : ILogListApi
    {
        // https://www.gstatic.com/ct/log_list/v2/log_list_pubkey.pem
        public const string GoogleLogListPublicKey = "-----BEGIN PUBLIC KEY-----\nMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAsu0BHGnQ++W2CTdyZyxv\nHHRALOZPlnu/VMVgo2m+JZ8MNbAOH2cgXb8mvOj8flsX/qPMuKIaauO+PwROMjiq\nfUpcFm80Kl7i97ZQyBDYKm3MkEYYpGN+skAR2OebX9G2DfDqFY8+jUpOOWtBNr3L\nrmVcwx+FcFdMjGDlrZ5JRmoJ/SeGKiORkbbu9eY1Wd0uVhz/xI5bQb0OgII7hEj+\ni/IPbJqOHgB8xQ5zWAJJ0DmG+FM6o7gk403v6W3S8qRYiR84c50KppGwe4YqSMkF\nbLDleGQWLoaDSpEWtESisb4JiLaY4H+Kk0EyAhPSb+49JfUozYl+lf7iFN3qRq/S\nIXXTh6z0S7Qa8EYDhKGCrpI03/+qprwy+my6fpWHi6aUIk4holUCmWvFxZDfixox\nK0RlqbFDl2JXMBquwlQpm8u5wrsic1ksIv9z8x9zh4PJqNpCah0ciemI3YGRQqSe\n/mRRXBiSn9YQBUPcaeqCYan+snGADFwHuXCd9xIAdFBolw9R9HTedHGUfVXPJDiF\n4VusfX6BRR/qaadB+bqEArF/TzuDUr6FvOR4o8lUUxgLuZ/7HO+bHnaPFKYHHSm+\n+z1lVDhhYuSZ8ax3T0C3FZpb7HMjZtpEorSV5ElKJEJwrhrBCMOD8L01EoSPrGlS\n1w22i9uGHMn/uGQKo28u7AsCAwEAAQ==\n-----END PUBLIC KEY-----";
        public const string GoogleLogListUrl = "https://www.gstatic.com/ct/log_list/v2/";


        // https://github.com/google/certificate-transparency-community-site/blob/master/docs/google/known-logs.md
        // https://www.gstatic.com/ct/log_list/v2/log_list.json
        // https://www.gstatic.com/ct/log_list/v2/log_list.sig verified with https://www.gstatic.com/ct/log_list/v2/log_list_pubkey.pem
        // all known: https://www.gstatic.com/ct/log_list/v2/all_logs_list.json

        private readonly string _googleLogListUrl;
        private readonly HttpClient _httpClient;

        public GoogleLogListApi()
        {
            _googleLogListUrl = GoogleLogListUrl;
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(_googleLogListUrl)
            };
        }

        public async Task<byte[]> GetLogListPublicKeyAsync(CancellationToken cancellationToken)
        {
            using (var msg = new HttpRequestMessage(HttpMethod.Get, "log_list_pubkey.pem"))
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
            using (var msg = new HttpRequestMessage(HttpMethod.Get, "log_list.json"))
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
            using (var msg = new HttpRequestMessage(HttpMethod.Get, "log_list.sig"))
            {
                msg.Headers.Add("Cache-Control", "no-cache");
                msg.Headers.Add("Max-Size", "512");

                var result = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                result.EnsureSuccessStatusCode();

                return await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
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

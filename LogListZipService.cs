using Cats.CertificateTransparency.Api;
using Cats.CertificateTransparency.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cats.CertificateTransparency.Services
{
    public class LogListZipService : LogListService
    {
        private readonly SemaphoreSlim _logListSemaphore = new SemaphoreSlim(1, 1);

        public LogListZipService(
            ILogListApi logListApi,
            ILogStoreService logStoreService) : base(logListApi, logStoreService)
        {
        }

        public async override Task<LogListRoot> GetLogListRootAsync(CancellationToken cancellationToken)
        {
            var logListRoot = default(LogListRoot);

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            if (!LogStoreService.TryGetValue(LogListRootKey, out logListRoot))
            {
                await _logListSemaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (!LogStoreService.TryGetValue(LogListRootKey, out logListRoot))
                    {
                        var logListBytes = await LogListApi.GetLogListWithSigAsync(cancellationToken).ConfigureAwait(false);
                        var logListPublicKey = await LogListApi.GetLogListPublicKeyAsync(cancellationToken).ConfigureAwait(false);

                        if (logListBytes.sig != null || logListPublicKey != null)
                        {
                            var isValid = VerifyLogListSignature(logListBytes.list, logListBytes.sig, logListPublicKey);

                            if (!isValid)
                                throw new InvalidDataException("Log list failed signature verification!");
                        }

                        logListRoot = Deserialise<LogListRoot>(logListBytes.list);

                        if (logListRoot?.Operators != null)
                        {
                            LogStoreService.SetValue(LogListRootKey, logListRoot);

                            foreach (var op in logListRoot?.Operators)
                            {
                                Console.WriteLine($"{string.Join(";", op.Email)}");
                                foreach (var log in op.Logs)
                                {
                                    Console.WriteLine($"   {BitConverter.ToString(Convert.FromBase64String(log.LogId)).Replace("-", string.Empty).ToLowerInvariant()} {log.Description}");
                                }
                                Console.WriteLine();
                            }
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    _logListSemaphore.Release();
                }
            }

            stopwatch.Stop();

            return logListRoot;
        }
    }
}

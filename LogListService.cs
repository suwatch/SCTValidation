﻿using Cats.CertificateTransparency.Api;
using Cats.CertificateTransparency.Models;
//using Cats.CertificateTransparency.Models;
using Newtonsoft.Json;
using Org.BouncyCastle.Security;
//using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cats.CertificateTransparency.Services
{
    public class LogListService : ILogListService
    {
        protected const string LogListRootKey = nameof(Cats) + "_" + nameof(LogListService) + "_" + nameof(GetLogListRootAsync);
        protected const string LogDictionaryKey = nameof(Cats) + "_" + nameof(LogListService) + "_" + nameof(GetLogDictionaryAsync);

        protected readonly ILogListApi LogListApi;
        protected readonly ILogStoreService LogStoreService;

        private readonly SemaphoreSlim _logListSemaphore = new SemaphoreSlim(1, 1);

        public LogListService(
            ILogListApi logListApi,
            ILogStoreService logStoreService)
        {
            LogListApi = logListApi;
            LogStoreService = logStoreService;
        }

        public bool HasLogList => LogStoreService.ContainsKey(LogListRootKey);

        public async Task<bool> LoadLogListAsync(CancellationToken cancellationToken)
        {
            if (!HasLogList)
                await GetLogListRootAsync(cancellationToken).ConfigureAwait(false);

            return HasLogList;
        }

        public void ClearLogList()
        {
            LogStoreService.Remove(LogListRootKey);
            LogStoreService.Remove(LogDictionaryKey);
        }

        public async virtual Task<LogListRoot> GetLogListRootAsync(CancellationToken cancellationToken)
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
                        var logListTask = LogListApi.GetLogListAsync(cancellationToken);
                        var logListSignatureTask = LogListApi.GetLogListSignatureAsync(cancellationToken);
                        var logListPublicKeyTask = LogListApi.GetLogListPublicKeyAsync(cancellationToken);

                        await Task.WhenAll(logListTask, logListSignatureTask).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();

                        var logListBytes = logListTask.Result;
                        var logListSignatureBytes = logListSignatureTask.Result;
                        var logListPublicKey = logListPublicKeyTask.Result;

                        if (logListPublicKey != null || logListSignatureBytes != null)
                        {
                            var isValid = VerifyLogListSignature(logListBytes, logListSignatureBytes, logListPublicKey);

                            if (!isValid)
                                throw new InvalidDataException("Log list failed signature verification!");
                        }

                        logListRoot = Deserialise<LogListRoot>(logListBytes);

                        if (logListRoot?.Operators != null)
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
                catch (Exception)
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

        public async Task<IDictionary<string, Log>> GetLogDictionaryAsync(CancellationToken cancellationToken)
        {
            var logDictionary = default(IDictionary<string, Log>);

            if (!LogStoreService.TryGetValue(LogDictionaryKey, out logDictionary))
            {
                var logListRoot = await GetLogListRootAsync(cancellationToken).ConfigureAwait(false);
                if (logListRoot?.Operators != null)
                {
                    logDictionary = logListRoot.ToDictionary();
                    if (logDictionary.Any())
                        LogStoreService.SetValue(LogDictionaryKey, logDictionary);
                }
            }

            return logDictionary ?? new Dictionary<string, Log>(0);
        }

        protected static bool VerifyLogListSignature(byte[] data, byte[] signature, byte[] publicKey)
        {
            var signer = SignerUtilities.GetSigner(Constants.Sha256WithRsa);
            var pubKey = PublicKeyFactory.CreateKey(publicKey);
            //var pubKey = PublicKeyFactory.CreateKey(ReadPemPublicKey(Constants.GoogleLogListPublicKey));
            signer.Init(false, pubKey);
            signer.BlockUpdate(data, 0, data.Length);
            var isValid = signer.VerifySignature(signature);
            return isValid;
        }

        public static byte[] ReadPemPublicKey(string publicKey)
        {
            string encodedPublicKey = publicKey
                .Replace(Constants.BeginPublicKey, string.Empty)
                .Replace(Constants.EndPublicKey, string.Empty)
                .Trim();
            return Convert.FromBase64String(encodedPublicKey);
        }

        protected static T Deserialise<T>(byte[] data) where T : class
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var jsonSettings = new JsonSerializerSettings() { DateTimeZoneHandling = DateTimeZoneHandling.Utc };
                return JsonSerializer.Create(jsonSettings).Deserialize(reader, typeof(T)) as T;
            }
        }
    }
}

﻿using System;

namespace Cats.CertificateTransparency.Models
{
    public class SctVerificationResult
    {
        public static SctVerificationResult Valid(DateTime timestampUtc, string logId, string logDescription)
            => new SctVerificationResult(SctResult.Valid, timestampUtc, logId, logDescription, string.Empty);
        //public static SctVerificationResult FailedVerification(DateTime timestampUtc, string logId, string logDescription)
        //    => new SctVerificationResult(SctResult.FailedVerification, timestampUtc, logId, logDescription, string.Empty);
        public static SctVerificationResult FailedVerification(DateTime timestampUtc, string logId, string logDescription, string message)
            => new SctVerificationResult(SctResult.FailedVerification, timestampUtc, logId, logDescription, message);
        public static SctVerificationResult NoTrustedLogServerFound(DateTime timestampUtc)
           => new SctVerificationResult(SctResult.NoTrustedLogServerFound, timestampUtc, string.Empty, string.Empty, string.Empty);
        public static SctVerificationResult FutureTimestamp(DateTime timestampUtc, string logId, string logDescription)
           => new SctVerificationResult(SctResult.FutureTimestamp, timestampUtc, logId, logDescription, string.Empty);
        public static SctVerificationResult LogServerUntrusted(DateTime timestampUtc, string logId, string logDescription)
          => new SctVerificationResult(SctResult.UntrustedLogServer, timestampUtc, logId, logDescription, string.Empty);
        public static SctVerificationResult FailedWithException(DateTime timestampUtc, string logId, string logDescription, Exception exception)
          => new SctVerificationResult(SctResult.FailedWithException, timestampUtc, logId, logDescription, string.Empty, exception);

        public SctVerificationResult(SctResult result, DateTime timestampUtc, string logId, string logDescription, string message, Exception exception = null)
        {
            Result = result;
            TimestampUtc = timestampUtc;
            LogId = logId ?? string.Empty;
            LogDescription = logDescription ?? string.Empty;
            Message = message ?? string.Empty;
            Exception = exception;
        }

        public SctVerificationResult()
        {
            Result = SctResult.Unknown;
            TimestampUtc = DateTime.MinValue;
            LogId = string.Empty;
            Message = string.Empty;
            Exception = null;
        }

        public SctResult Result { get; private set; }
        public DateTime TimestampUtc { get; private set; }
        public string LogId { get; private set; }
        public string LogDescription { get; private set; }
        public string Message { get; private set; }
        public Exception Exception { get; private set; }

        public bool IsValid => Result == SctResult.Valid;

        public string Description => $"{Result} {Message} {Exception} by {LogDescription}";

        //public string Description => Result switch
        //{
        //    SctResult.Valid => "Valid SCT",
        //    SctResult.FailedVerification => "SCT signature failed verification",
        //    SctResult.NoTrustedLogServerFound => "No trusted log server found for SCT",
        //    SctResult.FutureTimestamp => "SCT timestamp is in the future",
        //    SctResult.UntrustedLogServer => "SCT timestamp is greater than the log server validity",
        //    SctResult.FailedWithException => $"SCT validation encounted an exception, '{Exception?.Message ?? string.Empty}'",
        //    _ => Result.ToString()
        //};

        public override string ToString() => Description;
    }
}

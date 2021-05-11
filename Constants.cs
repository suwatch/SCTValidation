using System;
using System.Runtime.CompilerServices;

#if DEBUG
[assembly: InternalsVisibleTo("Tests")]
[assembly: InternalsVisibleTo("Tests.Droid")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif

namespace Cats.CertificateTransparency
{
    public static class Constants
    {
        public const int BitsInByte = 8;

        public const int ExtensionsMaxLength = (1 << 16) - 1;
        public const int SignatureMaxLength = (1 << 16) - 1;
        public const int KeyIdLength = 32;
        public const int TimestampLength = 8;
        public const int VersionLength = 1;
        public const int LogEntryTypeLength = 2;
        public const int CertificateMaxLength = (1 << 24) - 1;

        public const string PreCertificateSigningOid = "1.3.6.1.4.1.11129.2.4.4";
        public const string PoisonOid = "1.3.6.1.4.1.11129.2.4.3";
        public const string SctCertificateOid = "1.3.6.1.4.1.11129.2.4.2";

        public const string X509AuthorityKeyIdentifier = "2.5.29.35";

        public const string BeginPublicKey = "-----BEGIN PUBLIC KEY-----";
        public const string EndPublicKey = "-----END PUBLIC KEY-----";

        public const string Sha256WithRsa = "SHA256withRSA";
        public const string Sha256WithEcdsa = "SHA256withECDSA";
    }
}

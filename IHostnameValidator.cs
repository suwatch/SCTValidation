using System;

namespace Cats.CertificateTransparency.Services
{
    public interface IHostnameValidator
    {
        bool ValidateHost(string host);
    }
}

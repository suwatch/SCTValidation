using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCTValidation
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var cert1 = new X509Certificate2(@"c:\temp\functionsuxsslcert_0ba5246de38f4240945f0fa7889258b8.cer");
                var cert2 = new X509Certificate2(@"c:\temp\functionsuxsslcert_d23fee7bb9044761af77f2baff482997.cer");

                foreach (var cert in new[] { cert1, cert2 })
                {
                    Console.WriteLine($"cert: {cert.Thumbprint}");
                    Console.WriteLine($"NotBefore: {cert.NotBefore}");
                    Console.WriteLine($"NotAfter: {cert.NotAfter}");
                    var chain = new X509Chain();
                    var result = chain.Build(cert);
                    Console.WriteLine($"chain.Build: {result}");

                    var certificateChain = chain.ChainElements.OfType<X509ChainElement>().Select(i => i.Certificate).ToList();
                    var certificateVerifier = Cats.CertificateTransparency.Instance.CertificateTransparencyVerifier;
                    var ctValueTask = certificateVerifier.IsValidAsync("functions.ext.microsoftazure.de", certificateChain, CancellationToken.None);

                    //var ctResult = ctValueTask.IsCompleted
                    //    ? ctValueTask.Result
                    //    : ctValueTask.AsTask().Result;

                    var ctResult = ctValueTask.Result;
                    Console.WriteLine($"ctResult: {ctResult}");
                }

                //var client = new HttpClient(new HttpClientHandler()
                //{
                //    ServerCertificateCustomValidationCallback = (request, certificate, chain, sslPolicyErrors) =>
                //    {
                //        var certificateChain = chain.ChainElements.OfType<X509ChainElement>().Select(i => i.Certificate).ToList();
                //        var certificateVerifier = Cats.CertificateTransparency.Instance.CertificateTransparencyVerifier;
                //        var ctValueTask = certificateVerifier.IsValidAsync(request.RequestUri.Host, certificateChain, CancellationToken.None);

                //        //var ctResult = ctValueTask.IsCompleted
                //        //    ? ctValueTask.Result
                //        //    : ctValueTask.AsTask().Result;

                //        var ctResult = ctValueTask.Result;

                //        return ctResult.IsValid;
                //    }
                //});
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}

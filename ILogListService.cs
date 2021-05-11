using Cats.CertificateTransparency.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cats.CertificateTransparency.Services
{
    public interface ILogListService
    {
        bool HasLogList { get; }
        
        Task<bool> LoadLogListAsync(CancellationToken cancellationToken);
        void ClearLogList();

        Task<LogListRoot> GetLogListRootAsync(CancellationToken cancellationToken);
        Task<IDictionary<string, Log>> GetLogDictionaryAsync(CancellationToken cancellationToken);
    }
}

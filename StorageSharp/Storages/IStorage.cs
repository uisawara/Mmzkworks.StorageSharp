using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StorageSharp.Storages
{
    public interface IStorage
    {
        Task<string[]> ListAll(CancellationToken cancellationToken = default);
        Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken = default);
        Task WriteAsync(string key, byte[] data, CancellationToken cancellationToken = default);
        Task<Stream> ReadToStreamAsync(string key, CancellationToken cancellationToken = default);
        Task WriteAsync(string key, Stream stream, CancellationToken cancellationToken = default);
    }
}
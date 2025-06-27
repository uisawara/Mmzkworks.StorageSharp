using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StorageSharp.Storages
{
    public class MemoryStorage : IStorage
    {
        private readonly ConcurrentDictionary<string, byte[]> _storage = new ConcurrentDictionary<string, byte[]>();

        public int Count => _storage.Count;

        public Task<string[]> ListAll(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_storage.Keys.ToArray());
        }

        public Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            if (!_storage.TryGetValue(key, out var data))
                throw new KeyNotFoundException($"Key not found: {key}");

            return Task.FromResult(data);
        }

        public Task WriteAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            if (data == null || data.Length == 0)
                _storage.TryRemove(key, out _);
            else
                _storage.AddOrUpdate(key, data, (k, v) => data);

            return Task.CompletedTask;
        }

        public Task<StreamReader> ReadToStreamAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            if (!_storage.TryGetValue(key, out var data))
                throw new KeyNotFoundException($"Key not found: {key}");

            var stream = new MemoryStream(data);
            var reader = new StreamReader(stream);

            return Task.FromResult(reader);
        }

        public async Task WriteAsync(string key, StreamReader stream, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            cancellationToken.ThrowIfCancellationRequested();

            using var memoryStream = new MemoryStream();
            await stream.BaseStream.CopyToAsync(memoryStream, cancellationToken);

            var data = memoryStream.ToArray();
            _storage.AddOrUpdate(key, data, (k, v) => data);
        }

        public void Clear()
        {
            _storage.Clear();
        }
    }
}
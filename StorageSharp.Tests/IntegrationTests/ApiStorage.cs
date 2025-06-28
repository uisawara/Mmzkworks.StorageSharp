using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StorageSharp.Storages;

namespace StorageSharp.Tests.IntegrationTests
{
    /// <summary>
    /// E2Eテスト用のHTTP APIストレージ実装
    /// このクラスはテストプロジェクト内でのみ使用され、ライブラリコードには含まれません
    /// </summary>
    public class ApiStorage : IStorage, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed = false;

        public ApiStorage(string baseUrl = "http://localhost:8080")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<string[]> ListAll(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/list", cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ApiListResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Keys ?? Array.Empty<string>();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"API request failed: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/read/{Uri.EscapeDataString(key)}", cancellationToken);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException($"Key '{key}' not found");
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ApiReadResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Data == null)
                {
                    throw new InvalidOperationException("Invalid response format");
                }

                return Convert.FromBase64String(result.Data);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"API request failed: {ex.Message}", ex);
            }
        }

        public async Task WriteAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestData = new ApiWriteRequest
                {
                    Data = Convert.ToBase64String(data)
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/write/{Uri.EscapeDataString(key)}", content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"API request failed: {ex.Message}", ex);
            }
        }

        public async Task<StreamReader> ReadToStreamAsync(string key, CancellationToken cancellationToken = default)
        {
            var data = await ReadAsync(key, cancellationToken);
            var stream = new MemoryStream(data);
            return new StreamReader(stream);
        }

        public async Task WriteAsync(string key, StreamReader stream, CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();
            await stream.BaseStream.CopyToAsync(memoryStream, cancellationToken);
            var data = memoryStream.ToArray();
            await WriteAsync(key, data, cancellationToken);
        }

        /// <summary>
        /// サーバーのヘルスチェックを実行
        /// </summary>
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/health", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 指定されたキーを削除
        /// </summary>
        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/delete/{Uri.EscapeDataString(key)}", cancellationToken);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException($"Key '{key}' not found");
                }

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"API request failed: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        // APIレスポンス用の内部クラス
        private class ApiListResponse
        {
            public string[] Keys { get; set; } = Array.Empty<string>();
        }

        private class ApiReadResponse
        {
            public string Key { get; set; } = string.Empty;
            public string Data { get; set; } = string.Empty;
            public int Size { get; set; }
        }

        private class ApiWriteRequest
        {
            public string Data { get; set; } = string.Empty;
        }
    }
} 
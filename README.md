# StorageSharp

StorageSharp is a flexible storage system for handling single binary files and folder file collections. By combining them, you can flexibly handle file systems, caching, and folder file collections (called Packs).

[![CI/CD Pipeline](https://github.com/uisawara/storageSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/uisawara/storageSharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Mmzkworks.StorageSharp.svg)](https://www.nuget.org/packages/Mmzkworks.StorageSharp/)

## Read first

- [overview](./Documents/overview.md)

## Features

### Storage Features (IStorage)

- **FileStorage**: File system-based storage
- **MemoryStorage**: Memory-based storage
- **CachedStorage**: Storage with caching functionality
- **EncryptedStorage**: AES-256 encrypted storage that wraps any IStorage implementation
- **StorageRouter**: Routes operations to different storages based on key patterns

### Archive Features (IPacks)

- **ZippedPacks**: Archive implementation that manages packages in ZIP format

## Basic Usage

### Using Storage

```csharp
// File storage
var fileStorage = new FileStorage("StorageDirectory");

// Writing data
await fileStorage.WriteAsync("key.txt", data);

// Reading data
var data = await fileStorage.ReadAsync("key.txt");
```

### Using Cached Storage

```csharp
var storage = new CachedStorage(
    cache: new MemoryStorage(), // Cache storage
    origin: new FileStorage("OriginStorage") // Origin storage
);
```

### Using Encrypted Storage

```csharp
// Encrypt data with a password
var baseStorage = new FileStorage("EncryptedStorage");
var encryptedStorage = new EncryptedStorage(baseStorage, "your-secure-password");

// Write encrypted data
await encryptedStorage.WriteAsync("secret.txt", data);

// Read and decrypt data
var decryptedData = await encryptedStorage.ReadAsync("secret.txt");

// Advanced: Use custom encryption key and IV
var key = new byte[32]; // 32-byte key for AES-256
var iv = new byte[16];  // 16-byte IV
// Initialize key and iv with secure random values...
var customEncryptedStorage = new EncryptedStorage(baseStorage, key, iv);
```

### Using ZIP Packages

```csharp
var packages = new ZippedPacks(
    new ZippedPacks.Settings("Tmp/Packs/"),
    storage
);

// Add directory to archive
var archiveScheme = await packages.Add(directoryPath);

// Load archive
var loadedPath = await packages.Load(archiveScheme);

// Use files
// ...

// Unload archive
await packages.Unload(archiveScheme);

// Delete archive
await packages.Delete(archiveScheme);

// List all archives
var list = await packages.ListAll();
```

## Setup

### Using as a Library

```bash
# Add reference to project
dotnet add reference path/to/StorageSharp.csproj
```

### Using as NuGet Package (in the future)

```bash
dotnet add package StorageSharp
```

### Development Environment Setup

```bash
# Clone repository
git clone <repository-url>
cd storageSharp

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test
```

### Running Sample Programs

```bash
# Run sample project
cd StorageSharp.Samples
dotnet run
```

## Project Structure

```
storageSharp/
├── StorageSharp/                    # Main library
│   ├── Storages/
│   │   ├── IStorage.cs              # Storage interface
│   │   ├── FileStorage.cs           # File storage implementation
│   │   ├── MemoryStorage.cs         # Memory storage implementation
│   │   ├── CachedStorage.cs         # Cached storage implementation
│   │   ├── EncryptedStorage.cs      # AES-256 encrypted storage implementation
│   │   └── StorageRouter.cs         # Storage routing implementation
│   ├── Packs/
│   │   ├── IPacks.cs                # Archive interface
│   │   └── ZippedPacks.cs           # ZIP package implementation
│   └── StorageSharp.csproj          # Library project
├── StorageSharp.Samples/            # Sample project
│   ├── Program.cs                   # Sample program
│   ├── StorageSharp.Samples.csproj  # Sample project
│   └── README.md                    # Sample README
├── StorageSharp.Tests/              # Test project
│   ├── UnitTests/                   # Unit tests
│   └── IntegrationTests/            # Integration tests
├── storageSharp.sln                 # Solution file
└── README.md                        # This file
```

## Usage Examples

### Basic Storage Operations

```csharp
// Using file storage
var fileStorage = new FileStorage("ExampleStorage");
var testData = System.Text.Encoding.UTF8.GetBytes("Hello, StorageSharp!");
await fileStorage.WriteAsync("test.txt", testData);

// Using memory storage
var memoryStorage = new MemoryStorage();
await memoryStorage.WriteAsync("memory-test.txt", testData);
```

### Using Cached Storage

```csharp
var cache = new MemoryStorage();
var origin = new FileStorage("OriginStorage");
var cachedStorage = new CachedStorage(cache, origin);

// Writing data
var data = System.Text.Encoding.UTF8.GetBytes("Cached data example");
await cachedStorage.WriteAsync("cached-file.txt", data);

// Reading data (cache hit/miss is automatically managed)
var readData = await cachedStorage.ReadAsync("cached-file.txt");
```

### Using Encrypted Storage

```csharp
// Basic encrypted storage with password
var baseStorage = new FileStorage("SecureStorage");
var encryptedStorage = new EncryptedStorage(baseStorage, "MySecurePassword123!");

// Writing encrypted data
var sensitiveData = System.Text.Encoding.UTF8.GetBytes("This data will be encrypted");
await encryptedStorage.WriteAsync("confidential.txt", sensitiveData);

// Reading encrypted data (automatically decrypted)
var decryptedData = await encryptedStorage.ReadAsync("confidential.txt");

// Using encrypted storage with different backends
var memoryBackend = new MemoryStorage();
var encryptedMemory = new EncryptedStorage(memoryBackend, "MemoryPassword");

var cachedBackend = new CachedStorage(new MemoryStorage(), new FileStorage("Cache"));
var encryptedCached = new EncryptedStorage(cachedBackend, "CachedPassword");

// Custom encryption settings
var customKey = new byte[32]; // 32-byte key for AES-256
var customIV = new byte[16];  // 16-byte IV for AES
// Fill with cryptographically secure random values...
var customEncrypted = new EncryptedStorage(baseStorage, customKey, customIV);
```

### Using ZIP Packages

```csharp
var storage = new FileStorage("ZippedPacks");
var packages = new ZippedPacks(
    new ZippedPacks.Settings("Tmp/Packs/"),
    storage
);

// Add directory to archive
var archiveScheme = await packages.Add("MyDirectory");

// Load and use archive
var loadedPath = await packages.Load(archiveScheme);
// Use files...
await packages.Unload(archiveScheme);

// Delete archive
await packages.Delete(archiveScheme);
```

### Using Storage Router

```csharp
var storageRouter = new StorageRouter(new[]
{
    // Route HTTP/HTTPS keys to specific storage
    new StorageRouter.Branch(
        key => key.StartsWith("http://") || key.StartsWith("https://"),
        new FileStorage("HttpStorage")),
    
    // Route file:// keys with prefix removal
    new StorageRouter.Branch(
        key => key.StartsWith("file://"),
        key => key.Substring("file://".Length), // Key formatter
        new FileStorage("LocalStorage"))
},
new FileStorage("DefaultStorage")); // Default storage

// Write to appropriate storage based on key
await storageRouter.WriteAsync("http://example.com/data.txt", data);
await storageRouter.WriteAsync("file://local/data.txt", data); // Routed to LocalStorage with key "local/data.txt"
await storageRouter.WriteAsync("regular-file.txt", data); // Routed to DefaultStorage
```

## Notes

- Temporary files are automatically managed, but please consider appropriate cleanup when handling large amounts of data
- Use the caching functionality with attention to memory usage
- The ZIP package functionality uses the SharpZipLib library

### Security Considerations for EncryptedStorage

- **Password Management**: Encryption passwords/keys must be properly managed and secured
- **Key Storage**: Never hardcode encryption keys in source code
- **Algorithm**: Uses AES-256 encryption with CBC mode and PKCS7 padding
- **IV Management**: Initialization vectors (IV) are fixed per instance - ensure proper key rotation for production use
- **Performance**: Encryption/decryption adds computational overhead - consider this for high-throughput scenarios

## About AI Generation

- This document has been machine translated.
- This repo contains generated code by ChatGPT and Cursor.

## License

This project is published under the MIT License. 

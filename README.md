# StorageSharp

StorageSharp is a flexible storage system for handling single binary files and folder file collections. By combining them, you can flexibly handle file systems, caching, and folder file collections (called Packs).

[![CI/CD Pipeline](https://github.com/uisawara/storageSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/uisawara/storageSharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Mmzkworks.StorageSharp.svg)](https://www.nuget.org/packages/Mmzkworks.StorageSharp/)

## Features

### Storage Features (IStorage)

- **FileStorage**: File system-based storage
- **MemoryStorage**: Memory-based storage
- **CachedStorage**: Storage with caching functionality

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
│   │   └── CachedStorage.cs         # Cached storage implementation
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

### Additional documents

- [overview](./Documents/overview.md)

## Notes

- Temporary files are automatically managed, but please consider appropriate cleanup when handling large amounts of data
- Use the caching functionality with attention to memory usage
- The ZIP package functionality uses the SharpZipLib library

## About AI Generation

- This document has been machine translated.
- This repo contains generated code by ChatGPT and Cursor.

## License

This project is published under the MIT License. 

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StardustCraft.Bundles.BundleSystem
{
    public class BundleManager
    {
        private static BundleManager _instance;
        public static BundleManager Instance => _instance ??= new BundleManager();

        private Dictionary<string, Bundle> loadedBundles = new Dictionary<string, Bundle>();
        private Dictionary<string, CatalogFile.BundleFileInfo> fileCatalog = new Dictionary<string, CatalogFile.BundleFileInfo>();

        private readonly object bundleLock = new object();
        private string currentBundlePath = "Assets/Bundles";

        public void Initialize(string basePath = null)
        {
            if (!string.IsNullOrEmpty(basePath))
                currentBundlePath = basePath;

            LoadCatalog();
        }

        public byte[] GetFileBytes(string path)
        {
            lock (bundleLock)
            {
                string key = path.ToLower();
                if (fileCatalog.TryGetValue(key, out var fileInfo))
                {
                    if (!loadedBundles.TryGetValue(fileInfo.bundleName, out var bundle))
                    {
                        bundle = LoadBundle(fileInfo.bundleName);
                        loadedBundles[fileInfo.bundleName] = bundle;
                    }

                    return bundle.GetFileData(fileInfo);
                }

                // Fallback su filesystem
                if (File.Exists(path))
                    return File.ReadAllBytes(path);

                throw new FileNotFoundException($"File non trovato in bundle o filesystem: {path}");
            }
        }

        public string GetFileText(string path)
        {
            var bytes = GetFileBytes(path);

            // Controlla BOM UTF-8
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3); // salta i primi 3 byte
            }

            return Encoding.UTF8.GetString(bytes);
        }

        public Stream GetFileStream(string path)
        {
            var bytes = GetFileBytes(path);
            return new MemoryStream(bytes);
        }

        public T LoadAsset<T>(string path) where T : class
        {
            var bytes = GetFileBytes(path);

            if (typeof(T) == typeof(byte[])) return bytes as T;
            if (typeof(T) == typeof(string)) return Encoding.UTF8.GetString(bytes) as T;
            if (typeof(T) == typeof(Stream)) return new MemoryStream(bytes) as T;

            throw new InvalidOperationException($"Tipo asset non supportato: {typeof(T)}");
        }

        private Bundle LoadBundle(string bundleName)
        {
            string bundlePath = Path.Combine(currentBundlePath, bundleName);

            if (!File.Exists(bundlePath))
                throw new FileNotFoundException($"Bundle non trovato: {bundlePath}");

            return new Bundle(bundlePath);
        }

        private void LoadCatalog()
        {
            string catalogPath = Path.Combine(currentBundlePath, "catalog.dat");
            if (!File.Exists(catalogPath))
            {
                fileCatalog = new Dictionary<string, CatalogFile.BundleFileInfo>();
                return;
            }

            using var stream = File.OpenRead(catalogPath);
            using var reader = new BinaryReader(stream);
            var catalog = CatalogFile.Deserialize(reader);
            fileCatalog = catalog.files;
        }

        public void UnloadBundle(string bundleName)
        {
            lock (bundleLock)
            {
                if (loadedBundles.TryGetValue(bundleName, out var bundle))
                {
                    bundle.Dispose();
                    loadedBundles.Remove(bundleName);
                }
            }
        }

        public void UnloadAll()
        {
            lock (bundleLock)
            {
                foreach (var bundle in loadedBundles.Values)
                    bundle.Dispose();
                loadedBundles.Clear();
                fileCatalog.Clear();
            }
        }

        public IEnumerable<string> GetAllFiles() => fileCatalog.Keys;
        public bool FileExists(string path) => fileCatalog.ContainsKey(path.ToLower());
        public long GetFileSize(string path) => fileCatalog.TryGetValue(path.ToLower(), out var info) ? info.size : -1;
        public string GetFileHash(string path) => fileCatalog.TryGetValue(path.ToLower(), out var info) ? info.hash : null;

        public void PreloadBundle(string bundleName)
        {
            if (!loadedBundles.ContainsKey(bundleName))
            {
                var bundle = LoadBundle(bundleName);
                loadedBundles[bundleName] = bundle;
            }
        }

        #region CatalogFile & Bundle

        public class CatalogFile
        {
            public Dictionary<string, BundleFileInfo> files = new Dictionary<string, BundleFileInfo>();
            public long totalSize;
            public int bundleCount;
            public string version;

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(version ?? "1.0");
                writer.Write(totalSize);
                writer.Write(bundleCount);
                writer.Write(files.Count);

                foreach (var kvp in files)
                {
                    writer.Write(kvp.Key);
                    kvp.Value.Serialize(writer);
                }
            }

            public static CatalogFile Deserialize(BinaryReader reader)
            {
                var catalog = new CatalogFile
                {
                    version = reader.ReadString(),
                    totalSize = reader.ReadInt64(),
                    bundleCount = reader.ReadInt32()
                };

                int fileCount = reader.ReadInt32();
                for (int i = 0; i < fileCount; i++)
                {
                    string path = reader.ReadString();
                    var fileInfo = BundleFileInfo.Deserialize(reader);
                    catalog.files[path] = fileInfo;
                }

                return catalog;
            }

            public class BundleFileInfo
            {
                public string path;
                public string bundleName;
                public long size;
                public long startOffset;
                public long compressedSize;
                public bool isCompressed;
                public string hash;
                public DateTime lastModified;

                public void Serialize(BinaryWriter writer)
                {
                    writer.Write(bundleName);
                    writer.Write(path);
                    writer.Write(size);
                    writer.Write(startOffset);
                    writer.Write(compressedSize);
                    writer.Write(isCompressed);
                    writer.Write(hash ?? "");
                    writer.Write(lastModified.ToBinary());
                }

                public static BundleFileInfo Deserialize(BinaryReader reader)
                {
                    return new BundleFileInfo
                    {
                        bundleName = reader.ReadString(),
                        path = reader.ReadString(),
                        size = reader.ReadInt64(),
                        startOffset = reader.ReadInt64(),
                        compressedSize = reader.ReadInt64(),
                        isCompressed = reader.ReadBoolean(),
                        hash = reader.ReadString(),
                        lastModified = DateTime.FromBinary(reader.ReadInt64())
                    };
                }
            }
        }

        public class Bundle : IDisposable
        {
            private readonly string filePath;
            private byte[] memoryData;
            private FileStream fileStream;
            private bool isMemoryLoaded;

            public Bundle(string filePath)
            {
                this.filePath = filePath;
                var info = new FileInfo(filePath);

                // Bundle < 50MB → carica in memoria
                if (info.Length < 50 * 1024 * 1024)
                {
                    memoryData = File.ReadAllBytes(filePath);
                    isMemoryLoaded = true;
                }
                else
                {
                    fileStream = File.OpenRead(filePath);
                    isMemoryLoaded = false;
                }
            }

            public byte[] GetFileData(CatalogFile.BundleFileInfo fileInfo)
            {
                if (isMemoryLoaded && memoryData != null)
                {
                    using var stream = new MemoryStream(memoryData);
                    return ReadFromStream(stream, fileInfo);
                }
                else if (fileStream != null)
                {
                    lock (fileStream)
                    {
                        return ReadFromStream(fileStream, fileInfo);
                    }
                }

                throw new InvalidOperationException("Bundle non inizializzato correttamente");
            }
            public static void CreateBundlesFromDirectories(Dictionary<string, string> directoryToBundleMap,
    string outputPath, CompressionLevel compression = CompressionLevel.Optimal)
            {
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                var catalog = new BundleManager.CatalogFile
                {
                    version = "1.0",
                    files = new Dictionary<string, BundleManager.CatalogFile.BundleFileInfo>(),
                    bundleCount = directoryToBundleMap.Count
                };

                foreach (var mapping in directoryToBundleMap)
                {
                    string sourceDirectory = mapping.Key;
                    string bundleName = mapping.Value;

                    if (!Directory.Exists(sourceDirectory))
                    {
                        Console.WriteLine($"Warning: Directory not found: {sourceDirectory}");
                        continue;
                    }

                    Console.WriteLine($"Processing: {sourceDirectory} -> {bundleName}");

                    CreateBundleFromDirectory(sourceDirectory, outputPath, bundleName, catalog, compression);
                }

                // Salva catalogo
                string catalogPath = Path.Combine(outputPath, "catalog.dat");
                using var catalogStream = File.Create(catalogPath);
                using var writer = new BinaryWriter(catalogStream);
                catalog.Serialize(writer);

                Console.WriteLine($"Created {catalog.bundleCount} bundles with {catalog.files.Count} total files");
            }
            private static bool ShouldCompressFile(string filePath)
            {
                string extension = Path.GetExtension(filePath).ToLower();

                // File già compressi o binari che non hanno senso comprimere
                string[] alreadyCompressed = { ".jpg", ".jpeg", ".png", ".gif", ".mp3", ".ogg", ".wav", ".mp4", ".avi", ".zip", ".rar", ".7z" };
                if (alreadyCompressed.Contains(extension))
                    return false;

                // File di testo e dati che beneficiano della compressione
                string[] compressible = { ".txt", ".json", ".xml", ".csv", ".html", ".css", ".js", ".lua", ".cs", ".asset", ".meta", ".prefab" };
                if (compressible.Contains(extension))
                    return true;

                // Default: non comprimere
                return false;
            }

            private static string GetRelativePath(string fullPath, string basePath)
            {
                fullPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar);
                basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar);

                if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Full path '{fullPath}' is not under base path '{basePath}'");

                string relativePath = fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
                return relativePath.Replace('\\', '/'); // Sempre con slash per consistenza
            }

            private static string CalculateSHA256(byte[] data)
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            private static void CreateBundleFromDirectory(string sourceDirectory, string outputPath,
                string bundleName, BundleManager.CatalogFile catalog, CompressionLevel compression)
            {
                string bundlePath = Path.Combine(outputPath, bundleName);

                using var bundleStream = File.Create(bundlePath);
                using var writer = new BinaryWriter(bundleStream);

                long currentOffset = 0;
                var allFiles = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    try
                    {
                        var fileBytes = File.ReadAllBytes(file);
                        string hash = CalculateSHA256(fileBytes);

                        string relativePath = GetRelativePath(file, sourceDirectory).ToLower();

                        bool shouldCompress = ShouldCompressFile(file);
                        byte[] dataToWrite;
                        long compressedSize;

                        if (shouldCompress)
                        {
                            using var compressedStream = new MemoryStream();
                            using (var gzipStream = new GZipStream(compressedStream, compression, true))
                            {
                                gzipStream.Write(fileBytes, 0, fileBytes.Length);
                            }
                            dataToWrite = compressedStream.ToArray();
                            compressedSize = dataToWrite.Length;
                        }
                        else
                        {
                            dataToWrite = fileBytes;
                            compressedSize = fileBytes.Length;
                        }

                        writer.Write(dataToWrite);

                        catalog.files[relativePath] = new BundleManager.CatalogFile.BundleFileInfo
                        {
                            path = relativePath,
                            bundleName = bundleName,
                            size = fileBytes.Length,
                            startOffset = currentOffset,
                            compressedSize = compressedSize,
                            isCompressed = shouldCompress,
                            hash = hash,
                            lastModified = File.GetLastWriteTimeUtc(file)
                        };

                        currentOffset += dataToWrite.Length;
                        catalog.totalSize += fileBytes.Length;

                        Console.WriteLine($"  Added: {relativePath} ({fileBytes.Length / 1024} KB)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Error processing {file}: {ex.Message}");
                    }
                }

                Console.WriteLine($"  Bundle size: {currentOffset / 1024} KB");
            }

            public static void CreateBundlesAuto(string rootDirectory, string outputPath,
                string bundlePrefix = "", CompressionLevel compression = CompressionLevel.Optimal)
            {
                if (!Directory.Exists(rootDirectory))
                    throw new DirectoryNotFoundException($"Root directory not found: {rootDirectory}");

                var directories = Directory.GetDirectories(rootDirectory, "*", SearchOption.TopDirectoryOnly);

                var directoryMap = new Dictionary<string, string>();
                foreach (var dir in directories)
                {
                    string dirName = Path.GetFileName(dir);
                    string bundleName = $"{bundlePrefix}{dirName.ToLower()}.bundle";
                    directoryMap[dir] = bundleName;
                }

                CreateBundlesFromDirectories(directoryMap, outputPath, compression);
            }
            private byte[] ReadFromStream(Stream stream, CatalogFile.BundleFileInfo fileInfo)
            {
                stream.Seek(fileInfo.startOffset, SeekOrigin.Begin);

                if (fileInfo.isCompressed)
                {
                    // Leggi solo i byte compressi del file
                    byte[] compressedData = new byte[fileInfo.compressedSize];
                    int totalRead = 0;
                    while (totalRead < compressedData.Length)
                    {
                        int read = stream.Read(compressedData, totalRead, compressedData.Length - totalRead);
                        if (read == 0) throw new EndOfStreamException("Lettura file compressa incompleta");
                        totalRead += read;
                    }

                    using var compressedStream = new MemoryStream(compressedData);
                    using var gzip = new GZipStream(compressedStream, CompressionMode.Decompress);
                    using var result = new MemoryStream();
                    gzip.CopyTo(result);
                    return result.ToArray();
                }
                else
                {
                    byte[] buffer = new byte[fileInfo.size];
                    int totalRead = 0;
                    while (totalRead < buffer.Length)
                    {
                        int read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                        if (read == 0) throw new EndOfStreamException("Lettura file incompleta");
                        totalRead += read;
                    }
                    return buffer;
                }
            }

            public void Dispose()
            {
                memoryData = null;
                fileStream?.Dispose();
                fileStream = null;
            }
        }

        #endregion
    }
}

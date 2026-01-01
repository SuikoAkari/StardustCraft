using StardustCraft.Bundles.BundleSystem;
using System;
using System.Collections.Generic;
using System.IO;
using static StardustCraft.Bundles.BundleSystem.BundleManager;

namespace StardustCraft.BundleTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Automatic Bundle Creator ===");
            Console.WriteLine("1. Create bundles from directories");
            Console.WriteLine("2. Auto-create bundles from root directory");
            Console.WriteLine("3. Custom configuration");
            Console.Write("\nSelect option: ");

            var option = Console.ReadLine();

            string outputPath = "GameData/Bundles";
            Directory.CreateDirectory(outputPath);

            switch (option)
            {
                case "1":
                    CreateFromDirectories(outputPath);
                    break;

                case "2":
                    CreateAutoFromRoot(outputPath);
                    break;

                case "3":
                    CustomConfiguration(outputPath);
                    break;

                default:
                    Console.WriteLine("Invalid option");
                    break;
            }
        }

        static void CreateFromDirectories(string outputPath)
        {
            Console.WriteLine("\n=== Create Bundles from Directories ===");

            var directoryMap = new Dictionary<string, string>();

            while (true)
            {
                Console.Write("\nEnter source directory (or 'done' to finish): ");
                string sourceDir = Console.ReadLine();

                if (sourceDir.ToLower() == "done")
                    break;

                if (!Directory.Exists(sourceDir))
                {
                    Console.WriteLine("Directory not found!");
                    continue;
                }

                Console.Write("Enter bundle name (e.g., 'textures.bundle'): ");
                string bundleName = Console.ReadLine();

                if (!bundleName.EndsWith(".bundle"))
                    bundleName += ".bundle";

                directoryMap[sourceDir] = bundleName;
                Console.WriteLine($"Added: {sourceDir} -> {bundleName}");
            }

            if (directoryMap.Count > 0)
            {
                Console.WriteLine($"\nCreating {directoryMap.Count} bundles...");
                Bundle.CreateBundlesFromDirectories(directoryMap, outputPath);
                Console.WriteLine("✅ Done!");
            }
        }

        static void CreateAutoFromRoot(string outputPath)
        {
            Console.WriteLine("\n=== Auto-create from Root Directory ===");
            Console.Write("Enter root directory: ");
            string rootDir = Console.ReadLine();

            if (!Directory.Exists(rootDir))
            {
                Console.WriteLine("Directory not found!");
                return;
            }

            Console.Write("Enter bundle prefix (optional): ");
            string prefix = Console.ReadLine();

            Console.WriteLine("\nScanning directories...");
            var subdirs = Directory.GetDirectories(rootDir);
            Console.WriteLine($"Found {subdirs.Length} directories:");

            foreach (var dir in subdirs)
            {
                string dirName = Path.GetFileName(dir);
                Console.WriteLine($"  - {dirName} -> {prefix}{dirName.ToLower()}.bundle");
            }

            Console.Write("\nProceed? (y/n): ");
            if (Console.ReadLine().ToLower() != "y")
                return;

            Bundle.CreateBundlesAuto(rootDir, outputPath, prefix);
            Console.WriteLine("✅ Done!");
        }

        

        static void CustomConfiguration(string outputPath)
        {
            Console.WriteLine("\n=== Custom Configuration ===");

            // Leggi configurazione da file JSON
            string configFile = "bundle_config.json";

            if (File.Exists(configFile))
            {
                var config = LoadConfig(configFile);
                CreateFromConfig(config, outputPath);
            }
            else
            {
                Console.WriteLine("Config file not found. Creating example...");
                CreateExampleConfig(configFile);
            }
        }

        static BundleConfig LoadConfig(string path)
        {
            string json = File.ReadAllText(path);
            return System.Text.Json.JsonSerializer.Deserialize<BundleConfig>(json);
        }

        static void CreateFromConfig(BundleConfig config, string outputPath)
        {
            var directoryMap = new Dictionary<string, string>();

            foreach (var mapping in config.DirectoryMappings)
            {
                directoryMap[mapping.SourceDirectory] = mapping.BundleName;
            }

            Bundle.CreateBundlesFromDirectories(directoryMap, outputPath, config.CompressionLevel);
        }

        static void CreateExampleConfig(string path)
        {
            var exampleConfig = new BundleConfig
            {
                CompressionLevel = System.IO.Compression.CompressionLevel.Optimal,
                DirectoryMappings = new List<DirectoryMapping>
                {
                    new DirectoryMapping
                    {
                        SourceDirectory = "Assets/Textures",
                        BundleName = "textures.bundle"
                    },
                    new DirectoryMapping
                    {
                        SourceDirectory = "Assets/Models",
                        BundleName = "models.bundle"
                    },
                    new DirectoryMapping
                    {
                        SourceDirectory = "Assets/Audio",
                        BundleName = "audio.bundle"
                    },
                    new DirectoryMapping
                    {
                        SourceDirectory = "Assets/Shaders",
                        BundleName = "shaders.bundle"
                    }
                }
            };

            string json = System.Text.Json.JsonSerializer.Serialize(exampleConfig,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, json);
            Console.WriteLine($"Created example config at: {path}");
        }
    }

    // Classi per configurazione
    public class BundleConfig
    {
        public System.IO.Compression.CompressionLevel CompressionLevel { get; set; }
        public List<DirectoryMapping> DirectoryMappings { get; set; }
    }

    public class DirectoryMapping
    {
        public string SourceDirectory { get; set; }
        public string BundleName { get; set; }
    }
}
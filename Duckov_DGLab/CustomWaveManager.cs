using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DGLabCSharp;
using DGLabCSharp.Enums;
using UnityEngine;

namespace Duckov_DGLab
{
    public class CustomWaveManager
    {
        public const string CustomWaveDirectory = "DGLabCustomWaves";

        public static readonly List<CustomWave> CustomWaves = [];
        private static FileSystemWatcher? _fileWatcher;

        public static readonly string CustomWavePath = $"{Application.dataPath}/../{CustomWaveDirectory}";

        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized) return;

            if (!Directory.Exists(CustomWavePath))
                if (!CreateDefaultConfigs())
                {
                    ModLogger.LogError("Failed to create default custom wave configurations.");
                    return;
                }

            if (!ReloadWaves())
            {
                ModLogger.LogError("Failed to load custom wave configurations.");
                return;
            }

            RegisterFileWatcher();
            IsInitialized = true;
        }

        public static void Uninitialize()
        {
            if (!IsInitialized) return;

            UnregisterFileWatcher();
            CustomWaves.Clear();
            IsInitialized = false;
        }

        public static bool ReloadWaves()
        {
            CustomWaves.Clear();

            try
            {
                var jsonFiles = Directory.GetFiles(CustomWavePath, "*.json");

                foreach (var file in jsonFiles)
                {
                    var jsonData = File.ReadAllText(file);
                    var waveData = JsonSerializerFactory.Instance.Deserialize<string[]>(jsonData);

                    var customWave = new CustomWave(Path.GetFileNameWithoutExtension(file), waveData);
                    if (!customWave.Validate())
                    {
                        ModLogger.LogError($"Invalid custom wave configuration in file: {file}");
                        continue;
                    }

                    CustomWaves.Add(customWave);
                }
            }
            catch (IOException e)
            {
                ModLogger.LogError($"Failed to read custom wave configurations: {e.Message}");
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Unexpected error loading custom wave configurations: {ex.Message}");
                return false;
            }

            return true;
        }

        public static string[] GetAllCustomWaveNames()
        {
            return CustomWaves.Select(cw => cw.Name).ToArray();
        }

        public static string[]? GetWavesByName(string name)
        {
            var customWave = CustomWaves.FirstOrDefault(cw =>
                cw.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (customWave.Waves != null)
                return customWave.Waves;

            ModLogger.LogWarning($"Custom wave with name '{name}' not found.");
            return null;
        }

        private static void RegisterFileWatcher()
        {
            if (_fileWatcher != null) return;

            _fileWatcher = new(CustomWavePath, "*.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            };
            _fileWatcher.Changed += (_, _) => ReloadWaves();
            _fileWatcher.Created += (_, _) => ReloadWaves();
            _fileWatcher.Deleted += (_, _) => ReloadWaves();
            _fileWatcher.Renamed += (_, _) => ReloadWaves();
            _fileWatcher.EnableRaisingEvents = true;
        }

        private static void UnregisterFileWatcher()
        {
            if (_fileWatcher == null) return;

            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
            _fileWatcher = null;
        }

        private static bool CreateDefaultConfigs()
        {
            if (!Directory.Exists(CustomWavePath))
                try
                {
                    Directory.CreateDirectory(CustomWavePath);
                }
                catch (IOException e)
                {
                    ModLogger.LogError($"Failed to create custom wave directory: {e.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    ModLogger.LogError($"Unexpected error creating custom wave directory: {ex.Message}");
                    return false;
                }

            foreach (var (type, waves) in WaveData.Waves)
            {
                if (waves.Length == 0) continue;

                if (File.Exists($"{CustomWavePath}/{type}.json")) continue;

                try
                {
                    var jsonData = JsonSerializerFactory.Instance.Serialize(waves);
                    File.WriteAllText($"{CustomWavePath}/{type}.json", jsonData);
                }
                catch (IOException e)
                {
                    ModLogger.LogError($"Failed to create default config for wave type '{type}': {e.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    ModLogger.LogError(
                        $"Unexpected error creating default config for wave type '{type}': {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        public readonly struct CustomWave(string name, string[]? waves)
        {
            public string Name { get; } = name;
            public string[]? Waves { get; } = waves;

            public bool Validate()
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    ModLogger.LogError("CustomWave validation failed: Name is null or empty.");
                    return false;
                }

                if (Waves == null || Waves.Length == 0)
                {
                    ModLogger.LogError(
                        $"CustomWave validation failed: Waves array is null or empty for wave '{Name}'.");
                    return false;
                }

                foreach (var wave in Waves)
                {
                    if (string.IsNullOrWhiteSpace(wave))
                    {
                        ModLogger.LogError(
                            $"CustomWave validation failed: One of the waves in '{Name}' is null or empty.");
                        return false;
                    }

                    if (Regex.IsMatch(wave, @"^[0-9A-Fa-f]{16}$")) continue;

                    ModLogger.LogError(
                        $"CustomWave validation failed: Wave '{wave}' in '{Name}' does not match the required format.");
                    return false;
                }

                return true;
            }
        }
    }
}
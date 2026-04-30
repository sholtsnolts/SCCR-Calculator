using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SccrWpfApp.Models;

namespace SccrWpfApp.Services
{
    /// <summary>
    /// Loads and saves the editable device library. User edits are stored in AppData.
    /// </summary>
    public class DeviceDatabaseService
    {
        private readonly string _appDataFolder;
        private readonly string _defaultDatabasePath;
        private readonly string _settingsPath;
        private string _databasePath;

        public DeviceDatabaseService()
        {
            _appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SccrWpfApp");

            Directory.CreateDirectory(_appDataFolder);
            _defaultDatabasePath = Path.Combine(_appDataFolder, "DeviceDatabase.json");
            _settingsPath = Path.Combine(_appDataFolder, "Settings.json");
            _databasePath = LoadConfiguredDatabasePath();
        }

        public string DatabasePath => _databasePath;

        public ObservableCollection<DeviceDatabaseEntry> LoadDevices()
        {
            EnsureUserDatabaseExists();

            try
            {
                var json = File.ReadAllText(_databasePath);
                var data = JsonSerializer.Deserialize<DeviceDatabaseFile>(json, GetJsonOptions());
                return new ObservableCollection<DeviceDatabaseEntry>(data?.Devices ?? new List<DeviceDatabaseEntry>());
            }
            catch
            {
                return new ObservableCollection<DeviceDatabaseEntry>();
            }
        }

        public void SaveDevices(IEnumerable<DeviceDatabaseEntry> devices)
        {
            var data = new DeviceDatabaseFile
            {
                Devices = devices
                    .Where(device => !string.IsNullOrWhiteSpace(device.Manufacturer) || !string.IsNullOrWhiteSpace(device.PartNumber))
                    .Select(NormalizeEntry)
                    .OrderBy(device => device.Manufacturer)
                    .ThenBy(device => device.PartNumber)
                    .ToList()
            };

            var json = JsonSerializer.Serialize(data, GetJsonOptions());
            var folder = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrWhiteSpace(folder))
                Directory.CreateDirectory(folder);

            File.WriteAllText(_databasePath, json);
        }

        public void ExportDatabase(string destinationPath)
        {
            EnsureUserDatabaseExists();
            var folder = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(folder))
                Directory.CreateDirectory(folder);

            File.Copy(_databasePath, destinationPath, overwrite: true);
        }

        public void ImportDatabase(string sourcePath)
        {
            ValidateDatabaseFile(sourcePath);
            EnsureUserDatabaseExists();
            File.Copy(sourcePath, _databasePath, overwrite: true);
        }

        public void SetDatabasePath(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Database path is required.", nameof(databasePath));

            var fullPath = Path.GetFullPath(databasePath);
            var folder = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(folder))
                Directory.CreateDirectory(folder);

            if (File.Exists(fullPath))
            {
                ValidateDatabaseFile(fullPath);
            }
            else
            {
                EnsureUserDatabaseExists();
                File.Copy(_databasePath, fullPath, overwrite: false);
            }

            _databasePath = fullPath;
            SaveSettings();
        }

        public void AddOrUpdate(DeviceDatabaseEntry entry)
        {
            var devices = LoadDevices();
            var normalizedEntry = NormalizeEntry(entry);
            var existing = devices.FirstOrDefault(device =>
                device.Id.Equals(normalizedEntry.Id, StringComparison.OrdinalIgnoreCase)
                || (device.Manufacturer.Equals(normalizedEntry.Manufacturer, StringComparison.OrdinalIgnoreCase)
                    && device.PartNumber.Equals(normalizedEntry.PartNumber, StringComparison.OrdinalIgnoreCase)));

            if (existing == null)
            {
                devices.Add(normalizedEntry);
            }
            else
            {
                CopyEntry(normalizedEntry, existing);
            }

            SaveDevices(devices);
        }

        private void EnsureUserDatabaseExists()
        {
            if (File.Exists(_databasePath))
                return;

            var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DeviceDatabase.json");
            if (File.Exists(bundledPath))
            {
                var folder = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrWhiteSpace(folder))
                    Directory.CreateDirectory(folder);

                File.Copy(bundledPath, _databasePath, overwrite: false);
                return;
            }

            SaveDevices(Array.Empty<DeviceDatabaseEntry>());
        }

        private string LoadConfiguredDatabasePath()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                    return _defaultDatabasePath;

                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<DeviceDatabaseSettings>(json, GetJsonOptions());
                if (!string.IsNullOrWhiteSpace(settings?.DatabasePath))
                    return Path.GetFullPath(settings.DatabasePath);
            }
            catch
            {
                // Fall back to the default AppData database.
            }

            return _defaultDatabasePath;
        }

        private void SaveSettings()
        {
            var settings = new DeviceDatabaseSettings { DatabasePath = _databasePath };
            var json = JsonSerializer.Serialize(settings, GetJsonOptions());
            File.WriteAllText(_settingsPath, json);
        }

        private static void ValidateDatabaseFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            JsonSerializer.Deserialize<DeviceDatabaseFile>(json, GetJsonOptions());
        }

        private static DeviceDatabaseEntry NormalizeEntry(DeviceDatabaseEntry entry)
        {
            var normalized = entry.Clone();
            if (string.IsNullOrWhiteSpace(normalized.Id))
                normalized.Id = DeviceDatabaseEntry.CreateId(normalized.Manufacturer, normalized.PartNumber);

            return normalized;
        }

        private static void CopyEntry(DeviceDatabaseEntry source, DeviceDatabaseEntry target)
        {
            target.Id = source.Id;
            target.Manufacturer = source.Manufacturer;
            target.PartNumber = source.PartNumber;
            target.InternalPartNumber = source.InternalPartNumber;
            target.DeviceType = source.DeviceType;
            target.Description = source.Description;
            target.ImagePath = source.ImagePath;
            target.Voltage = source.Voltage;
            target.SccrRating = source.SccrRating;
            target.InterruptingRating = source.InterruptingRating;
            target.OcpdAmps = source.OcpdAmps;
            target.InputCurrentAmps = source.InputCurrentAmps;
            target.IsFusedDisconnect = source.IsFusedDisconnect;
            target.FuseManufacturer = source.FuseManufacturer;
            target.FusePartNumber = source.FusePartNumber;
            target.FuseInternalPartNumber = source.FuseInternalPartNumber;
            target.FuseClass = source.FuseClass;
            target.FuseAmps = source.FuseAmps;
            target.LetThroughCurrent = source.LetThroughCurrent;
            target.Source = source.Source;
            target.Notes = source.Notes;
            target.ExemptFromSccr = source.ExemptFromSccr;
            target.ExemptReason = source.ExemptReason;
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        private class DeviceDatabaseFile
        {
            [JsonPropertyName("devices")]
            public List<DeviceDatabaseEntry>? Devices { get; set; }
        }

        private class DeviceDatabaseSettings
        {
            [JsonPropertyName("databasePath")]
            public string DatabasePath { get; set; } = "";
        }
    }
}

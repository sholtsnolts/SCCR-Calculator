using System.Collections.ObjectModel;
using System.Globalization;
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

        public void ExportDatabaseCsv(string destinationPath)
        {
            var devices = LoadDevices();
            var lines = new List<string>
            {
                string.Join(",",
                    Csv("Manufacturer"),
                    Csv("PartNumber"),
                    Csv("InternalPartNumber"),
                    Csv("DeviceType"),
                    Csv("Description"),
                    Csv("ImagePath"),
                    Csv("Voltage"),
                    Csv("SccrRating"),
                    Csv("InterruptingRating"),
                    Csv("OcpdAmps"),
                    Csv("InputCurrentAmps"),
                    Csv("IsFusedDisconnect"),
                    Csv("FuseManufacturer"),
                    Csv("FusePartNumber"),
                    Csv("FuseInternalPartNumber"),
                    Csv("FuseClass"),
                    Csv("FuseAmps"),
                    Csv("LetThroughCurrent"),
                    Csv("Source"),
                    Csv("Notes"),
                    Csv("ExemptFromSccr"),
                    Csv("ExemptReason"))
            };

            foreach (var device in devices)
            {
                lines.Add(string.Join(",",
                    Csv(device.Manufacturer),
                    Csv(device.PartNumber),
                    Csv(device.InternalPartNumber),
                    Csv(device.DeviceType),
                    Csv(device.Description),
                    Csv(device.ImagePath),
                    Csv(device.Voltage),
                    Csv(device.SccrRating),
                    Csv(device.InterruptingRating),
                    Csv(device.OcpdAmps),
                    Csv(device.InputCurrentAmps),
                    Csv(device.IsFusedDisconnect),
                    Csv(device.FuseManufacturer),
                    Csv(device.FusePartNumber),
                    Csv(device.FuseInternalPartNumber),
                    Csv(device.FuseClass),
                    Csv(device.FuseAmps),
                    Csv(device.LetThroughCurrent),
                    Csv(device.Source),
                    Csv(device.Notes),
                    Csv(device.ExemptFromSccr),
                    Csv(device.ExemptReason)));
            }

            File.WriteAllLines(destinationPath, lines);
        }

        public void ImportDatabase(string sourcePath)
        {
            ValidateDatabaseFile(sourcePath);
            EnsureUserDatabaseExists();
            File.Copy(sourcePath, _databasePath, overwrite: true);
        }

        public void ImportDatabaseCsv(string sourcePath)
        {
            var rows = ReadCsvRows(sourcePath);
            if (rows.Count == 0)
                throw new InvalidDataException("CSV file is empty.");

            var headers = rows[0]
                .Select((header, index) => new { Header = NormalizeHeader(header), Index = index })
                .Where(item => !string.IsNullOrWhiteSpace(item.Header))
                .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

            var devices = new List<DeviceDatabaseEntry>();
            foreach (var row in rows.Skip(1))
            {
                var entry = new DeviceDatabaseEntry
                {
                    Manufacturer = GetCsv(row, headers, "manufacturer"),
                    PartNumber = GetCsv(row, headers, "partnumber"),
                    InternalPartNumber = GetCsv(row, headers, "internalpartnumber", "ipn"),
                    DeviceType = GetCsv(row, headers, "devicetype"),
                    Description = GetCsv(row, headers, "description"),
                    ImagePath = GetCsv(row, headers, "imagepath"),
                    Voltage = GetCsvDouble(row, headers, "voltage"),
                    SccrRating = GetCsvDouble(row, headers, "sccrrating", "sccr"),
                    InterruptingRating = GetCsvDouble(row, headers, "interruptingrating", "ir"),
                    OcpdAmps = GetCsvDouble(row, headers, "ocpdamps", "ocpdamprating"),
                    InputCurrentAmps = GetCsvDouble(row, headers, "inputcurrentamps", "inputcurrent"),
                    IsFusedDisconnect = GetCsvBool(row, headers, "isfuseddisconnect", "fuseddisconnect"),
                    FuseManufacturer = GetCsv(row, headers, "fusemanufacturer"),
                    FusePartNumber = GetCsv(row, headers, "fusepartnumber"),
                    FuseInternalPartNumber = GetCsv(row, headers, "fuseinternalpartnumber", "fuseipn"),
                    FuseClass = GetCsv(row, headers, "fuseclass"),
                    FuseAmps = GetCsvDouble(row, headers, "fuseamps", "fusea"),
                    LetThroughCurrent = GetCsvDouble(row, headers, "letthroughcurrent"),
                    Source = GetCsv(row, headers, "source"),
                    Notes = GetCsv(row, headers, "notes"),
                    ExemptFromSccr = GetCsvBool(row, headers, "exemptfromsccr"),
                    ExemptReason = GetCsv(row, headers, "exemptreason")
                };

                if (string.IsNullOrWhiteSpace(entry.Manufacturer) && string.IsNullOrWhiteSpace(entry.PartNumber))
                    continue;

                entry.Id = DeviceDatabaseEntry.CreateId(entry.Manufacturer, entry.PartNumber);
                devices.Add(entry);
            }

            SaveDevices(devices);
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

        private static string Csv(object? value)
        {
            var text = value switch
            {
                null => "",
                double number => number.ToString(CultureInfo.InvariantCulture),
                bool flag => flag ? "true" : "false",
                _ => value.ToString() ?? ""
            };

            return text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r')
                ? "\"" + text.Replace("\"", "\"\"") + "\""
                : text;
        }

        private static List<List<string>> ReadCsvRows(string sourcePath)
        {
            var rows = new List<List<string>>();
            foreach (var line in File.ReadLines(sourcePath))
            {
                rows.Add(ParseCsvLine(line));
            }

            return rows;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            values.Add(current.ToString());
            return values;
        }

        private static string NormalizeHeader(string header)
        {
            return new string((header ?? "")
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static string GetCsv(List<string> row, Dictionary<string, int> headers, params string[] names)
        {
            foreach (var name in names)
            {
                if (headers.TryGetValue(NormalizeHeader(name), out var index) && index < row.Count)
                    return row[index].Trim();
            }

            return "";
        }

        private static double? GetCsvDouble(List<string> row, Dictionary<string, int> headers, params string[] names)
        {
            var text = GetCsv(row, headers, names);
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        private static bool GetCsvBool(List<string> row, Dictionary<string, int> headers, params string[] names)
        {
            var text = GetCsv(row, headers, names);
            return text.Equals("true", StringComparison.OrdinalIgnoreCase)
                || text.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || text.Equals("1", StringComparison.OrdinalIgnoreCase);
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

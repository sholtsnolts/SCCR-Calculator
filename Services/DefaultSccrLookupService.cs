using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SccrWpfApp.Services
{
    /// <summary>
    /// Provides UL 508A SB4.1 default SCCR values by device type.
    /// Loads from DefaultSccrLookup.json.
    /// </summary>
    public class DefaultSccrLookup
    {
        private Dictionary<string, double> _lookupTable = new();
        private Dictionary<string, string> _sourceTable = new();

        public DefaultSccrLookup()
        {
            LoadLookupTable();
        }

        private void LoadLookupTable()
        {
            try
            {
                // Try to load from Data folder relative to app directory
                var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DefaultSccrLookup.json");
                
                if (!File.Exists(dataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: DefaultSccrLookup.json not found at {dataPath}. Using hardcoded defaults.");
                    LoadHardcodedDefaults();
                    return;
                }

                var json = File.ReadAllText(dataPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<DefaultSccrData>(json, options);

                if (data?.DefaultSccrValues != null)
                {
                    foreach (var entry in data.DefaultSccrValues)
                    {
                        _lookupTable[entry.ComponentType.ToLower()] = entry.DefaultSccrKa;
                        _sourceTable[entry.ComponentType.ToLower()] = entry.Source;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading DefaultSccrLookup.json: {ex.Message}. Using hardcoded defaults.");
                LoadHardcodedDefaults();
            }
        }

        private void LoadHardcodedDefaults()
        {
            // Fallback hardcoded defaults per UL 508A SB4.1
            _lookupTable["power-distribution-block"] = 5;
            _lookupTable["fuse-block"] = 10;
            _lookupTable["fuse + fuse-block"] = 10;
            _lookupTable["terminal-block"] = 5;
            _lookupTable["contactor"] = 5;
            _lookupTable["starter"] = 5;
            _lookupTable["transformer"] = 5;
            _lookupTable["drive"] = 5;
            _lookupTable["power-supply"] = 5;
            _lookupTable["brake-resistor"] = 5;
            _lookupTable["disconnect"] = 65; // Typical disconnect rating
            _lookupTable["breaker"] = 65;
            _lookupTable["fuse"] = 100;
            _lookupTable["load"] = 5;

            foreach (var key in _lookupTable.Keys.ToList())
            {
                _sourceTable[key] = "UL 508A SB4.1 (Hardcoded Default)";
            }
        }

        /// <summary>
        /// Get the default SCCR in kA for a device type.
        /// </summary>
        public double? GetDefaultSccr(string deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType))
                return null;

            var key = deviceType.ToLower().Trim();
            return _lookupTable.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Get the source/reason for the default SCCR.
        /// </summary>
        public string? GetDefaultSource(string deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType))
                return null;

            var key = deviceType.ToLower().Trim();
            return _sourceTable.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Get all available device types in the lookup table.
        /// </summary>
        public IEnumerable<string> GetAvailableDeviceTypes()
        {
            return _lookupTable.Keys;
        }

        // JSON schema classes
        private class DefaultSccrData
        {
            [JsonPropertyName("defaultSccrValues")]
            public List<DefaultSccrEntry>? DefaultSccrValues { get; set; }
        }

        private class DefaultSccrEntry
        {
            [JsonPropertyName("componentType")]
            public string ComponentType { get; set; } = "";

            [JsonPropertyName("defaultSccrKa")]
            public double DefaultSccrKa { get; set; }

            [JsonPropertyName("source")]
            public string Source { get; set; } = "";
        }
    }
}

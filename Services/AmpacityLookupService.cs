using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SccrWpfApp.Services
{
    /// <summary>
    /// Looks up copper conductor size from UL 508A Table 28.1 ampacity values.
    /// </summary>
    public class AmpacityLookupService
    {
        private List<AmpacityEntry> _entries = new();

        public AmpacityLookupService()
        {
            LoadTable();
        }

        public string GetCopper75CConductorSize(double? requiredAmps)
        {
            if (!requiredAmps.HasValue || requiredAmps <= 0)
                return "AWG TBD";

            var match = _entries
                .OrderBy(entry => entry.Copper75CAmps)
                .FirstOrDefault(entry => requiredAmps <= entry.Copper75CAmps);

            return match?.ConductorSize ?? "Table range exceeded";
        }

        private void LoadTable()
        {
            try
            {
                var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "AmpacityTable28_1.json");
                if (!File.Exists(dataPath))
                {
                    LoadHardcodedDefaults();
                    return;
                }

                var json = File.ReadAllText(dataPath);
                var data = JsonSerializer.Deserialize<AmpacityData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _entries = data?.Entries ?? new List<AmpacityEntry>();
                if (_entries.Count == 0)
                    LoadHardcodedDefaults();
            }
            catch
            {
                LoadHardcodedDefaults();
            }
        }

        private void LoadHardcodedDefaults()
        {
            _entries = new List<AmpacityEntry>
            {
                new() { ConductorSize = "14 AWG", Copper75CAmps = 15 },
                new() { ConductorSize = "12 AWG", Copper75CAmps = 20 },
                new() { ConductorSize = "10 AWG", Copper75CAmps = 30 },
                new() { ConductorSize = "8 AWG", Copper75CAmps = 50 },
                new() { ConductorSize = "6 AWG", Copper75CAmps = 65 },
                new() { ConductorSize = "4 AWG", Copper75CAmps = 85 },
                new() { ConductorSize = "3 AWG", Copper75CAmps = 100 },
                new() { ConductorSize = "2 AWG", Copper75CAmps = 115 },
                new() { ConductorSize = "1 AWG", Copper75CAmps = 130 },
                new() { ConductorSize = "1/0 AWG", Copper75CAmps = 150 },
                new() { ConductorSize = "2/0 AWG", Copper75CAmps = 175 },
                new() { ConductorSize = "3/0 AWG", Copper75CAmps = 200 },
                new() { ConductorSize = "4/0 AWG", Copper75CAmps = 230 },
                new() { ConductorSize = "250 kcmil", Copper75CAmps = 255 },
                new() { ConductorSize = "300 kcmil", Copper75CAmps = 285 },
                new() { ConductorSize = "350 kcmil", Copper75CAmps = 310 },
                new() { ConductorSize = "400 kcmil", Copper75CAmps = 335 },
                new() { ConductorSize = "500 kcmil", Copper75CAmps = 380 },
                new() { ConductorSize = "600 kcmil", Copper75CAmps = 420 },
                new() { ConductorSize = "700 kcmil", Copper75CAmps = 460 },
                new() { ConductorSize = "750 kcmil", Copper75CAmps = 475 },
                new() { ConductorSize = "800 kcmil", Copper75CAmps = 490 },
                new() { ConductorSize = "900 kcmil", Copper75CAmps = 520 },
                new() { ConductorSize = "1000 kcmil", Copper75CAmps = 545 }
            };
        }

        private class AmpacityData
        {
            [JsonPropertyName("entries")]
            public List<AmpacityEntry>? Entries { get; set; }
        }

        private class AmpacityEntry
        {
            [JsonPropertyName("conductorSize")]
            public string ConductorSize { get; set; } = "";

            [JsonPropertyName("copper75CAmps")]
            public double Copper75CAmps { get; set; }
        }
    }
}

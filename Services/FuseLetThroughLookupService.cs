using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SccrWpfApp.Services
{
    /// <summary>
    /// Provides fuse let-through current lookup tables (Class J, CC, RK1, etc.)
    /// Loads from FuseLetThroughTable.json.
    /// </summary>
    public class FuseLetThroughLookup
    {
        public class LetThroughPoint
        {
            [JsonPropertyName("faultCurrentKa")]
            public double FaultCurrentKa { get; set; }

            [JsonPropertyName("peakLetThroughKa")]
            public double PeakLetThroughKa { get; set; }
        }

        public class FuseEntry
        {
            [JsonPropertyName("fuseClass")]
            public string FuseClass { get; set; } = "";

            [JsonPropertyName("ampRating")]
            public double AmpRating { get; set; }

            [JsonPropertyName("letThroughCurrent")]
            public List<LetThroughPoint> LetThroughCurrent { get; set; } = new();
        }

        private List<FuseEntry> _fuseTable = new();

        public FuseLetThroughLookup()
        {
            LoadFuseTable();
        }

        private void LoadFuseTable()
        {
            try
            {
                var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "FuseLetThroughTable.json");

                if (!File.Exists(dataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: FuseLetThroughTable.json not found at {dataPath}");
                    LoadHardcodedDefaults();
                    return;
                }

                var json = File.ReadAllText(dataPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<FuseTableData>(json, options);

                if (data?.Fuses != null)
                {
                    _fuseTable = data.Fuses;
                }
                else
                {
                    LoadHardcodedDefaults();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading FuseLetThroughTable.json: {ex.Message}");
                LoadHardcodedDefaults();
            }
        }

        private void LoadHardcodedDefaults()
        {
            // Minimal hardcoded defaults
            _fuseTable = new List<FuseEntry>
            {
                new FuseEntry
                {
                    FuseClass = "J",
                    AmpRating = 100,
                    LetThroughCurrent = new List<LetThroughPoint>
                    {
                        new LetThroughPoint { FaultCurrentKa = 50, PeakLetThroughKa = 35 },
                        new LetThroughPoint { FaultCurrentKa = 100, PeakLetThroughKa = 45 },
                        new LetThroughPoint { FaultCurrentKa = 200, PeakLetThroughKa = 55 }
                    }
                },
                new FuseEntry
                {
                    FuseClass = "CC",
                    AmpRating = 60,
                    LetThroughCurrent = new List<LetThroughPoint>
                    {
                        new LetThroughPoint { FaultCurrentKa = 50, PeakLetThroughKa = 18 },
                        new LetThroughPoint { FaultCurrentKa = 100, PeakLetThroughKa = 25 },
                        new LetThroughPoint { FaultCurrentKa = 200, PeakLetThroughKa = 35 }
                    }
                }
            };
        }

        /// <summary>
        /// Get let-through current for a specific fuse class and amp rating at a given fault current level.
        /// </summary>
        public double? GetLetThroughCurrent(string fuseClass, double ampRating, double faultCurrentKa)
        {
            var entry = _fuseTable.FirstOrDefault(f => 
                f.FuseClass.Equals(fuseClass, StringComparison.OrdinalIgnoreCase) && 
                Math.Abs(f.AmpRating - ampRating) < 0.01);

            if (entry == null)
                return null;

            // Find the exact or closest fault current level
            var letThrough = entry.LetThroughCurrent.FirstOrDefault(lt => 
                Math.Abs(lt.FaultCurrentKa - faultCurrentKa) < 0.01);

            return letThrough?.PeakLetThroughKa;
        }

        /// <summary>
        /// Gets the let-through current at the requested fault current, using the next higher
        /// table point when there is no exact match. This is conservative for tabular lookups.
        /// </summary>
        public double? GetConservativeLetThroughCurrent(string fuseClass, double ampRating, double faultCurrentKa)
        {
            var entry = _fuseTable.FirstOrDefault(f =>
                f.FuseClass.Equals(fuseClass, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs(f.AmpRating - ampRating) < 0.01);

            if (entry == null || entry.LetThroughCurrent.Count == 0)
                return null;

            var orderedPoints = entry.LetThroughCurrent
                .OrderBy(lt => lt.FaultCurrentKa)
                .ToList();

            var exact = orderedPoints.FirstOrDefault(lt => Math.Abs(lt.FaultCurrentKa - faultCurrentKa) < 0.01);
            if (exact != null)
                return exact.PeakLetThroughKa;

            var nextHigher = orderedPoints.FirstOrDefault(lt => lt.FaultCurrentKa >= faultCurrentKa);
            return nextHigher?.PeakLetThroughKa;
        }

        /// <summary>
        /// Get all let-through points for a specific fuse.
        /// </summary>
        public List<LetThroughPoint>? GetFuseLetThroughCurve(string fuseClass, double ampRating)
        {
            var entry = _fuseTable.FirstOrDefault(f =>
                f.FuseClass.Equals(fuseClass, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs(f.AmpRating - ampRating) < 0.01);

            return entry?.LetThroughCurrent;
        }

        /// <summary>
        /// Get all available fuse classes.
        /// </summary>
        public IEnumerable<string> GetAvailableFuseClasses()
        {
            return _fuseTable.Select(f => f.FuseClass).Distinct();
        }

        /// <summary>
        /// Get all amp ratings for a specific fuse class.
        /// </summary>
        public IEnumerable<double> GetAvailableAmpRatings(string fuseClass)
        {
            return _fuseTable
                .Where(f => f.FuseClass.Equals(fuseClass, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.AmpRating)
                .Distinct()
                .OrderBy(a => a);
        }

        /// <summary>
        /// Get all available fault current levels.
        /// </summary>
        public IEnumerable<double> GetAvailableFaultCurrentLevels()
        {
            return _fuseTable
                .SelectMany(f => f.LetThroughCurrent)
                .Select(lt => lt.FaultCurrentKa)
                .Distinct()
                .OrderBy(fc => fc);
        }

        private class FuseTableData
        {
            [JsonPropertyName("fuses")]
            public List<FuseEntry>? Fuses { get; set; }
        }
    }
}

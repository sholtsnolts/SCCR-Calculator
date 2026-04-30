using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SccrWpfApp.Models;

namespace SccrWpfApp.Services
{
    /// <summary>
    /// Provides manufacturer combination SCCR ratings loaded from CombinationRatings.json.
    /// Ratings are applied only when the documented conditions match the circuit.
    /// </summary>
    public class CombinationRatingLookup
    {
        private List<CombinationRatingEntry> _ratings = new();

        public CombinationRatingLookup()
        {
            LoadCombinationRatings();
        }

        private void LoadCombinationRatings()
        {
            try
            {
                var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "CombinationRatings.json");

                if (!File.Exists(dataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: CombinationRatings.json not found at {dataPath}");
                    LoadHardcodedDefaults();
                    return;
                }

                var json = File.ReadAllText(dataPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<CombinationRatingData>(json, options);

                _ratings = data?.CombinationRatings ?? new List<CombinationRatingEntry>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading CombinationRatings.json: {ex.Message}");
                LoadHardcodedDefaults();
            }
        }

        private void LoadHardcodedDefaults()
        {
            _ratings = new List<CombinationRatingEntry>
            {
                new CombinationRatingEntry
                {
                    Id = "eaton-disconnect-lpj-100sp",
                    UpstreamDevice = "Eaton R9K3100FJ",
                    OcpdType = "Class J Fuse",
                    OcpdMaxAmps = 100,
                    ResultingSccrKa = 100,
                    DownstreamMinSccrKa = 10,
                    Voltage = 480,
                    Source = "Eaton combination rating",
                    Notes = "With LPJ-100SP Class J fuses"
                }
            };
        }

        /// <summary>
        /// Finds the best documented combination rating that can be applied to a node.
        /// </summary>
        public CombinationRatingMatch? FindBestMatch(CircuitNode downstreamNode, double baseComponentSccrKa)
        {
            var ancestors = GetAncestors(downstreamNode).ToList();
            if (downstreamNode.Device == null || ancestors.Count == 0)
                return null;

            var candidates = new List<CombinationRatingMatch>();

            foreach (var rating in _ratings)
            {
                if (!rating.IsUsable())
                    continue;

                if (baseComponentSccrKa + 0.001 < rating.DownstreamMinSccrKa)
                    continue;

                if (!VoltageMatches(downstreamNode.Device.Voltage, rating.Voltage))
                    continue;

                if (!DownstreamDeviceMatches(downstreamNode, rating))
                    continue;

                foreach (var upstreamNode in ancestors)
                {
                    if (upstreamNode.Device == null)
                        continue;

                    if (!UpstreamDeviceMatches(upstreamNode.Device, rating))
                        continue;

                    var ocpdNode = FindMatchingOcpd(upstreamNode, downstreamNode, rating);
                    if (ocpdNode == null)
                        continue;

                    candidates.Add(new CombinationRatingMatch
                    {
                        Rating = rating,
                        UpstreamNode = upstreamNode,
                        OcpdNode = ocpdNode,
                        AppliedSccrKa = rating.ResultingSccrKa
                    });
                }
            }

            return candidates
                .OrderByDescending(match => match.AppliedSccrKa)
                .FirstOrDefault();
        }

        private static IEnumerable<CircuitNode> GetAncestors(CircuitNode node)
        {
            var current = node.Parent;
            while (current != null)
            {
                yield return current;
                current = current.Parent;
            }
        }

        private static CircuitNode? FindMatchingOcpd(CircuitNode upstreamNode, CircuitNode downstreamNode, CombinationRatingEntry rating)
        {
            if (OcpdMatches(upstreamNode.Device, upstreamNode.DeviceType, rating))
                return upstreamNode;

            var pathNodes = GetPathBetween(upstreamNode, downstreamNode);
            return pathNodes.FirstOrDefault(node => OcpdMatches(node.Device, node.DeviceType, rating));
        }

        private static IEnumerable<CircuitNode> GetPathBetween(CircuitNode upstreamNode, CircuitNode downstreamNode)
        {
            var stack = new Stack<CircuitNode>();
            var current = downstreamNode.Parent;

            while (current != null && current != upstreamNode)
            {
                stack.Push(current);
                current = current.Parent;
            }

            return stack;
        }

        private static bool UpstreamDeviceMatches(Device device, CombinationRatingEntry rating)
        {
            var expected = Normalize(rating.UpstreamDevice);
            if (string.IsNullOrWhiteSpace(expected))
                return true;

            var manufacturerAndPart = Normalize($"{device.Manufacturer} {device.PartNumber}");
            var partOnly = Normalize(device.PartNumber);
            var description = Normalize(device.Description);

            return ContainsNonEmpty(manufacturerAndPart, expected)
                || ContainsNonEmpty(expected, manufacturerAndPart)
                || (!string.IsNullOrWhiteSpace(partOnly) && partOnly.Equals(expected, StringComparison.OrdinalIgnoreCase))
                || ContainsNonEmpty(description, expected);
        }

        private static bool DownstreamDeviceMatches(CircuitNode downstreamNode, CombinationRatingEntry rating)
        {
            if (downstreamNode.Device == null)
                return false;

            return MatchesAnyOrEmpty(rating.DownstreamDeviceTypes, downstreamNode.DeviceType)
                && MatchesAnyOrEmpty(rating.DownstreamManufacturers, downstreamNode.Device.Manufacturer)
                && MatchesAnyOrEmpty(rating.DownstreamPartNumbers, downstreamNode.Device.PartNumber);
        }

        private static bool OcpdMatches(Device? device, string deviceType, CombinationRatingEntry rating)
        {
            if (device == null)
                return false;

            if (rating.OcpdMaxAmps > 0 && (!device.FuseAmps.HasValue || device.FuseAmps > rating.OcpdMaxAmps))
                return false;

            var expectedType = Normalize(rating.OcpdType);
            if (string.IsNullOrWhiteSpace(expectedType))
                return true;

            var fuseClass = Normalize(device.FuseClass);
            var nodeType = Normalize(deviceType);
            var description = Normalize(device.Description);

            return ContainsNonEmpty(expectedType, fuseClass)
                || ContainsNonEmpty(nodeType, expectedType)
                || ContainsNonEmpty(expectedType, nodeType)
                || ContainsNonEmpty(description, expectedType);
        }

        private static bool VoltageMatches(double? deviceVoltage, double ratingVoltage)
        {
            return ratingVoltage <= 0 || !deviceVoltage.HasValue || deviceVoltage <= ratingVoltage;
        }

        private static string Normalize(string? value)
        {
            return (value ?? "")
                .Replace("-", "")
                .Replace("_", "")
                .Trim()
                .ToLowerInvariant();
        }

        private static bool ContainsNonEmpty(string value, string expected)
        {
            return !string.IsNullOrWhiteSpace(value)
                && !string.IsNullOrWhiteSpace(expected)
                && value.Contains(expected);
        }

        private static bool MatchesAnyOrEmpty(List<string> expectedValues, string actualValue)
        {
            if (expectedValues.Count == 0)
                return true;

            var actual = Normalize(actualValue);
            return expectedValues
                .Select(Normalize)
                .Any(expected => ContainsNonEmpty(actual, expected) || ContainsNonEmpty(expected, actual));
        }

        private class CombinationRatingData
        {
            [JsonPropertyName("combinationRatings")]
            public List<CombinationRatingEntry>? CombinationRatings { get; set; }
        }
    }

    public class CombinationRatingEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("upstreamDevice")]
        public string UpstreamDevice { get; set; } = "";

        [JsonPropertyName("ocpdType")]
        public string OcpdType { get; set; } = "";

        [JsonPropertyName("ocpdMaxAmps")]
        public double OcpdMaxAmps { get; set; }

        [JsonPropertyName("resultingSccrKa")]
        public double ResultingSccrKa { get; set; }

        [JsonPropertyName("downstreamMinSccrKa")]
        public double DownstreamMinSccrKa { get; set; }

        [JsonPropertyName("downstreamDeviceTypes")]
        public List<string> DownstreamDeviceTypes { get; set; } = new();

        [JsonPropertyName("downstreamManufacturers")]
        public List<string> DownstreamManufacturers { get; set; } = new();

        [JsonPropertyName("downstreamPartNumbers")]
        public List<string> DownstreamPartNumbers { get; set; } = new();

        [JsonPropertyName("voltage")]
        public double Voltage { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = "";

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        public bool IsUsable()
        {
            return ResultingSccrKa > 0
                && DownstreamMinSccrKa > 0
                && !string.IsNullOrWhiteSpace(Source);
        }
    }

    public class CombinationRatingMatch
    {
        public CombinationRatingEntry Rating { get; set; } = new();
        public CircuitNode? UpstreamNode { get; set; }
        public CircuitNode? OcpdNode { get; set; }
        public double AppliedSccrKa { get; set; }

        public string Describe()
        {
            return $"Manufacturer combination rating {Rating.Id}: {AppliedSccrKa} kA with {UpstreamNode?.Name} and {OcpdNode?.Name}; source: {Rating.Source}. {Rating.Notes}".Trim();
        }
    }
}

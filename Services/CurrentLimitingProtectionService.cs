using SccrWpfApp.Models;

namespace SccrWpfApp.Services
{
    /// <summary>
    /// Evaluates upstream current-limiting fuse protection for downstream component SCCR.
    /// This logic modifies component SCCR only; it never raises an OCPD interrupting rating.
    /// </summary>
    public class CurrentLimitingProtectionService
    {
        private static readonly HashSet<string> CurrentLimitingFuseClasses = new(StringComparer.OrdinalIgnoreCase)
        {
            "CC",
            "G",
            "J",
            "L",
            "RK1",
            "RK5",
            "T"
        };

        private readonly FuseLetThroughLookup _fuseLetThroughLookup;

        public CurrentLimitingProtectionService()
        {
            _fuseLetThroughLookup = new FuseLetThroughLookup();
        }

        public CurrentLimitingProtectionMatch? FindProtection(
            CircuitNode downstreamNode,
            double componentSccrKa,
            double availableFaultCurrentKa)
        {
            if (availableFaultCurrentKa <= 0 || componentSccrKa <= 0)
                return null;

            foreach (var upstreamNode in GetAncestors(downstreamNode))
            {
                var upstreamDevice = upstreamNode.Device;
                if (upstreamDevice == null)
                    continue;

                if (!IsCurrentLimitingFuse(upstreamDevice))
                    continue;

                var letThrough = ResolveLetThroughCurrent(upstreamDevice, availableFaultCurrentKa);
                if (!letThrough.HasValue)
                {
                    return new CurrentLimitingProtectionMatch
                    {
                        FuseNode = upstreamNode,
                        IsApplied = false,
                        Notes = $"Current-limiting fuse {upstreamNode.Name} has no let-through value at {availableFaultCurrentKa} kA. Enter a manual value or add a table entry."
                    };
                }

                if (letThrough.Value <= componentSccrKa)
                {
                    return new CurrentLimitingProtectionMatch
                    {
                        FuseNode = upstreamNode,
                        LetThroughCurrentKa = letThrough.Value,
                        AppliedSccrKa = availableFaultCurrentKa,
                        IsApplied = true,
                        Notes = $"Current-limiting fuse {upstreamNode.Name} limits let-through to {letThrough.Value} kA at {availableFaultCurrentKa} kA available fault current."
                    };
                }

                return new CurrentLimitingProtectionMatch
                {
                    FuseNode = upstreamNode,
                    LetThroughCurrentKa = letThrough.Value,
                    IsApplied = false,
                    Notes = $"Current-limiting fuse {upstreamNode.Name} let-through is {letThrough.Value} kA, above downstream component SCCR {componentSccrKa} kA."
                };
            }

            return null;
        }

        private double? ResolveLetThroughCurrent(Device fuseDevice, double availableFaultCurrentKa)
        {
            if (fuseDevice.LetThroughCurrent.HasValue && fuseDevice.LetThroughCurrent > 0)
                return fuseDevice.LetThroughCurrent;

            if (string.IsNullOrWhiteSpace(fuseDevice.FuseClass) || !fuseDevice.FuseAmps.HasValue)
                return null;

            return _fuseLetThroughLookup.GetConservativeLetThroughCurrent(
                fuseDevice.FuseClass,
                fuseDevice.FuseAmps.Value,
                availableFaultCurrentKa);
        }

        private static bool IsCurrentLimitingFuse(Device device)
        {
            var fuseClass = NormalizeFuseClass(device.FuseClass);
            return !string.IsNullOrWhiteSpace(fuseClass)
                && device.FuseAmps.HasValue
                && CurrentLimitingFuseClasses.Contains(fuseClass);
        }

        private static string NormalizeFuseClass(string fuseClass)
        {
            return (fuseClass ?? "")
                .Replace("Class", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "")
                .Replace("-", "")
                .Trim();
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
    }

    public class CurrentLimitingProtectionMatch
    {
        public CircuitNode? FuseNode { get; set; }
        public double? LetThroughCurrentKa { get; set; }
        public double? AppliedSccrKa { get; set; }
        public bool IsApplied { get; set; }
        public string Notes { get; set; } = "";
    }
}

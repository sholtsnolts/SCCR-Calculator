using SccrWpfApp.Models;

namespace SccrWpfApp.Services
{
    /// <summary>
    /// Implements weakest-link SCCR calculation per UL 508A Supplement SB.
    /// </summary>
    public class SccrCalculator
    {
        private readonly DefaultSccrLookup _defaultLookup;
        private readonly CombinationRatingLookup _combinationRatingLookup;
        private readonly CurrentLimitingProtectionService _currentLimitingProtectionService;

        public SccrCalculator()
        {
            _defaultLookup = new DefaultSccrLookup();
            _combinationRatingLookup = new CombinationRatingLookup();
            _currentLimitingProtectionService = new CurrentLimitingProtectionService();
        }

        /// <summary>
        /// Calculate the overall panel SCCR using a weakest-link approach.
        /// The SCCR is limited by the lowest of:
        /// 1. Component SCCRs (explicit or default via UL 508A SB4.1)
        /// 2. OCPD interrupting ratings
        /// 3. Current-limiting fuse effects on downstream component SCCR
        /// </summary>
        public CalculationResult Calculate(CircuitNode rootNode)
        {
            return Calculate(rootNode, 100);
        }

        /// <summary>
        /// Calculate the overall panel SCCR at the selected available fault current.
        /// </summary>
        public CalculationResult Calculate(CircuitNode rootNode, double availableFaultCurrentKa)
        {
            var result = new CalculationResult();
            if (availableFaultCurrentKa <= 0)
            {
                availableFaultCurrentKa = 100;
                result.Warnings.Add("Available fault current was not valid. Calculation used 100 kA.");
            }

            // Collect all relevant devices in the circuit
            var allNodes = new List<CircuitNode>();
            CollectAllNodes(rootNode, allNodes);

            // Find minimum SCCR across all nodes
            double minSccr = double.MaxValue;
            CircuitNode? limitingNode = null;
            string limitingReason = "";

            foreach (var node in allNodes)
            {
                if (node.Device == null)
                    continue;

                var device = node.Device;

                // Skip nodes exempt from SCCR
                if (device.ExemptFromSccr)
                {
                    result.LogEntries.Add(new CalculationLogEntry
                    {
                        NodeName = node.Name,
                        DeviceType = node.DeviceType,
                        Notes = $"Exempt from SCCR: {device.ExemptReason}"
                    });
                    continue;
                }

                // Check for warnings on missing data
                if (!device.SccrRating.HasValue && !device.InterruptingRating.HasValue)
                {
                    var defaultSccr = _defaultLookup.GetDefaultSccr(node.DeviceType);
                    if (!defaultSccr.HasValue)
                    {
                        result.Warnings.Add($"{node.Name}: No SCCR, interrupting rating, or default value available");
                    }
                }

                // Use component SCCR if available, otherwise check for default
                double? componentSccr = null;
                string componentSource = "";

                if (device.SccrRating.HasValue && device.SccrRating > 0)
                {
                    componentSccr = device.SccrRating;
                    componentSource = "Component SCCR (Explicit)";
                }
                else
                {
                    // Try to get default SCCR from device type
                    var defaultSccr = _defaultLookup.GetDefaultSccr(node.DeviceType);
                    if (defaultSccr.HasValue && defaultSccr > 0)
                    {
                        componentSccr = defaultSccr;
                        componentSource = $"Component SCCR (Default: {_defaultLookup.GetDefaultSource(node.DeviceType)})";
                    }
                }

                var calculationNotes = componentSource;

                // Phase 4: Apply documented manufacturer combination ratings to component SCCR only.
                // This does not raise or replace any OCPD interrupting rating.
                if (componentSccr.HasValue)
                {
                    var combinationMatch = _combinationRatingLookup.FindBestMatch(node, componentSccr.Value);
                    if (combinationMatch != null && combinationMatch.AppliedSccrKa > componentSccr)
                    {
                        componentSccr = combinationMatch.AppliedSccrKa;
                        calculationNotes = combinationMatch.Describe();
                    }
                }

                // Phase 5: Current-limiting fuse protection may modify downstream component SCCR
                // only when the documented let-through is within the component's SCCR.
                if (componentSccr.HasValue)
                {
                    var currentLimitingMatch = _currentLimitingProtectionService.FindProtection(
                        node,
                        componentSccr.Value,
                        availableFaultCurrentKa);

                    if (currentLimitingMatch != null)
                    {
                        if (currentLimitingMatch.IsApplied && currentLimitingMatch.AppliedSccrKa > componentSccr)
                        {
                            componentSccr = currentLimitingMatch.AppliedSccrKa;
                            calculationNotes = currentLimitingMatch.Notes;
                        }
                        else if (!currentLimitingMatch.IsApplied && !string.IsNullOrWhiteSpace(currentLimitingMatch.Notes))
                        {
                            result.Warnings.Add($"{node.Name}: {currentLimitingMatch.Notes}");
                        }
                    }
                }

                double? effectiveSccr = componentSccr;
                string limitingSource = calculationNotes;

                // Also consider OCPD interrupting rating as a separate limit.
                // Do not use upstream current-limiting devices or combination SCCR to raise this value.
                if (device.InterruptingRating.HasValue && device.InterruptingRating > 0)
                {
                    if (!effectiveSccr.HasValue || device.InterruptingRating < effectiveSccr)
                    {
                        effectiveSccr = device.InterruptingRating;
                        limitingSource = "OCPD Interrupting Rating";
                    }
                }

                // Log this node's calculation
                if (effectiveSccr.HasValue)
                {
                    result.LogEntries.Add(new CalculationLogEntry
                    {
                        NodeName = node.Name,
                        DeviceType = node.DeviceType,
                        ComponentSccr = componentSccr,
                        OcpdRating = device.InterruptingRating,
                        ResultingSccr = effectiveSccr,
                        Notes = limitingSource
                    });

                    // Update minimum
                    if (effectiveSccr < minSccr)
                    {
                        minSccr = effectiveSccr.Value;
                        limitingNode = node;
                        limitingReason = limitingSource;
                    }
                }
            }

            // Set result
            if (minSccr == double.MaxValue)
            {
                // No devices with SCCR found
                result.OverallSccr = 0;
                result.Warnings.Add("No devices with SCCR or interrupting ratings found in circuit.");
            }
            else
            {
                result.OverallSccr = minSccr;
                result.LimitingNode = limitingNode;
                result.LimitingValue = minSccr;
                result.LimitingReason = $"{limitingNode?.Name} ({limitingReason}: {minSccr} kA)";
            }

            return result;
        }

        private void CollectAllNodes(CircuitNode node, List<CircuitNode> nodes)
        {
            nodes.Add(node);
            foreach (var child in node.Children)
            {
                CollectAllNodes(child, nodes);
            }
        }
    }
}

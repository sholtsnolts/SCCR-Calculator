namespace SccrWpfApp.Models
{
    /// <summary>
    /// Holds the result of an SCCR calculation.
    /// </summary>
    public class CalculationResult
    {
        /// <summary>
        /// Overall panel SCCR rating in kA.
        /// </summary>
        public double OverallSccr { get; set; }

        /// <summary>
        /// The weakest link (node/device) that limits the SCCR.
        /// </summary>
        public CircuitNode? LimitingNode { get; set; }

        /// <summary>
        /// The value that determines the limiting SCCR (either component SCCR or OCPD rating).
        /// </summary>
        public double? LimitingValue { get; set; }

        /// <summary>
        /// Reason or description of what's limiting the SCCR.
        /// </summary>
        public string LimitingReason { get; set; } = "";

        /// <summary>
        /// List of audit trail entries explaining the calculation.
        /// </summary>
        public List<CalculationLogEntry> LogEntries { get; set; } = new List<CalculationLogEntry>();

        /// <summary>
        /// Warnings about missing or questionable data.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"Overall SCCR: {OverallSccr} kA (Limited by: {LimitingReason})";
        }
    }

    /// <summary>
    /// An entry in the calculation audit trail/log.
    /// </summary>
    public class CalculationLogEntry
    {
        public string NodeName { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public double? ComponentSccr { get; set; }
        public double? OcpdRating { get; set; }
        public double? ResultingSccr { get; set; }
        public string Notes { get; set; } = "";

        public override string ToString()
        {
            var components = new List<string>();
            if (ComponentSccr.HasValue)
                components.Add($"Component SCCR: {ComponentSccr} kA");
            if (OcpdRating.HasValue)
                components.Add($"OCPD Rating: {OcpdRating} kA");
            if (ResultingSccr.HasValue)
                components.Add($"Resulting: {ResultingSccr} kA");

            return $"{NodeName} ({DeviceType}) - {string.Join(", ", components)} - {Notes}";
        }
    }
}

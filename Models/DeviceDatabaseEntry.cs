using System.Text.Json.Serialization;

namespace SccrWpfApp.Models
{
    /// <summary>
    /// A reusable device library record loaded from the editable JSON database.
    /// </summary>
    public class DeviceDatabaseEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = "";

        [JsonPropertyName("partNumber")]
        public string PartNumber { get; set; } = "";

        [JsonPropertyName("internalPartNumber")]
        public string InternalPartNumber { get; set; } = "";

        [JsonPropertyName("deviceType")]
        public string DeviceType { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("imagePath")]
        public string ImagePath { get; set; } = "";

        [JsonPropertyName("voltage")]
        public double? Voltage { get; set; } = 480;

        [JsonPropertyName("sccrRating")]
        public double? SccrRating { get; set; }

        [JsonPropertyName("interruptingRating")]
        public double? InterruptingRating { get; set; }

        [JsonPropertyName("ocpdAmps")]
        public double? OcpdAmps { get; set; }

        [JsonPropertyName("inputCurrentAmps")]
        public double? InputCurrentAmps { get; set; }

        [JsonPropertyName("isFusedDisconnect")]
        public bool IsFusedDisconnect { get; set; }

        [JsonPropertyName("fuseManufacturer")]
        public string FuseManufacturer { get; set; } = "";

        [JsonPropertyName("fusePartNumber")]
        public string FusePartNumber { get; set; } = "";

        [JsonPropertyName("fuseInternalPartNumber")]
        public string FuseInternalPartNumber { get; set; } = "";

        [JsonPropertyName("fuseClass")]
        public string FuseClass { get; set; } = "";

        [JsonPropertyName("fuseAmps")]
        public double? FuseAmps { get; set; }

        [JsonPropertyName("letThroughCurrent")]
        public double? LetThroughCurrent { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = "";

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        [JsonPropertyName("exemptFromSccr")]
        public bool ExemptFromSccr { get; set; }

        [JsonPropertyName("exemptReason")]
        public string ExemptReason { get; set; } = "";

        public Device ToDevice()
        {
            return new Device
            {
                Manufacturer = Manufacturer,
                PartNumber = PartNumber,
                InternalPartNumber = InternalPartNumber,
                Description = Description,
                ImagePath = ImagePath,
                Voltage = Voltage,
                SccrRating = SccrRating,
                InterruptingRating = InterruptingRating,
                OcpdAmps = OcpdAmps,
                InputCurrentAmps = InputCurrentAmps,
                IsFusedDisconnect = IsFusedDisconnect,
                FuseManufacturer = FuseManufacturer,
                FusePartNumber = FusePartNumber,
                FuseInternalPartNumber = FuseInternalPartNumber,
                FuseClass = FuseClass,
                FuseAmps = FuseAmps,
                LetThroughCurrent = LetThroughCurrent,
                Source = Source,
                Notes = Notes,
                ExemptFromSccr = ExemptFromSccr,
                ExemptReason = ExemptReason
            };
        }

        public static DeviceDatabaseEntry FromDevice(Device device, string deviceType)
        {
            return new DeviceDatabaseEntry
            {
                Id = CreateId(device.Manufacturer, device.PartNumber),
                Manufacturer = device.Manufacturer,
                PartNumber = device.PartNumber,
                InternalPartNumber = device.InternalPartNumber,
                DeviceType = deviceType,
                Description = device.Description,
                ImagePath = device.ImagePath,
                Voltage = device.Voltage,
                SccrRating = device.SccrRating,
                InterruptingRating = device.InterruptingRating,
                OcpdAmps = device.OcpdAmps,
                InputCurrentAmps = device.InputCurrentAmps,
                IsFusedDisconnect = device.IsFusedDisconnect,
                FuseManufacturer = device.FuseManufacturer,
                FusePartNumber = device.FusePartNumber,
                FuseInternalPartNumber = device.FuseInternalPartNumber,
                FuseClass = device.FuseClass,
                FuseAmps = device.FuseAmps,
                LetThroughCurrent = device.LetThroughCurrent,
                Source = device.Source,
                Notes = device.Notes,
                ExemptFromSccr = device.ExemptFromSccr,
                ExemptReason = device.ExemptReason
            };
        }

        public DeviceDatabaseEntry Clone()
        {
            return new DeviceDatabaseEntry
            {
                Id = Id,
                Manufacturer = Manufacturer,
                PartNumber = PartNumber,
                InternalPartNumber = InternalPartNumber,
                DeviceType = DeviceType,
                Description = Description,
                ImagePath = ImagePath,
                Voltage = Voltage,
                SccrRating = SccrRating,
                InterruptingRating = InterruptingRating,
                OcpdAmps = OcpdAmps,
                InputCurrentAmps = InputCurrentAmps,
                IsFusedDisconnect = IsFusedDisconnect,
                FuseManufacturer = FuseManufacturer,
                FusePartNumber = FusePartNumber,
                FuseInternalPartNumber = FuseInternalPartNumber,
                FuseClass = FuseClass,
                FuseAmps = FuseAmps,
                LetThroughCurrent = LetThroughCurrent,
                Source = Source,
                Notes = Notes,
                ExemptFromSccr = ExemptFromSccr,
                ExemptReason = ExemptReason
            };
        }

        public static string CreateId(string manufacturer, string partNumber)
        {
            var raw = $"{manufacturer}-{partNumber}".Trim('-');
            if (string.IsNullOrWhiteSpace(raw))
                raw = Guid.NewGuid().ToString("N");

            var chars = raw
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray();

            return string.Join("-", new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}

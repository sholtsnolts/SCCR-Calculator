using System.ComponentModel;

namespace SccrWpfApp.Models
{
    /// <summary>
    /// Represents a component/device with its electrical characteristics.
    /// </summary>
    public class Device : INotifyPropertyChanged
    {
        private string _manufacturer = "";
        private string _partNumber = "";
        private string _internalPartNumber = "";
        private string _description = "";
        private string _imagePath = "";
        private double? _voltage = 480;
        private double? _sccrRating; // Short Circuit Current Rating in kA
        private double? _interruptingRating; // For OCPDs, in kA
        private double? _ocpdAmps; // Ampere rating used for conductor sizing
        private double? _inputCurrentAmps; // Input current for power conversion equipment
        private bool _isFusedDisconnect = false;
        private string _fuseManufacturer = "";
        private string _fusePartNumber = "";
        private string _fuseInternalPartNumber = "";
        private string _fuseClass = ""; // Class J, CC, RK1, etc. if applicable
        private double? _fuseAmps; // Fuse amp rating if applicable
        private double? _letThroughCurrent; // Peak let-through current in kA
        private string _notes = "";
        private string _source = ""; // datasheet, UL table, marking, etc.
        private bool _exemptFromSccr = false;
        private string _exemptReason = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public Device()
        {
        }

        public string Manufacturer
        {
            get => _manufacturer;
            set { _manufacturer = value; OnPropertyChanged(nameof(Manufacturer)); }
        }

        public string PartNumber
        {
            get => _partNumber;
            set { _partNumber = value; OnPropertyChanged(nameof(PartNumber)); }
        }

        public string InternalPartNumber
        {
            get => _internalPartNumber;
            set { _internalPartNumber = value; OnPropertyChanged(nameof(InternalPartNumber)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
        }

        public double? Voltage
        {
            get => _voltage;
            set { _voltage = value; OnPropertyChanged(nameof(Voltage)); }
        }

        public double? SccrRating
        {
            get => _sccrRating;
            set { _sccrRating = value; OnPropertyChanged(nameof(SccrRating)); }
        }

        public double? InterruptingRating
        {
            get => _interruptingRating;
            set { _interruptingRating = value; OnPropertyChanged(nameof(InterruptingRating)); }
        }

        public double? OcpdAmps
        {
            get => _ocpdAmps;
            set { _ocpdAmps = value; OnPropertyChanged(nameof(OcpdAmps)); }
        }

        public double? InputCurrentAmps
        {
            get => _inputCurrentAmps;
            set { _inputCurrentAmps = value; OnPropertyChanged(nameof(InputCurrentAmps)); }
        }

        public bool IsFusedDisconnect
        {
            get => _isFusedDisconnect;
            set { _isFusedDisconnect = value; OnPropertyChanged(nameof(IsFusedDisconnect)); }
        }

        public string FuseManufacturer
        {
            get => _fuseManufacturer;
            set { _fuseManufacturer = value; OnPropertyChanged(nameof(FuseManufacturer)); }
        }

        public string FusePartNumber
        {
            get => _fusePartNumber;
            set { _fusePartNumber = value; OnPropertyChanged(nameof(FusePartNumber)); }
        }

        public string FuseInternalPartNumber
        {
            get => _fuseInternalPartNumber;
            set { _fuseInternalPartNumber = value; OnPropertyChanged(nameof(FuseInternalPartNumber)); }
        }

        public string FuseClass
        {
            get => _fuseClass;
            set { _fuseClass = value; OnPropertyChanged(nameof(FuseClass)); }
        }

        public double? FuseAmps
        {
            get => _fuseAmps;
            set { _fuseAmps = value; OnPropertyChanged(nameof(FuseAmps)); }
        }

        public double? LetThroughCurrent
        {
            get => _letThroughCurrent;
            set { _letThroughCurrent = value; OnPropertyChanged(nameof(LetThroughCurrent)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public string Source
        {
            get => _source;
            set { _source = value; OnPropertyChanged(nameof(Source)); }
        }

        public bool ExemptFromSccr
        {
            get => _exemptFromSccr;
            set { _exemptFromSccr = value; OnPropertyChanged(nameof(ExemptFromSccr)); }
        }

        public string ExemptReason
        {
            get => _exemptReason;
            set { _exemptReason = value; OnPropertyChanged(nameof(ExemptReason)); }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{Manufacturer} {PartNumber} ({Description})";
        }
    }
}

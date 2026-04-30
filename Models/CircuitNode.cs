using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SccrWpfApp.Models
{
    /// <summary>
    /// Represents a node in the circuit tree (feeder, branch, device, etc.)
    /// </summary>
    public class CircuitNode : INotifyPropertyChanged
    {
        private string _name = "";
        private string _deviceType = ""; // feeder, branch, distribution-block, fuse-block, drive, load, etc.
        private Device? _device;
        private ObservableCollection<CircuitNode> _children;
        private CircuitNode? _parent;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CircuitNode(string name = "New Node", string deviceType = "")
        {
            _name = name;
            _deviceType = deviceType;
            _children = new ObservableCollection<CircuitNode>();
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string DeviceType
        {
            get => _deviceType;
            set { _deviceType = value; OnPropertyChanged(nameof(DeviceType)); }
        }

        public Device? Device
        {
            get => _device;
            set { _device = value; OnPropertyChanged(nameof(Device)); }
        }

        [JsonIgnore]
        public CircuitNode? Parent
        {
            get => _parent;
            set { _parent = value; }
        }

        public ObservableCollection<CircuitNode> Children
        {
            get => _children;
            set { _children = value ?? new ObservableCollection<CircuitNode>(); OnPropertyChanged(nameof(Children)); }
        }

        public void AddChild(CircuitNode child)
        {
            child.Parent = this;
            _children.Add(child);
        }

        public void RemoveChild(CircuitNode child)
        {
            _children.Remove(child);
            child.Parent = null;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

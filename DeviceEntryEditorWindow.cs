using System.Globalization;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using SccrWpfApp.Models;

namespace SccrWpfApp
{
    public class DeviceEntryEditorWindow : Window
    {
        private readonly TextBox _manufacturerTextBox = new();
        private readonly TextBox _partNumberTextBox = new();
        private readonly TextBox _internalPartNumberTextBox = new();
        private readonly ComboBox _deviceTypeComboBox = new();
        private readonly TextBox _customDeviceTypeTextBox = new();
        private readonly TextBox _descriptionTextBox = new();
        private readonly TextBox _imagePathTextBox = new();
        private readonly TextBox _voltageTextBox = new();
        private readonly TextBox _sccrTextBox = new();
        private readonly TextBox _interruptingTextBox = new();
        private readonly TextBox _ocpdAmpsTextBox = new();
        private readonly TextBox _inputCurrentAmpsTextBox = new();
        private readonly TextBox _fuseManufacturerTextBox = new();
        private readonly TextBox _fusePartNumberTextBox = new();
        private readonly TextBox _fuseInternalPartNumberTextBox = new();
        private readonly TextBox _fuseClassTextBox = new();
        private readonly TextBox _fuseAmpsTextBox = new();
        private readonly TextBox _letThroughTextBox = new();
        private readonly TextBox _sourceTextBox = new();
        private readonly TextBox _notesTextBox = new();
        private readonly CheckBox _exemptCheckBox = new();
        private readonly TextBox _exemptReasonTextBox = new();

        public DeviceDatabaseEntry Entry { get; private set; }

        public DeviceEntryEditorWindow(DeviceDatabaseEntry entry)
        {
            Entry = entry.Clone();
            Title = string.IsNullOrWhiteSpace(Entry.PartNumber) ? "Add Device" : "Edit Device";
            Width = 620;
            Height = 720;
            MinWidth = 560;
            MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Content = BuildLayout();
            LoadEntry();
        }

        private UIElement BuildLayout()
        {
            var root = new DockPanel { Margin = new Thickness(12) };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var okButton = new Button { Content = "OK", Width = 90, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            okButton.Click += (_, _) => SaveAndClose();

            var cancelButton = new Button { Content = "Cancel", Width = 90, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            cancelButton.Click += (_, _) => DialogResult = false;

            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            DockPanel.SetDock(buttons, Dock.Bottom);
            root.Children.Add(buttons);

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel();

            AddTextField(panel, "Manufacturer:", _manufacturerTextBox);
            AddTextField(panel, "Manufacturer Part Number:", _partNumberTextBox);
            AddTextField(panel, "Internal Part Number (IPN):", _internalPartNumberTextBox);
            AddDeviceTypeField(panel);
            AddTextField(panel, "Description:", _descriptionTextBox);
            AddImageField(panel);
            AddTextField(panel, "Voltage (V):", _voltageTextBox);
            AddTextField(panel, "SCCR Rating (kA):", _sccrTextBox);
            AddTextField(panel, "OCPD Interrupting Rating (kA):", _interruptingTextBox);
            AddTextField(panel, "OCPD Amp Rating:", _ocpdAmpsTextBox);
            AddTextField(panel, "Input Current (A):", _inputCurrentAmpsTextBox);
            AddTextField(panel, "Fuse Manufacturer:", _fuseManufacturerTextBox);
            AddTextField(panel, "Fuse Part Number:", _fusePartNumberTextBox);
            AddTextField(panel, "Fuse IPN:", _fuseInternalPartNumberTextBox);
            AddTextField(panel, "Fuse Class:", _fuseClassTextBox);
            AddTextField(panel, "Fuse Rating (A):", _fuseAmpsTextBox);
            AddTextField(panel, "Let-Through Current (kA):", _letThroughTextBox);
            AddTextField(panel, "Source:", _sourceTextBox);
            AddTextField(panel, "Notes:", _notesTextBox, acceptsReturn: true);

            _exemptCheckBox.Content = "Exempt from SCCR";
            _exemptCheckBox.Margin = new Thickness(160, 8, 0, 0);
            panel.Children.Add(_exemptCheckBox);
            AddTextField(panel, "Exempt Reason:", _exemptReasonTextBox);

            scrollViewer.Content = panel;
            root.Children.Add(scrollViewer);
            return root;
        }

        private static void AddTextField(StackPanel panel, string label, TextBox textBox, bool acceptsReturn = false)
        {
            var grid = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            textBox.Padding = new Thickness(5);
            textBox.AcceptsReturn = acceptsReturn;
            textBox.Height = acceptsReturn ? 72 : double.NaN;
            textBox.TextWrapping = acceptsReturn ? TextWrapping.Wrap : TextWrapping.NoWrap;
            textBox.VerticalScrollBarVisibility = acceptsReturn ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(textBox, 1);
            grid.Children.Add(labelBlock);
            grid.Children.Add(textBox);
            panel.Children.Add(grid);
        }

        private void AddImageField(StackPanel panel)
        {
            var grid = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });

            var labelBlock = new TextBlock { Text = "Image Path:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            _imagePathTextBox.Padding = new Thickness(5);

            var browseButton = new Button { Content = "Browse", Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            browseButton.Click += (_, _) =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Device Image",
                    Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files (*.*)|*.*"
                };

                if (dialog.ShowDialog(this) == true)
                {
                    _imagePathTextBox.Text = dialog.FileName;
                }
            };

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(_imagePathTextBox, 1);
            Grid.SetColumn(browseButton, 2);
            grid.Children.Add(labelBlock);
            grid.Children.Add(_imagePathTextBox);
            grid.Children.Add(browseButton);
            panel.Children.Add(grid);
        }

        private void AddDeviceTypeField(StackPanel panel)
        {
            var grid = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = "Type of Device:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var selectorPanel = new StackPanel();
            _deviceTypeComboBox.ItemsSource = DeviceTypeCatalog.KnownTypes;
            _deviceTypeComboBox.Padding = new Thickness(5);
            _deviceTypeComboBox.SelectionChanged += (_, _) =>
            {
                _customDeviceTypeTextBox.Visibility = _deviceTypeComboBox.SelectedItem?.ToString() == "other"
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };

            _customDeviceTypeTextBox.Padding = new Thickness(5);
            _customDeviceTypeTextBox.Margin = new Thickness(0, 5, 0, 0);

            selectorPanel.Children.Add(_deviceTypeComboBox);
            selectorPanel.Children.Add(_customDeviceTypeTextBox);

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(selectorPanel, 1);
            grid.Children.Add(labelBlock);
            grid.Children.Add(selectorPanel);
            panel.Children.Add(grid);
        }

        private void LoadEntry()
        {
            _manufacturerTextBox.Text = Entry.Manufacturer;
            _partNumberTextBox.Text = Entry.PartNumber;
            _internalPartNumberTextBox.Text = Entry.InternalPartNumber;
            Entry.DeviceType = NormalizeDeviceType(Entry.DeviceType);
            var knownType = DeviceTypeCatalog.KnownTypes.FirstOrDefault(type =>
                type.Equals(Entry.DeviceType, StringComparison.OrdinalIgnoreCase));
            _deviceTypeComboBox.SelectedItem = knownType ?? "other";
            _customDeviceTypeTextBox.Text = knownType == null ? Entry.DeviceType : "";
            _customDeviceTypeTextBox.Visibility = knownType == null || knownType == "other"
                ? Visibility.Visible
                : Visibility.Collapsed;
            _descriptionTextBox.Text = Entry.Description;
            _imagePathTextBox.Text = Entry.ImagePath;
            _voltageTextBox.Text = FormatNumber(Entry.Voltage);
            _sccrTextBox.Text = FormatNumber(Entry.SccrRating);
            _interruptingTextBox.Text = FormatNumber(Entry.InterruptingRating);
            _ocpdAmpsTextBox.Text = FormatNumber(Entry.OcpdAmps);
            _inputCurrentAmpsTextBox.Text = FormatNumber(Entry.InputCurrentAmps);
            _fuseManufacturerTextBox.Text = Entry.FuseManufacturer;
            _fusePartNumberTextBox.Text = Entry.FusePartNumber;
            _fuseInternalPartNumberTextBox.Text = Entry.FuseInternalPartNumber;
            _fuseClassTextBox.Text = Entry.FuseClass;
            _fuseAmpsTextBox.Text = FormatNumber(Entry.FuseAmps);
            _letThroughTextBox.Text = FormatNumber(Entry.LetThroughCurrent);
            _sourceTextBox.Text = Entry.Source;
            _notesTextBox.Text = Entry.Notes;
            _exemptCheckBox.IsChecked = Entry.ExemptFromSccr;
            _exemptReasonTextBox.Text = Entry.ExemptReason;
        }

        private void SaveAndClose()
        {
            if (string.IsNullOrWhiteSpace(_manufacturerTextBox.Text) || string.IsNullOrWhiteSpace(_partNumberTextBox.Text))
            {
                MessageBox.Show(this, "Manufacturer and manufacturer part number are required.", "Device Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Entry.Manufacturer = _manufacturerTextBox.Text.Trim();
            Entry.PartNumber = _partNumberTextBox.Text.Trim();
            Entry.InternalPartNumber = _internalPartNumberTextBox.Text.Trim();
            Entry.DeviceType = NormalizeDeviceType(_deviceTypeComboBox.SelectedItem?.ToString() == "other"
                ? _customDeviceTypeTextBox.Text.Trim()
                : _deviceTypeComboBox.SelectedItem?.ToString() ?? "");
            Entry.Description = _descriptionTextBox.Text.Trim();
            Entry.ImagePath = _imagePathTextBox.Text.Trim();
            Entry.Voltage = ParseNullableDouble(_voltageTextBox.Text);
            Entry.SccrRating = ParseNullableDouble(_sccrTextBox.Text);
            Entry.InterruptingRating = ParseNullableDouble(_interruptingTextBox.Text);
            Entry.OcpdAmps = ParseNullableDouble(_ocpdAmpsTextBox.Text);
            Entry.InputCurrentAmps = ParseNullableDouble(_inputCurrentAmpsTextBox.Text);
            Entry.FuseManufacturer = _fuseManufacturerTextBox.Text.Trim();
            Entry.FusePartNumber = _fusePartNumberTextBox.Text.Trim();
            Entry.FuseInternalPartNumber = _fuseInternalPartNumberTextBox.Text.Trim();
            Entry.FuseClass = _fuseClassTextBox.Text.Trim();
            Entry.FuseAmps = ParseNullableDouble(_fuseAmpsTextBox.Text);
            Entry.LetThroughCurrent = ParseNullableDouble(_letThroughTextBox.Text);
            Entry.Source = _sourceTextBox.Text.Trim();
            Entry.Notes = _notesTextBox.Text.Trim();
            Entry.ExemptFromSccr = _exemptCheckBox.IsChecked == true;
            Entry.ExemptReason = _exemptReasonTextBox.Text.Trim();
            Entry.Id = DeviceDatabaseEntry.CreateId(Entry.Manufacturer, Entry.PartNumber);

            DialogResult = true;
        }

        private static string FormatNumber(double? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture) ?? "";
        }

        private static double? ParseNullableDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : null;
        }

        private static string NormalizeDeviceType(string? deviceType)
        {
            var value = (deviceType ?? "").Trim();
            return value.Equals("fuse", StringComparison.OrdinalIgnoreCase)
                || value.Equals("fuse-block", StringComparison.OrdinalIgnoreCase)
                || value.Equals("fuse block", StringComparison.OrdinalIgnoreCase)
                || value.Equals("fuse + fuse block", StringComparison.OrdinalIgnoreCase)
                ? "fuse + fuse-block"
                : value;
        }
    }
}

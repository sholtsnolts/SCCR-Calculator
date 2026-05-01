using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SccrWpfApp.Models;
using SccrWpfApp.Services;

namespace SccrWpfApp
{
    public class DeviceDatabaseWindow : Window
    {
        private readonly DeviceDatabaseService _deviceDatabaseService;
        private readonly ObservableCollection<DeviceDatabaseEntry> _devices;
        private readonly bool _allowInsert;
        private readonly bool _selectionOnly;
        private readonly Func<DeviceDatabaseEntry, bool>? _filter;
        private readonly DataGrid _deviceGrid = new();
        private readonly Image _previewImage = new();
        private readonly TextBlock _previewText = new();
        private readonly TextBlock _databasePathBlock = new();

        public DeviceDatabaseEntry? SelectedEntry { get; private set; }

        public DeviceDatabaseWindow(DeviceDatabaseService deviceDatabaseService, bool allowInsert)
            : this(deviceDatabaseService, allowInsert, null, false)
        {
        }

        public DeviceDatabaseWindow(
            DeviceDatabaseService deviceDatabaseService,
            bool allowInsert,
            Func<DeviceDatabaseEntry, bool>? filter,
            bool selectionOnly)
        {
            _deviceDatabaseService = deviceDatabaseService;
            _allowInsert = allowInsert;
            _filter = filter;
            _selectionOnly = selectionOnly;
            _devices = LoadFilteredDevices();

            Title = allowInsert ? "Insert Device from Database" : "Device Database";
            Width = 1180;
            Height = 720;
            MinWidth = 920;
            MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Content = BuildLayout();
        }

        private UIElement BuildLayout()
        {
            var root = new DockPanel { Margin = new Thickness(10) };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var insertButton = new Button
            {
                Content = "Insert Selected",
                Width = 115,
                Padding = new Thickness(5),
                Margin = new Thickness(5, 0, 0, 0),
                IsEnabled = _allowInsert
            };
            insertButton.Click += (_, _) => InsertSelected();

            var addButton = new Button { Content = "Add New", Width = 90, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            addButton.Visibility = _selectionOnly ? Visibility.Collapsed : Visibility.Visible;
            addButton.Click += (_, _) => AddNewDevice();

            var editButton = new Button { Content = "Edit Selected", Width = 105, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            editButton.Visibility = _selectionOnly ? Visibility.Collapsed : Visibility.Visible;
            editButton.Click += (_, _) => EditSelectedDevice();

            var deleteButton = new Button { Content = "Delete", Width = 80, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            deleteButton.Visibility = _selectionOnly ? Visibility.Collapsed : Visibility.Visible;
            deleteButton.Click += (_, _) => DeleteSelectedDevice();

            var saveButton = new Button { Content = "Save Database", Width = 115, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            saveButton.Visibility = _selectionOnly ? Visibility.Collapsed : Visibility.Visible;
            saveButton.Click += (_, _) => SaveDatabase();

            var importButton = new Button { Content = "Import", Width = 80, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            importButton.Visibility = _selectionOnly ? Visibility.Collapsed : Visibility.Visible;
            importButton.Click += (_, _) => ImportDatabase();

            var exportButton = new Button { Content = "Export", Width = 80, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            exportButton.Visibility = _selectionOnly ? Visibility.Collapsed : Visibility.Visible;
            exportButton.Click += (_, _) => ExportDatabase();

            var importCsvButton = new Button { Content = "Import CSV", Width = 95, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            importCsvButton.Visibility = _selectionOnly ? Visibility.Collapsed : Visibility.Visible;
            importCsvButton.Click += (_, _) => ImportDatabaseCsv();

            var exportCsvButton = new Button { Content = "Export CSV", Width = 95, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            exportCsvButton.Visibility = _selectionOnly ? Visibility.Collapsed : Visibility.Visible;
            exportCsvButton.Click += (_, _) => ExportDatabaseCsv();

            var pathButton = new Button { Content = "Set Path", Width = 85, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            pathButton.Visibility = _selectionOnly ? Visibility.Collapsed : Visibility.Visible;
            pathButton.Click += (_, _) => SetDatabasePath();

            var closeButton = new Button { Content = "Close", Width = 80, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };
            closeButton.Click += (_, _) => DialogResult = false;

            buttonPanel.Children.Add(insertButton);
            buttonPanel.Children.Add(addButton);
            buttonPanel.Children.Add(editButton);
            buttonPanel.Children.Add(deleteButton);
            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(importButton);
            buttonPanel.Children.Add(exportButton);
            buttonPanel.Children.Add(importCsvButton);
            buttonPanel.Children.Add(exportCsvButton);
            buttonPanel.Children.Add(pathButton);
            buttonPanel.Children.Add(closeButton);
            DockPanel.SetDock(buttonPanel, Dock.Bottom);
            root.Children.Add(buttonPanel);

            _databasePathBlock.Margin = new Thickness(0, 0, 0, 8);
            _databasePathBlock.TextWrapping = TextWrapping.Wrap;
            RefreshDatabasePathDisplay();
            DockPanel.SetDock(_databasePathBlock, Dock.Top);
            root.Children.Add(_databasePathBlock);

            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });

            ConfigureDeviceGrid();
            Grid.SetColumn(_deviceGrid, 0);
            contentGrid.Children.Add(_deviceGrid);

            var previewPanel = BuildPreviewPanel();
            Grid.SetColumn(previewPanel, 1);
            contentGrid.Children.Add(previewPanel);

            root.Children.Add(contentGrid);
            return root;
        }

        private void ConfigureDeviceGrid()
        {
            _deviceGrid.ItemsSource = _devices;
            _deviceGrid.AutoGenerateColumns = false;
            _deviceGrid.CanUserSortColumns = true;
            _deviceGrid.IsReadOnly = true;
            _deviceGrid.SelectionMode = DataGridSelectionMode.Single;
            _deviceGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
            _deviceGrid.Margin = new Thickness(0, 0, 10, 0);
            _deviceGrid.MouseDoubleClick += (_, _) =>
            {
                if (_allowInsert)
                    InsertSelected();
                else
                    EditSelectedDevice();
            };
            _deviceGrid.SelectionChanged += (_, _) => RefreshPreview();

            AddColumn("Manufacturer", nameof(DeviceDatabaseEntry.Manufacturer), 130);
            AddColumn("Part Number", nameof(DeviceDatabaseEntry.PartNumber), 145);
            AddColumn("IPN", nameof(DeviceDatabaseEntry.InternalPartNumber), 110);
            AddColumn("Device Type", nameof(DeviceDatabaseEntry.DeviceType), 145);
            AddColumn("Description", nameof(DeviceDatabaseEntry.Description), 210);
            AddColumn("Image", nameof(DeviceDatabaseEntry.ImagePath), 140);
            AddColumn("Voltage", nameof(DeviceDatabaseEntry.Voltage), 80);
            AddColumn("SCCR", nameof(DeviceDatabaseEntry.SccrRating), 75);
            AddColumn("IR", nameof(DeviceDatabaseEntry.InterruptingRating), 75);
            AddColumn("OCPD A", nameof(DeviceDatabaseEntry.OcpdAmps), 80);
            AddColumn("Input A", nameof(DeviceDatabaseEntry.InputCurrentAmps), 80);
            AddColumn("Fused Disc.", nameof(DeviceDatabaseEntry.IsFusedDisconnect), 85);
            AddColumn("Fuse Mfg", nameof(DeviceDatabaseEntry.FuseManufacturer), 110);
            AddColumn("Fuse Part", nameof(DeviceDatabaseEntry.FusePartNumber), 120);
            AddColumn("Fuse IPN", nameof(DeviceDatabaseEntry.FuseInternalPartNumber), 100);
            AddColumn("Fuse Class", nameof(DeviceDatabaseEntry.FuseClass), 90);
            AddColumn("Fuse A", nameof(DeviceDatabaseEntry.FuseAmps), 80);
            AddColumn("Let-Through", nameof(DeviceDatabaseEntry.LetThroughCurrent), 100);
            AddColumn("Source", nameof(DeviceDatabaseEntry.Source), 160);

            if (_devices.Count > 0)
                _deviceGrid.SelectedIndex = 0;
        }

        private ObservableCollection<DeviceDatabaseEntry> LoadFilteredDevices()
        {
            var devices = _deviceDatabaseService.LoadDevices();
            if (_filter == null)
                return devices;

            return new ObservableCollection<DeviceDatabaseEntry>(devices.Where(_filter));
        }

        private void AddColumn(string header, string bindingPath, double width)
        {
            _deviceGrid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(bindingPath),
                SortMemberPath = bindingPath,
                Width = width
            });
        }

        private UIElement BuildPreviewPanel()
        {
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = "Image Preview",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            _previewImage.Width = 200;
            _previewImage.Height = 160;
            _previewImage.Stretch = System.Windows.Media.Stretch.Uniform;
            panel.Children.Add(_previewImage);

            _previewText.TextWrapping = TextWrapping.Wrap;
            _previewText.Margin = new Thickness(0, 8, 0, 0);
            panel.Children.Add(_previewText);

            return panel;
        }

        private void AddNewDevice()
        {
            var editor = new DeviceEntryEditorWindow(new DeviceDatabaseEntry()) { Owner = this };
            if (editor.ShowDialog() == true)
            {
                _devices.Add(editor.Entry);
                SaveDatabase(showMessage: false);
                _deviceGrid.Items.Refresh();
                _deviceGrid.SelectedItem = editor.Entry;
            }
        }

        private void EditSelectedDevice()
        {
            if (_deviceGrid.SelectedItem is not DeviceDatabaseEntry selected)
                return;

            var editor = new DeviceEntryEditorWindow(selected) { Owner = this };
            if (editor.ShowDialog() == true)
            {
                CopyEntry(editor.Entry, selected);
                SaveDatabase(showMessage: false);
                _deviceGrid.Items.Refresh();
                RefreshPreview();
            }
        }

        private void DeleteSelectedDevice()
        {
            if (_deviceGrid.SelectedItem is not DeviceDatabaseEntry selected)
                return;

            var response = MessageBox.Show(
                this,
                $"Delete {selected.Manufacturer} {selected.PartNumber} from the device database?",
                "Delete Device",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (response != MessageBoxResult.Yes)
                return;

            _devices.Remove(selected);
            SaveDatabase(showMessage: false);
        }

        private void InsertSelected()
        {
            if (_deviceGrid.SelectedItem is not DeviceDatabaseEntry selected)
            {
                MessageBox.Show(this, "Select a device to insert.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedEntry = selected.Clone();
            DialogResult = true;
        }

        private void SaveDatabase(bool showMessage = true)
        {
            try
            {
                _deviceDatabaseService.SaveDevices(_devices);
                if (showMessage)
                {
                    MessageBox.Show(this, "Device database saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to save device database: {ex.Message}", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportDatabase()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Device Database",
                Filter = "Device Database (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            var response = MessageBox.Show(
                this,
                "Importing will replace the currently selected device database. Continue?",
                "Import Device Database",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (response != MessageBoxResult.Yes)
                return;

            try
            {
                _deviceDatabaseService.ImportDatabase(dialog.FileName);
                ReloadDevices();
                MessageBox.Show(this, "Device database imported.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to import device database: {ex.Message}", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDatabase()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export Device Database",
                Filter = "Device Database (*.json)|*.json|All Files (*.*)|*.*",
                FileName = "DeviceDatabase.json"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                _deviceDatabaseService.ExportDatabase(dialog.FileName);
                MessageBox.Show(this, "Device database exported.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to export device database: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetDatabasePath()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Select Device Database Path",
                Filter = "Device Database (*.json)|*.json|All Files (*.*)|*.*",
                FileName = Path.GetFileName(_deviceDatabaseService.DatabasePath),
                InitialDirectory = Path.GetDirectoryName(_deviceDatabaseService.DatabasePath)
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                _deviceDatabaseService.SetDatabasePath(dialog.FileName);
                RefreshDatabasePathDisplay();
                ReloadDevices();
                MessageBox.Show(this, "Device database path updated.", "Database Path", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to set database path: {ex.Message}", "Path Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportDatabaseCsv()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Device Database CSV",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            var response = MessageBox.Show(
                this,
                "Importing CSV will replace the currently selected device database. Continue?",
                "Import Device Database CSV",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (response != MessageBoxResult.Yes)
                return;

            try
            {
                _deviceDatabaseService.ImportDatabaseCsv(dialog.FileName);
                ReloadDevices();
                MessageBox.Show(this, "Device database CSV imported.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to import CSV: {ex.Message}", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDatabaseCsv()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export Device Database CSV",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = "DeviceDatabase.csv"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                _deviceDatabaseService.ExportDatabaseCsv(dialog.FileName);
                MessageBox.Show(this, "Device database CSV exported.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to export CSV: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReloadDevices()
        {
            _devices.Clear();
            foreach (var device in LoadFilteredDevices())
            {
                _devices.Add(device);
            }

            _deviceGrid.Items.Refresh();
            if (_devices.Count > 0)
                _deviceGrid.SelectedIndex = 0;

            RefreshPreview();
        }

        private void RefreshDatabasePathDisplay()
        {
            _databasePathBlock.Text = $"Database: {_deviceDatabaseService.DatabasePath}";
        }

        private void RefreshPreview()
        {
            _previewImage.Source = null;

            if (_deviceGrid.SelectedItem is not DeviceDatabaseEntry selected)
            {
                _previewText.Text = "";
                return;
            }

            _previewText.Text = string.IsNullOrWhiteSpace(selected.ImagePath)
                ? "No image path set."
                : selected.ImagePath;

            if (string.IsNullOrWhiteSpace(selected.ImagePath) || !File.Exists(selected.ImagePath))
                return;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(selected.ImagePath);
                image.EndInit();
                _previewImage.Source = image;
            }
            catch
            {
                _previewText.Text = $"Unable to load image: {selected.ImagePath}";
            }
        }

        private static void CopyEntry(DeviceDatabaseEntry source, DeviceDatabaseEntry target)
        {
            target.Id = source.Id;
            target.Manufacturer = source.Manufacturer;
            target.PartNumber = source.PartNumber;
            target.InternalPartNumber = source.InternalPartNumber;
            target.DeviceType = source.DeviceType;
            target.Description = source.Description;
            target.ImagePath = source.ImagePath;
            target.Voltage = source.Voltage;
            target.SccrRating = source.SccrRating;
            target.InterruptingRating = source.InterruptingRating;
            target.OcpdAmps = source.OcpdAmps;
            target.InputCurrentAmps = source.InputCurrentAmps;
            target.IsFusedDisconnect = source.IsFusedDisconnect;
            target.FuseManufacturer = source.FuseManufacturer;
            target.FusePartNumber = source.FusePartNumber;
            target.FuseInternalPartNumber = source.FuseInternalPartNumber;
            target.FuseClass = source.FuseClass;
            target.FuseAmps = source.FuseAmps;
            target.LetThroughCurrent = source.LetThroughCurrent;
            target.Source = source.Source;
            target.Notes = source.Notes;
            target.ExemptFromSccr = source.ExemptFromSccr;
            target.ExemptReason = source.ExemptReason;
        }
    }
}

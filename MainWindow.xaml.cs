using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SccrWpfApp.Models;
using SccrWpfApp.Services;

namespace SccrWpfApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private CircuitNode _rootNode;
    private SccrCalculator _calculator;
    private DefaultSccrLookup _defaultSccrLookup;
    private FuseLetThroughLookup _fuseLetThroughLookup;
    private DeviceDatabaseService _deviceDatabaseService;
    private AmpacityLookupService _ampacityLookupService;
    private CircuitNode? _selectedNode;
    private ObservableCollection<string> _calculationLog;
    private string? _currentProjectPath;

    public MainWindow()
    {
        InitializeComponent();
        
        _calculator = new SccrCalculator();
        _defaultSccrLookup = new DefaultSccrLookup();
        _fuseLetThroughLookup = new FuseLetThroughLookup();
        _deviceDatabaseService = new DeviceDatabaseService();
        _ampacityLookupService = new AmpacityLookupService();
        _calculationLog = new ObservableCollection<string>();
        LogListBox.ItemsSource = _calculationLog;

        // Initialize root node with a default panel/feeder
        _rootNode = new CircuitNode("Main Panel", "feeder")
        {
            Device = new Device
            {
                Manufacturer = "System",
                Description = "Main Feeder OCPD",
                Voltage = 480,
                InterruptingRating = 65
            }
        };

        // Set up data binding
        DataContext = new
        {
            RootNode = _rootNode,
            CalculationLog = _calculationLog
        };

        CircuitTreeView.DataContext = new { RootNode = _rootNode };
        RefreshCircuitViewMode();
    }

    #region Menu Event Handlers

    private void MenuNewProject_Click(object sender, RoutedEventArgs e)
    {
        _rootNode = new CircuitNode("Main Panel", "feeder")
        {
            Device = new Device
            {
                Manufacturer = "System",
                Description = "Main Feeder OCPD",
                Voltage = 480,
                InterruptingRating = 65
            }
        };
        CircuitTreeView.DataContext = new { RootNode = _rootNode };
        _selectedNode = null;
        _currentProjectPath = null;
        RefreshCircuitViewMode();
        _calculationLog.Clear();
        ClearPropertyPanel();
        ClearResultsPanel();
        MessageBox.Show("New project created.");
    }

    private void MenuOpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open SCCR Project",
            Filter = "SCCR Project (*.sccr.json)|*.sccr.json|JSON Files (*.json)|*.json|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadProject(dialog.FileName);
        }
    }

    private void MenuSaveProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save SCCR Project",
            Filter = "SCCR Project (*.sccr.json)|*.sccr.json|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(_currentProjectPath)
                ? "SCCR_Project.sccr.json"
                : System.IO.Path.GetFileName(_currentProjectPath)
        };

        if (dialog.ShowDialog(this) == true)
        {
            SaveProject(dialog.FileName);
        }
    }

    private void MenuExportReport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export SCCR CSV Report",
            Filter = "CSV Report (*.csv)|*.csv|All Files (*.*)|*.*",
            FileName = "SCCR_Report.csv"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ExportCsvReport(dialog.FileName);
        }
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void MenuAddFeeder_Click(object sender, RoutedEventArgs e)
    {
        AddNodeToSelected("feeder");
    }

    private void MenuAddBranch_Click(object sender, RoutedEventArgs e)
    {
        AddNodeToSelected("branch");
    }

    private void MenuRemoveNode_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelectedNode();
    }

    private void MenuRecalculate_Click(object sender, RoutedEventArgs e)
    {
        CalculateSccr();
    }

    private void MenuInsertDeviceFromDatabase_Click(object sender, RoutedEventArgs e)
    {
        InsertDeviceFromDatabase();
    }

    private void MenuManageDeviceDatabase_Click(object sender, RoutedEventArgs e)
    {
        ManageDeviceDatabase();
    }

    private void MenuAddSelectedDeviceToDatabase_Click(object sender, RoutedEventArgs e)
    {
        AddSelectedDeviceToDatabase();
    }

    private void MenuImportDeviceDatabase_Click(object sender, RoutedEventArgs e)
    {
        ImportDeviceDatabase();
    }

    private void MenuExportDeviceDatabase_Click(object sender, RoutedEventArgs e)
    {
        ExportDeviceDatabase();
    }

    private void MenuSetDeviceDatabasePath_Click(object sender, RoutedEventArgs e)
    {
        SetDeviceDatabasePath();
    }

    #endregion

    #region Toolbar Event Handlers

    private void ButtonAddNode_Click(object sender, RoutedEventArgs e)
    {
        AddNodeToSelected("branch");
    }

    private void ButtonInsertDeviceFromDatabase_Click(object sender, RoutedEventArgs e)
    {
        InsertDeviceFromDatabase();
    }

    private void ButtonManageDeviceDatabase_Click(object sender, RoutedEventArgs e)
    {
        ManageDeviceDatabase();
    }

    private void ButtonRemoveNode_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelectedNode();
    }

    private void ButtonCalculate_Click(object sender, RoutedEventArgs e)
    {
        CalculateSccr();
    }

    private void FlowchartViewCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        RefreshCircuitViewMode();
    }

    #endregion

    #region TreeView Event Handlers

    private void CircuitTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _selectedNode = e.NewValue as CircuitNode;
        if (_selectedNode != null)
        {
            DisplayNodeProperties(_selectedNode);
        }
    }

    private void CircuitTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var treeViewItem = FindVisualParent<TreeViewItem>(e.OriginalSource as DependencyObject);
        if (treeViewItem == null)
            return;

        treeViewItem.IsSelected = true;
        treeViewItem.Focus();
        e.Handled = true;
    }

    private void TreeMenuAddBranch_Click(object sender, RoutedEventArgs e)
    {
        AddNodeToSelected("branch");
    }

    private void TreeMenuInsertDevice_Click(object sender, RoutedEventArgs e)
    {
        InsertDeviceFromDatabase();
    }

    private void TreeMenuEditBranch_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedNode != null)
        {
            DisplayNodeProperties(_selectedNode);
        }
    }

    private void TreeMenuRemoveNode_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelectedNode();
    }

    #endregion

    #region UI Display Methods

    private void DisplayNodeProperties(CircuitNode node)
    {
        ClearPropertyPanel();

        var panel = PropertyPanel as StackPanel;
        if (panel == null) return;

        // Node name and type
        AddPropertyField(panel, "Node Name:", node.Name, (value) => node.Name = value);
        AddDeviceTypeField(panel, node);

        if (node.Device == null)
        {
            var createDeviceBtn = new Button
            {
                Content = "Create Device",
                Padding = new Thickness(5),
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White
            };
            createDeviceBtn.Click += (s, e) =>
            {
                node.Device = new Device();
                NormalizeFuseAssemblyDevice(node);
                DisplayNodeProperties(node);
            };
            panel.Children.Add(createDeviceBtn);
            return;
        }

        var device = node.Device;
        NormalizeFuseAssemblyDevice(node);
        var isFuseBlockAssembly = IsFuseBlockAssembly(node.DeviceType);

        // Device properties
        if (isFuseBlockAssembly)
            AddSectionHeader(panel, "Fuse Block / Holder");

        AddPropertyField(panel, isFuseBlockAssembly ? "Block Mfg:" : "Manufacturer:", device.Manufacturer, (value) => device.Manufacturer = value);
        AddPropertyField(panel, isFuseBlockAssembly ? "Block Part #:" : "Part Number:", device.PartNumber, (value) => device.PartNumber = value);
        AddPropertyField(panel, isFuseBlockAssembly ? "Block IPN:" : "IPN:", device.InternalPartNumber, (value) => device.InternalPartNumber = value);
        AddPropertyField(panel, isFuseBlockAssembly ? "Block Description:" : "Description:", device.Description, (value) => device.Description = value);
        AddPropertyField(panel, "Image Path:", device.ImagePath, (value) => device.ImagePath = value);
        DisplayDeviceImage(panel, device.ImagePath);
        AddPropertyNumericField(panel, "Voltage (V):", device.Voltage, (value) => device.Voltage = value);
        
        // Show SCCR with default value available
        AddPropertyNumericField(panel, "SCCR (kA):", device.SccrRating, (value) => device.SccrRating = value);
        
        // Display default SCCR if available
        var defaultSccr = _defaultSccrLookup.GetDefaultSccr(node.DeviceType);
        if (defaultSccr.HasValue)
        {
            var defaultSource = _defaultSccrLookup.GetDefaultSource(node.DeviceType);
            AddPropertyHint(panel, $"Default: {defaultSccr} kA ({defaultSource})");
        }
        
        AddPropertyNumericField(panel, "OCPD Interrupting Rating (kA):", device.InterruptingRating, (value) => device.InterruptingRating = value);
        AddPropertyNumericField(panel, "OCPD Amp Rating:", device.OcpdAmps, (value) => device.OcpdAmps = value);
        AddPropertyNumericField(panel, "Input Current (A):", device.InputCurrentAmps, (value) => device.InputCurrentAmps = value);
        
        // Fuse properties with let-through lookup
        if (isFuseBlockAssembly)
        {
            AddSectionHeader(panel, "Fuse");
            AddPropertyField(panel, "Fuse Mfg:", device.FuseManufacturer, (value) => device.FuseManufacturer = value);
            AddPropertyField(panel, "Fuse Part #:", device.FusePartNumber, (value) => device.FusePartNumber = value);
            AddPropertyField(panel, "Fuse IPN:", device.FuseInternalPartNumber, (value) => device.FuseInternalPartNumber = value);
        }

        AddPropertyField(panel, "Fuse Class:", device.FuseClass, (value) => 
        { 
            device.FuseClass = value;
            RefreshFuseLetThroughDisplay(panel, device, node);
        });
        
        AddPropertyNumericField(panel, "Fuse Amps:", device.FuseAmps, (value) => 
        { 
            device.FuseAmps = value;
            RefreshFuseLetThroughDisplay(panel, device, node);
        });
        
        AddPropertyNumericField(panel, "Let-Through Current (kA):", device.LetThroughCurrent, (value) => device.LetThroughCurrent = value);
        
        // Display fuse let-through table if class and amps are available
        if (!string.IsNullOrWhiteSpace(device.FuseClass) && device.FuseAmps.HasValue && device.FuseAmps > 0)
        {
            DisplayFuseLetThroughTable(panel, device);
        }
        
        AddPropertyField(panel, "Notes:", device.Notes, (value) => device.Notes = value);
        AddPropertyField(panel, "Source:", device.Source, (value) => device.Source = value);

        // Exempt checkbox
        var exemptCheckBox = new CheckBox { Content = "Exempt from SCCR", IsChecked = device.ExemptFromSccr, Margin = new Thickness(0, 10, 0, 5) };
        exemptCheckBox.Checked += (s, e) => device.ExemptFromSccr = true;
        exemptCheckBox.Unchecked += (s, e) => device.ExemptFromSccr = false;
        panel.Children.Add(exemptCheckBox);

        AddPropertyField(panel, "Exempt Reason:", device.ExemptReason, (value) => device.ExemptReason = value);

        var saveToDatabaseButton = new Button
        {
            Content = "Add/Update Device Database Entry",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 12, 0, 0)
        };
        saveToDatabaseButton.Click += (_, _) => AddSelectedDeviceToDatabase();
        panel.Children.Add(saveToDatabaseButton);
    }

    private void DisplayDeviceImage(StackPanel panel, string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            return;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(imagePath);
            image.EndInit();

            panel.Children.Add(new Image
            {
                Source = image,
                Width = 180,
                Height = 120,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(120, 6, 0, 8)
            });
        }
        catch
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Image could not be loaded.",
                Foreground = Brushes.Orange,
                Margin = new Thickness(120, 4, 0, 8)
            });
        }
    }

    private void RefreshCircuitViewMode()
    {
        var showFlowchart = FlowchartViewCheckBox.IsChecked == true;
        CircuitTreeView.Visibility = showFlowchart ? Visibility.Collapsed : Visibility.Visible;
        FlowchartScrollViewer.Visibility = showFlowchart ? Visibility.Visible : Visibility.Collapsed;

        if (showFlowchart)
        {
            RefreshFlowchartView();
        }
    }

    private void RefreshFlowchartView()
    {
        FlowchartPanel.Children.Clear();
        FlowchartPanel.Children.Add(BuildFlowchartNode(_rootNode, null));
    }

    private UIElement BuildFlowchartNode(CircuitNode node, CircuitNode? parent)
    {
        var wrapper = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(8)
        };

        if (parent != null)
        {
            wrapper.Children.Add(BuildWireLabel(parent));
        }

        wrapper.Children.Add(BuildDeviceFlowchartBox(node));

        if (node.Children.Count > 0)
        {
            var childrenPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            foreach (var child in node.Children)
            {
                childrenPanel.Children.Add(BuildFlowchartNode(child, node));
            }

            wrapper.Children.Add(childrenPanel);
        }

        return wrapper;
    }

    private UIElement BuildDeviceFlowchartBox(CircuitNode node)
    {
        var device = node.Device;
        var border = new Border
        {
            BorderBrush = Brushes.SteelBlue,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            Padding = new Thickness(8),
            MinWidth = 150,
            MaxWidth = 210,
            CornerRadius = new CornerRadius(4),
            Cursor = Cursors.Hand
        };

        border.MouseLeftButtonDown += (_, _) =>
        {
            _selectedNode = node;
            DisplayNodeProperties(node);
        };

        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(node.Name) ? "Unnamed Device" : node.Name,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        });
        content.Children.Add(new TextBlock
        {
            Text = $"IPN: {DisplayValue(device?.InternalPartNumber)}",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        });
        content.Children.Add(new TextBlock
        {
            Text = $"SCCR: {FormatKa(device?.SccrRating)}",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        });

        border.Child = content;
        return border;
    }

    private UIElement BuildWireLabel(CircuitNode upstreamNode)
    {
        var sizing = GetWireSizingCurrent(upstreamNode);
        var wireSize = _ampacityLookupService.GetCopper75CConductorSize(sizing.RequiredAmps);
        var label = sizing.RequiredAmps.HasValue
            ? $"Cu wire: {wireSize} ({sizing.RequiredAmps:g} A)"
            : $"Cu wire: {wireSize}";

        return new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "|",
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.Gray
                },
                new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.LightYellow,
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(0, 2, 0, 4),
                    Child = new TextBlock
                    {
                        Text = label,
                        FontSize = 10,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
                ,
                new TextBlock
                {
                    Text = sizing.Note,
                    FontSize = 9,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 3)
                }
            }
        };
    }

    private static WireSizingBasis GetWireSizingCurrent(CircuitNode upstreamNode)
    {
        foreach (var child in upstreamNode.Children)
        {
            if (IsPowerConversionEquipment(child) && child.Device?.InputCurrentAmps > 0)
            {
                return new WireSizingBasis(
                    child.Device.InputCurrentAmps * 1.25,
                    "Table 28.1 75C; 125% input current");
            }
        }

        var current = upstreamNode;
        while (current != null)
        {
            var device = current.Device;
            if (device?.OcpdAmps > 0)
                return new WireSizingBasis(device.OcpdAmps, "Table 28.1 75C; OCPD rating");

            if (device?.FuseAmps > 0)
                return new WireSizingBasis(device.FuseAmps, "Table 28.1 75C; fuse rating");

            current = current.Parent;
        }

        return new WireSizingBasis(null, "Table 28.1 75C");
    }

    private static bool IsPowerConversionEquipment(CircuitNode node)
    {
        var type = node.DeviceType.ToLowerInvariant();
        return type.Contains("drive")
            || type.Contains("power-conversion")
            || type.Contains("solid-state")
            || type.Contains("speed-controller");
    }

    private static string DisplayValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "TBD" : value;
    }

    private static string FormatKa(double? value)
    {
        return value.HasValue && value > 0 ? $"{value:g} kA" : "TBD";
    }

    private record WireSizingBasis(double? RequiredAmps, string Note);

    private void AddPropertyField(StackPanel panel, string label, string value, Action<string> onChanged)
    {
        var grid = new Grid { Margin = new Thickness(0, 5, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
        var textBox = new TextBox { Text = value ?? "", Padding = new Thickness(5), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) };
        textBox.TextChanged += (s, e) => onChanged(textBox.Text);

        Grid.SetColumn(labelBlock, 0);
        Grid.SetColumn(textBox, 1);
        grid.Children.Add(labelBlock);
        grid.Children.Add(textBox);
        panel.Children.Add(grid);
    }

    private static void AddSectionHeader(StackPanel panel, string text)
    {
        panel.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.DarkSlateGray,
            Margin = new Thickness(0, 12, 0, 2)
        });
    }

    private void AddDeviceTypeField(StackPanel panel, CircuitNode node)
    {
        var normalizedType = NormalizeDeviceType(node.DeviceType);
        if (!string.Equals(node.DeviceType, normalizedType, StringComparison.Ordinal))
            node.DeviceType = normalizedType;
        var knownMatch = DeviceTypeCatalog.KnownTypes.FirstOrDefault(type =>
            type.Equals(normalizedType, StringComparison.OrdinalIgnoreCase));
        var isCustomType = !string.IsNullOrWhiteSpace(normalizedType) && knownMatch == null;

        var grid = new Grid { Margin = new Thickness(0, 5, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = "Device Type:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 5, 0)
        };

        var selectorPanel = new StackPanel();
        var comboBox = new ComboBox
        {
            ItemsSource = DeviceTypeCatalog.KnownTypes,
            SelectedItem = isCustomType ? "other" : (knownMatch ?? "other"),
            Padding = new Thickness(5)
        };

        var customTextBox = new TextBox
        {
            Text = isCustomType ? normalizedType : "",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 5, 0, 0),
            Visibility = isCustomType || comboBox.SelectedItem?.ToString() == "other"
                ? Visibility.Visible
                : Visibility.Collapsed
        };

        comboBox.SelectionChanged += (_, _) =>
        {
            var selectedType = NormalizeDeviceType(comboBox.SelectedItem?.ToString() ?? "");
            if (selectedType == "other")
            {
                customTextBox.Visibility = Visibility.Visible;
                node.DeviceType = customTextBox.Text.Trim();
            }
            else
            {
                customTextBox.Visibility = Visibility.Collapsed;
                node.DeviceType = selectedType;
                DisplayNodeProperties(node);
            }
        };

        customTextBox.TextChanged += (_, _) =>
        {
            if (comboBox.SelectedItem?.ToString() == "other")
            {
                node.DeviceType = customTextBox.Text.Trim();
            }
        };

        selectorPanel.Children.Add(comboBox);
        selectorPanel.Children.Add(customTextBox);

        Grid.SetColumn(labelBlock, 0);
        Grid.SetColumn(selectorPanel, 1);
        grid.Children.Add(labelBlock);
        grid.Children.Add(selectorPanel);
        panel.Children.Add(grid);
    }

    private static void AddPropertyHint(StackPanel panel, string text)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var hintBlock = new TextBlock
        {
            Text = text,
            Foreground = Brushes.DarkBlue,
            FontSize = 11,
            FontStyle = FontStyles.Italic,
            TextWrapping = TextWrapping.Wrap
        };

        Grid.SetColumn(hintBlock, 1);
        grid.Children.Add(hintBlock);
        panel.Children.Add(grid);
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

    private static bool IsFuseBlockAssembly(string deviceType)
    {
        return NormalizeDeviceType(deviceType).Equals("fuse + fuse-block", StringComparison.OrdinalIgnoreCase);
    }

    private static void NormalizeFuseAssemblyDevice(CircuitNode node)
    {
        if (!IsFuseBlockAssembly(node.DeviceType) || node.Device == null)
            return;

        var device = node.Device;
        if (string.IsNullOrWhiteSpace(device.FuseManufacturer)
            && string.IsNullOrWhiteSpace(device.FusePartNumber)
            && string.IsNullOrWhiteSpace(device.FuseInternalPartNumber)
            && device.FuseAmps.HasValue
            && string.IsNullOrWhiteSpace(device.Manufacturer)
            && string.IsNullOrWhiteSpace(device.PartNumber))
        {
            device.FuseManufacturer = device.Manufacturer;
            device.FusePartNumber = device.PartNumber;
            device.FuseInternalPartNumber = device.InternalPartNumber;
        }

        if (!device.OcpdAmps.HasValue && device.FuseAmps.HasValue)
            device.OcpdAmps = device.FuseAmps;
    }

    private void AddPropertyNumericField(StackPanel panel, string label, double? value, Action<double?> onChanged)
    {
        var grid = new Grid { Margin = new Thickness(0, 5, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
        var textBox = new TextBox { Text = value?.ToString() ?? "", Padding = new Thickness(5), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) };
        textBox.TextChanged += (s, e) =>
        {
            if (double.TryParse(textBox.Text, out var result))
                onChanged(result);
            else if (string.IsNullOrWhiteSpace(textBox.Text))
                onChanged(null);
        };

        Grid.SetColumn(labelBlock, 0);
        Grid.SetColumn(textBox, 1);
        grid.Children.Add(labelBlock);
        grid.Children.Add(textBox);
        panel.Children.Add(grid);
    }

    private void ClearPropertyPanel()
    {
        var panel = PropertyPanel as StackPanel;
        if (panel != null)
        {
            panel.Children.Clear();
            panel.Children.Add(new TextBlock { Text = "(Select a node to edit)", Foreground = Brushes.Gray });
        }
    }

    private void ClearResultsPanel()
    {
        var panel = ResultsPanel as StackPanel;
        if (panel != null)
        {
            panel.Children.Clear();
            panel.Children.Add(new TextBlock { Text = "(Click Calculate SCCR to run calculation)", Foreground = Brushes.Gray });
        }
    }

    private void DisplayCalculationResults(CalculationResult result)
    {
        ClearResultsPanel();
        var panel = ResultsPanel as StackPanel;
        if (panel == null) return;

        // Title
        var titleBlock = new TextBlock
        {
            Text = "Calculation Results",
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.DarkGreen,
            Margin = new Thickness(0, 0, 0, 10),
            FontSize = 14
        };
        panel.Children.Add(titleBlock);

        // Overall SCCR
        var overallBlock = new TextBlock
        {
            Text = $"Overall Panel SCCR: {result.OverallSccr} kA",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        };
        panel.Children.Add(overallBlock);

        // Target indicators
        var target100Block = new TextBlock
        {
            Text = result.OverallSccr >= 100 ? "✓ Meets 100 kA target" : "✗ Below 100 kA target",
            Foreground = result.OverallSccr >= 100 ? Brushes.Green : Brushes.Red,
            Margin = new Thickness(0, 0, 0, 3)
        };
        panel.Children.Add(target100Block);

        var target10Block = new TextBlock
        {
            Text = result.OverallSccr >= 10 ? "✓ Meets 10 kA minimum" : "✗ Below 10 kA minimum",
            Foreground = result.OverallSccr >= 10 ? Brushes.Green : Brushes.Red,
            Margin = new Thickness(0, 0, 0, 10)
        };
        panel.Children.Add(target10Block);

        // Limiting device
        if (result.LimitingNode != null)
        {
            var limitingBlock = new TextBlock
            {
                Text = $"Limiting Device: {result.LimitingReason}",
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.DarkRed,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(limitingBlock);
        }

        // Warnings
        if (result.Warnings.Count > 0)
        {
            var warningsBlock = new TextBlock
            {
                Text = "Warnings:",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Orange,
                Margin = new Thickness(0, 0, 0, 5)
            };
            panel.Children.Add(warningsBlock);

            foreach (var warning in result.Warnings)
            {
                var warningBlock = new TextBlock
                {
                    Text = "• " + warning,
                    Margin = new Thickness(10, 0, 0, 3),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.Orange
                };
                panel.Children.Add(warningBlock);
            }
        }
    }

    private void DisplayFuseLetThroughTable(StackPanel panel, Device device)
    {
        if (string.IsNullOrWhiteSpace(device.FuseClass) || !device.FuseAmps.HasValue)
            return;

        var curve = _fuseLetThroughLookup.GetFuseLetThroughCurve(device.FuseClass, device.FuseAmps.Value);
        if (curve == null || curve.Count == 0)
            return;

        // Add separator
        var separator = new Separator { Margin = new Thickness(0, 10, 0, 10) };
        panel.Children.Add(separator);

        // Title
        var titleBlock = new TextBlock
        {
            Text = $"Fuse Let-Through Curve ({device.FuseClass}-{device.FuseAmps}A)",
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.DarkGreen,
            Margin = new Thickness(0, 0, 0, 8),
            FontSize = 12
        };
        panel.Children.Add(titleBlock);

        // Table header
        var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

        var faultHeader = new TextBlock { Text = "Fault Current (kA)", FontWeight = FontWeights.Bold, Foreground = Brushes.White, Background = Brushes.DarkGray, Padding = new Thickness(5) };
        var letThroughHeader = new TextBlock { Text = "Peak Let-Through (kA)", FontWeight = FontWeights.Bold, Foreground = Brushes.White, Background = Brushes.DarkGray, Padding = new Thickness(5) };

        Grid.SetColumn(faultHeader, 0);
        Grid.SetColumn(letThroughHeader, 1);
        headerGrid.Children.Add(faultHeader);
        headerGrid.Children.Add(letThroughHeader);
        panel.Children.Add(headerGrid);

        // Table rows
        foreach (var point in curve.OrderBy(p => p.FaultCurrentKa))
        {
            var rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

            var faultBlock = new TextBlock { Text = $"{point.FaultCurrentKa:F0}", Padding = new Thickness(5) };
            var letThroughBlock = new TextBlock { Text = $"{point.PeakLetThroughKa:F1}", Padding = new Thickness(5) };

            Grid.SetColumn(faultBlock, 0);
            Grid.SetColumn(letThroughBlock, 1);
            rowGrid.Children.Add(faultBlock);
            rowGrid.Children.Add(letThroughBlock);
            panel.Children.Add(rowGrid);
        }
    }

    private void RefreshFuseLetThroughDisplay(StackPanel panel, Device device, CircuitNode node)
    {
        // Remove old fuse table display if it exists
        var separatorIndex = -1;
        for (int i = panel.Children.Count - 1; i >= 0; i--)
        {
            if (panel.Children[i] is Separator)
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex >= 0)
        {
            while (panel.Children.Count > separatorIndex)
            {
                panel.Children.RemoveAt(separatorIndex);
            }
        }

        // Redisplay if fuse data is available
        if (!string.IsNullOrWhiteSpace(device.FuseClass) && device.FuseAmps.HasValue && device.FuseAmps > 0)
        {
            DisplayFuseLetThroughTable(panel, device);
        }
    }

    #endregion

    #region Device Database

    private void InsertDeviceFromDatabase()
    {
        var dialog = new DeviceDatabaseWindow(_deviceDatabaseService, allowInsert: true) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SelectedEntry == null)
            return;

        var selectedEntry = dialog.SelectedEntry;
        var parent = _selectedNode ?? _rootNode;
        selectedEntry.DeviceType = NormalizeDeviceType(selectedEntry.DeviceType);
        var nodeName = BuildDeviceNodeName(selectedEntry);
        var newNode = new CircuitNode(nodeName, selectedEntry.DeviceType)
        {
            Device = CreateDeviceFromDatabaseEntry(selectedEntry)
        };

        parent.AddChild(newNode);
        CircuitTreeView.Items.Refresh();
        RefreshCircuitViewMode();
        ClearResultsPanel();

        _calculationLog.Add($"Inserted {selectedEntry.Manufacturer} {selectedEntry.PartNumber} under {parent.Name}.");
    }

    private void ManageDeviceDatabase()
    {
        var dialog = new DeviceDatabaseWindow(_deviceDatabaseService, allowInsert: false) { Owner = this };
        dialog.ShowDialog();
    }

    private void AddSelectedDeviceToDatabase()
    {
        if (_selectedNode?.Device == null)
        {
            MessageBox.Show(this, "Select a circuit node with a device first.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var entry = DeviceDatabaseEntry.FromDevice(_selectedNode.Device, _selectedNode.DeviceType);
        var editor = new DeviceEntryEditorWindow(entry) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            _deviceDatabaseService.AddOrUpdate(editor.Entry);
            MessageBox.Show(this, "Device database entry saved.", "Device Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ImportDeviceDatabase()
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
            MessageBox.Show(this, "Device database imported.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to import device database: {ex.Message}", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportDeviceDatabase()
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

    private void SetDeviceDatabasePath()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Select Device Database Path",
            Filter = "Device Database (*.json)|*.json|All Files (*.*)|*.*",
            FileName = System.IO.Path.GetFileName(_deviceDatabaseService.DatabasePath),
            InitialDirectory = System.IO.Path.GetDirectoryName(_deviceDatabaseService.DatabasePath)
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            _deviceDatabaseService.SetDatabasePath(dialog.FileName);
            MessageBox.Show(this, $"Device database path set to:\n{_deviceDatabaseService.DatabasePath}", "Database Path", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to set device database path: {ex.Message}", "Path Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string BuildDeviceNodeName(DeviceDatabaseEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Manufacturer) && !string.IsNullOrWhiteSpace(entry.PartNumber))
            return $"{entry.Manufacturer} {entry.PartNumber}";

        if (!string.IsNullOrWhiteSpace(entry.PartNumber))
            return entry.PartNumber;

        return string.IsNullOrWhiteSpace(entry.DeviceType) ? "New Device" : entry.DeviceType;
    }

    private static Device CreateDeviceFromDatabaseEntry(DeviceDatabaseEntry entry)
    {
        var device = entry.ToDevice();
        if (!IsFuseBlockAssembly(entry.DeviceType))
            return device;

        if (string.IsNullOrWhiteSpace(device.FuseManufacturer) && string.IsNullOrWhiteSpace(device.FusePartNumber))
        {
            device.FuseManufacturer = entry.Manufacturer;
            device.FusePartNumber = entry.PartNumber;
            device.FuseInternalPartNumber = entry.InternalPartNumber;
        }

        if (!device.OcpdAmps.HasValue && device.FuseAmps.HasValue)
            device.OcpdAmps = device.FuseAmps;

        return device;
    }

    #endregion
 
    #region Project Persistence and Reporting

    private void SaveProject(string filePath)
    {
        try
        {
            var project = new SccrProjectFile
            {
                ProjectVersion = 1,
                SavedUtc = DateTime.UtcNow,
                AvailableFaultCurrentKa = GetAvailableFaultCurrentKa(),
                RootNode = _rootNode
            };

            var json = JsonSerializer.Serialize(project, GetJsonOptions());
            File.WriteAllText(filePath, json, Encoding.UTF8);
            _currentProjectPath = filePath;

            MessageBox.Show($"Project saved to {filePath}", "SCCR Project Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to save project: {ex.Message}", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadProject(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var project = JsonSerializer.Deserialize<SccrProjectFile>(json, GetJsonOptions());

            if (project?.RootNode == null)
                throw new InvalidDataException("Project file does not contain a circuit tree.");

            _rootNode = project.RootNode;
            ReattachParentLinks(_rootNode, null);
            _selectedNode = null;
            _currentProjectPath = filePath;

            AvailableFaultCurrentTextBox.Text = project.AvailableFaultCurrentKa > 0
                ? project.AvailableFaultCurrentKa.ToString(CultureInfo.InvariantCulture)
                : "100";

            DataContext = new
            {
                RootNode = _rootNode,
                CalculationLog = _calculationLog
            };
            CircuitTreeView.DataContext = new { RootNode = _rootNode };
            CircuitTreeView.Items.Refresh();
            RefreshCircuitViewMode();

            _calculationLog.Clear();
            ClearPropertyPanel();
            ClearResultsPanel();

            MessageBox.Show($"Project loaded from {filePath}", "SCCR Project Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to load project: {ex.Message}", "Open Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportCsvReport(string filePath)
    {
        try
        {
            var availableFaultCurrentKa = GetAvailableFaultCurrentKa();
            var result = _calculator.Calculate(_rootNode, availableFaultCurrentKa);
            DisplayCalculationResults(result);

            var csv = new StringBuilder();
            csv.AppendLine("SCCR Calculation Summary");
            csv.AppendLine($"Available Fault Current (kA),{FormatCsv(availableFaultCurrentKa)}");
            csv.AppendLine($"Overall Panel SCCR (kA),{FormatCsv(result.OverallSccr)}");
            csv.AppendLine($"Limiting Device,{FormatCsv(result.LimitingNode?.Name ?? "")}");
            csv.AppendLine($"Limiting Reason,{FormatCsv(result.LimitingReason)}");
            csv.AppendLine();

            csv.AppendLine("Calculation Log");
            csv.AppendLine("Node,Device Type,Component SCCR (kA),OCPD Interrupting Rating (kA),Resulting SCCR (kA),Notes");
            foreach (var entry in result.LogEntries)
            {
                csv.AppendLine(string.Join(",",
                    FormatCsv(entry.NodeName),
                    FormatCsv(entry.DeviceType),
                    FormatCsv(entry.ComponentSccr),
                    FormatCsv(entry.OcpdRating),
                    FormatCsv(entry.ResultingSccr),
                    FormatCsv(entry.Notes)));
            }

            csv.AppendLine();
            csv.AppendLine("Warnings");
            foreach (var warning in result.Warnings)
            {
                csv.AppendLine(FormatCsv(warning));
            }

            csv.AppendLine();
            csv.AppendLine("Circuit Devices");
            csv.AppendLine("Path,Node,Device Type,Manufacturer,Part Number,IPN,Description,Image Path,Voltage,SCCR,Interrupting Rating,OCPD Amp Rating,Input Current,Fuse Manufacturer,Fuse Part Number,Fuse IPN,Fuse Class,Fuse Amps,Let-Through Current,Source,Notes,Exempt,Exempt Reason");
            AppendDeviceRows(csv, _rootNode, _rootNode.Name);

            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
            MessageBox.Show($"CSV report exported to {filePath}", "SCCR Report Exported", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to export report: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    private static void ReattachParentLinks(CircuitNode node, CircuitNode? parent)
    {
        node.Parent = parent;

        foreach (var child in node.Children)
        {
            ReattachParentLinks(child, node);
        }
    }

    private static void AppendDeviceRows(StringBuilder csv, CircuitNode node, string path)
    {
        var device = node.Device;
        csv.AppendLine(string.Join(",",
            FormatCsv(path),
            FormatCsv(node.Name),
            FormatCsv(node.DeviceType),
            FormatCsv(device?.Manufacturer),
            FormatCsv(device?.PartNumber),
            FormatCsv(device?.InternalPartNumber),
            FormatCsv(device?.Description),
            FormatCsv(device?.ImagePath),
            FormatCsv(device?.Voltage),
            FormatCsv(device?.SccrRating),
            FormatCsv(device?.InterruptingRating),
            FormatCsv(device?.OcpdAmps),
            FormatCsv(device?.InputCurrentAmps),
            FormatCsv(device?.FuseManufacturer),
            FormatCsv(device?.FusePartNumber),
            FormatCsv(device?.FuseInternalPartNumber),
            FormatCsv(device?.FuseClass),
            FormatCsv(device?.FuseAmps),
            FormatCsv(device?.LetThroughCurrent),
            FormatCsv(device?.Source),
            FormatCsv(device?.Notes),
            FormatCsv(device?.ExemptFromSccr),
            FormatCsv(device?.ExemptReason)));

        foreach (var child in node.Children)
        {
            AppendDeviceRows(csv, child, $"{path} > {child.Name}");
        }
    }

    private static string FormatCsv(object? value)
    {
        var text = value switch
        {
            null => "",
            double number => number.ToString(CultureInfo.InvariantCulture),
            bool flag => flag ? "true" : "false",
            _ => value.ToString() ?? ""
        };

        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            text = "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        return text;
    }

    #endregion
 
    #region Calculation Methods

    private void CalculateSccr()
    {
        _calculationLog.Clear();

        var availableFaultCurrentKa = GetAvailableFaultCurrentKa();
        var result = _calculator.Calculate(_rootNode, availableFaultCurrentKa);

        // Display results
        DisplayCalculationResults(result);
        RefreshCircuitViewMode();

        // Populate log
        foreach (var entry in result.LogEntries)
        {
            _calculationLog.Add(entry.ToString());
        }

        if (result.Warnings.Count > 0)
        {
            _calculationLog.Add("--- WARNINGS ---");
            foreach (var warning in result.Warnings)
            {
                _calculationLog.Add($"WARNING: {warning}");
            }
        }

        _calculationLog.Add($"Available fault current used: {availableFaultCurrentKa} kA");
        _calculationLog.Add($"\n=== FINAL RESULT: {result.OverallSccr} kA ===");
    }

    private double GetAvailableFaultCurrentKa()
    {
        if (double.TryParse(AvailableFaultCurrentTextBox.Text, out var availableFaultCurrentKa) && availableFaultCurrentKa > 0)
            return availableFaultCurrentKa;

        _calculationLog.Add("WARNING: Available fault current must be a positive number. Using 100 kA.");
        AvailableFaultCurrentTextBox.Text = "100";
        return 100;
    }

    private void AddNodeToSelected(string deviceType)
    {
        var parent = _selectedNode ?? _rootNode;
        var normalizedDeviceType = NormalizeDeviceType(deviceType);
        var newNode = new CircuitNode($"New {normalizedDeviceType}", normalizedDeviceType);
        parent.AddChild(newNode);
        
        // Refresh tree
        CircuitTreeView.Items.Refresh();
        RefreshCircuitViewMode();
    }

    private void RemoveSelectedNode()
    {
        if (_selectedNode?.Parent == null)
            return;

        var parent = _selectedNode.Parent;
        parent.RemoveChild(_selectedNode);
        _selectedNode = parent;
        CircuitTreeView.Items.Refresh();
        RefreshCircuitViewMode();
        DisplayNodeProperties(parent);
        ClearResultsPanel();
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T typedParent)
                return typedParent;

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    #endregion

    private class SccrProjectFile
    {
        public int ProjectVersion { get; set; }
        public DateTime SavedUtc { get; set; }
        public double AvailableFaultCurrentKa { get; set; } = 100;
        public CircuitNode? RootNode { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HELIOS.Platform.Phase10.BuilderUI
{
    /// <summary>
    /// Package selector component for step 4 of the wizard.
    /// Allows selection of packages with category grouping and dependency tracking.
    /// </summary>
    public class PackageSelector : UserControl
    {
        private IBuilderUIService _service;
        private StackPanel _packagesPanel;
        private TextBlock _totalSizeText;
        private Dictionary<string, CheckBox> _packageCheckboxes;
        private long _totalSelectedSize;

        public event EventHandler<long> TotalSizeChanged;

        public PackageSelector()
        {
            InitializeUI();
        }

        /// <summary>
        /// Initialize the package selector UI.
        /// </summary>
        private void InitializeUI()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10), Spacing = 10 };

            // Title
            panel.Children.Add(new TextBlock { Text = "Select Packages", FontWeight = System.Windows.FontWeights.Bold, FontSize = 14 });

            // Packages container with scroll
            var scrollViewer = new ScrollViewer { Height = 300 };
            _packagesPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(5) };
            scrollViewer.Content = _packagesPanel;
            panel.Children.Add(scrollViewer);

            // Total size display
            _totalSizeText = new TextBlock
            {
                Text = "Total Selected Size: 0 GB",
                Margin = new Thickness(10),
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LimeGreen)
            };
            panel.Children.Add(_totalSizeText);

            // Selection preset buttons
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(10) };
            
            var minimalButton = new Button { Content = "Minimal", Width = 80 };
            minimalButton.Click += async (s, e) => await SelectPresetAsync("minimal");
            buttonPanel.Children.Add(minimalButton);

            var standardButton = new Button { Content = "Standard", Width = 80 };
            standardButton.Click += async (s, e) => await SelectPresetAsync("standard");
            buttonPanel.Children.Add(standardButton);

            var completeButton = new Button { Content = "Complete", Width = 80 };
            completeButton.Click += async (s, e) => await SelectPresetAsync("complete");
            buttonPanel.Children.Add(completeButton);

            var clearButton = new Button { Content = "Clear All", Width = 80 };
            clearButton.Click += ClearAll_Click;
            buttonPanel.Children.Add(clearButton);

            panel.Children.Add(buttonPanel);

            _packageCheckboxes = new Dictionary<string, CheckBox>();
            this.Content = panel;
        }

        /// <summary>
        /// Initialize package selector with builder service.
        /// </summary>
        public async Task InitializeAsync(IBuilderUIService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            await LoadPackagesAsync();
        }

        /// <summary>
        /// Load all available packages grouped by category.
        /// </summary>
        public async Task LoadPackagesAsync()
        {
            try
            {
                _packagesPanel.Children.Clear();
                _packageCheckboxes.Clear();

                var packages = await _service.GetAllPackagesAsync();
                var categories = packages.Select(p => p.Category).Distinct().OrderBy(c => c);

                foreach (var category in categories)
                {
                    var categoryGroup = new GroupBox
                    {
                        Header = category,
                        Margin = new Thickness(5),
                        Padding = new Thickness(10)
                    };

                    var categoryPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };

                    var categoryPackages = packages.Where(p => p.Category == category).OrderBy(p => p.Name);
                    foreach (var pkg in categoryPackages)
                    {
                        var checkboxPanel = new StackPanel { Orientation = Orientation.Vertical };

                        var checkbox = new CheckBox
                        {
                            Content = $"{pkg.Name} ({FormatBytes(pkg.Size)})",
                            Margin = new Thickness(5),
                            IsChecked = pkg.IsSelected,
                            Tag = pkg.PackageId,
                            ToolTip = pkg.Description
                        };

                        checkbox.Checked += async (s, e) =>
                        {
                            await _service.SelectPackageAsync(pkg.PackageId);
                            await UpdateTotalSizeAsync();
                            await HandleDependenciesAsync(pkg.PackageId, true);
                        };

                        checkbox.Unchecked += async (s, e) =>
                        {
                            await _service.DeselectPackageAsync(pkg.PackageId);
                            await UpdateTotalSizeAsync();
                        };

                        _packageCheckboxes[pkg.PackageId] = checkbox;
                        checkboxPanel.Children.Add(checkbox);

                        // Show description
                        var descBlock = new TextBlock
                        {
                            Text = pkg.Description,
                            Margin = new Thickness(25, 0, 5, 5),
                            TextWrapping = System.Windows.TextWrapping.Wrap,
                            FontSize = 10,
                            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
                        };
                        checkboxPanel.Children.Add(descBlock);

                        // Show dependencies if any
                        if (pkg.Dependencies.Any())
                        {
                            var depsBlock = new TextBlock
                            {
                                Text = "Dependencies: " + string.Join(", ", pkg.Dependencies),
                                Margin = new Thickness(25, 0, 5, 5),
                                TextWrapping = System.Windows.TextWrapping.Wrap,
                                FontSize = 9,
                                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange)
                            };
                            checkboxPanel.Children.Add(depsBlock);
                        }

                        categoryPanel.Children.Add(checkboxPanel);
                    }

                    categoryGroup.Content = categoryPanel;
                    _packagesPanel.Children.Add(categoryGroup);
                }

                await UpdateTotalSizeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading packages: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Update total size display.
        /// </summary>
        private async Task UpdateTotalSizeAsync()
        {
            try
            {
                _totalSelectedSize = await _service.CalculateTotalSizeAsync();
                _totalSizeText.Text = $"Total Selected Size: {FormatBytes(_totalSelectedSize)}";
                TotalSizeChanged?.Invoke(this, _totalSelectedSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating size: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Handle package dependencies.
        /// </summary>
        private async Task HandleDependenciesAsync(string packageId, bool isSelected)
        {
            try
            {
                var dependencies = await _service.GetPackageDependenciesAsync(packageId);

                if (isSelected && dependencies.Any())
                {
                    foreach (var depId in dependencies)
                    {
                        if (_packageCheckboxes.ContainsKey(depId))
                        {
                            _packageCheckboxes[depId].IsChecked = true;
                        }
                    }
                }

                await UpdateTotalSizeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error handling dependencies: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Select package preset.
        /// </summary>
        private async Task SelectPresetAsync(string presetName)
        {
            try
            {
                var packages = await _service.GetAllPackagesAsync();

                foreach (var pkg in packages)
                {
                    bool shouldSelect = presetName switch
                    {
                        "minimal" => pkg.Category == "System",
                        "standard" => pkg.Category != "Media",
                        "complete" => true,
                        _ => pkg.IsSelected
                    };

                    if (shouldSelect)
                    {
                        await _service.SelectPackageAsync(pkg.PackageId);
                        if (_packageCheckboxes.ContainsKey(pkg.PackageId))
                        {
                            _packageCheckboxes[pkg.PackageId].IsChecked = true;
                        }
                    }
                    else
                    {
                        await _service.DeselectPackageAsync(pkg.PackageId);
                        if (_packageCheckboxes.ContainsKey(pkg.PackageId))
                        {
                            _packageCheckboxes[pkg.PackageId].IsChecked = false;
                        }
                    }
                }

                await UpdateTotalSizeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting preset: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Clear all selections.
        /// </summary>
        private async void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var packages = await _service.GetAllPackagesAsync();
                foreach (var pkg in packages)
                {
                    await _service.DeselectPackageAsync(pkg.PackageId);
                    if (_packageCheckboxes.ContainsKey(pkg.PackageId))
                    {
                        _packageCheckboxes[pkg.PackageId].IsChecked = false;
                    }
                }

                await UpdateTotalSizeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing packages: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Get selected packages.
        /// </summary>
        public async Task<List<Package>> GetSelectedPackagesAsync()
        {
            return await _service.GetSelectedPackagesAsync();
        }

        /// <summary>
        /// Format bytes to human-readable size.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}

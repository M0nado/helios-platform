// MONADO BLADE v3.4.0 - DASHBOARD UI IMPLEMENTATION
// File: src/MonadoBlade.GUI/Dashboard/DashboardUI.cs

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MonadoBlade.GUI.Design;
using MonadoBlade.GUI.Components;

namespace MonadoBlade.GUI.Dashboard
{
    /// <summary>
    /// Dashboard UI components for visualization and interaction
    /// </summary>
    public partial class DashboardUI
    {
        // ============================================================================
        // METRICS CHART COMPONENT
        // ============================================================================

        /// <summary>
        /// Chart component for visualizing metrics over time
        /// </summary>
        public class MetricsChart : Control
        {
            public enum ChartType { Line, Bar, Pie, Area, Histogram }

            static MetricsChart()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MetricsChart),
                    new FrameworkPropertyMetadata(typeof(MetricsChart)));
            }

            public ChartType Type
            {
                get { return (ChartType)GetValue(TypeProperty); }
                set { SetValue(TypeProperty, value); }
            }

            public static readonly DependencyProperty TypeProperty =
                DependencyProperty.Register("Type", typeof(ChartType), typeof(MetricsChart));

            public ObservableCollection<DataPoint> DataPoints
            {
                get { return (ObservableCollection<DataPoint>)GetValue(DataPointsProperty); }
                set { SetValue(DataPointsProperty, value); }
            }

            public static readonly DependencyProperty DataPointsProperty =
                DependencyProperty.Register("DataPoints", typeof(ObservableCollection<DataPoint>),
                    typeof(MetricsChart), new PropertyMetadata(new ObservableCollection<DataPoint>()));

            public string Title
            {
                get { return (string)GetValue(TitleProperty); }
                set { SetValue(TitleProperty, value); }
            }

            public static readonly DependencyProperty TitleProperty =
                DependencyProperty.Register("Title", typeof(string), typeof(MetricsChart));

            public string XAxisLabel
            {
                get { return (string)GetValue(XAxisLabelProperty); }
                set { SetValue(XAxisLabelProperty, value); }
            }

            public static readonly DependencyProperty XAxisLabelProperty =
                DependencyProperty.Register("XAxisLabel", typeof(string), typeof(MetricsChart),
                    new PropertyMetadata("Time"));

            public string YAxisLabel
            {
                get { return (string)GetValue(YAxisLabelProperty); }
                set { SetValue(YAxisLabelProperty, value); }
            }

            public static readonly DependencyProperty YAxisLabelProperty =
                DependencyProperty.Register("YAxisLabel", typeof(string), typeof(MetricsChart),
                    new PropertyMetadata("Value"));

            public bool ShowLegend
            {
                get { return (bool)GetValue(ShowLegendProperty); }
                set { SetValue(ShowLegendProperty, value); }
            }

            public static readonly DependencyProperty ShowLegendProperty =
                DependencyProperty.Register("ShowLegend", typeof(bool), typeof(MetricsChart),
                    new PropertyMetadata(true));

            public bool ShowGrid
            {
                get { return (bool)GetValue(ShowGridProperty); }
                set { SetValue(ShowGridProperty, value); }
            }

            public static readonly DependencyProperty ShowGridProperty =
                DependencyProperty.Register("ShowGrid", typeof(bool), typeof(MetricsChart),
                    new PropertyMetadata(true));
        }

        public class DataPoint
        {
            public double X { get; set; }
            public double Y { get; set; }
            public string Label { get; set; }
            public Color Color { get; set; }
            public DateTime Timestamp { get; set; }
        }

        // ============================================================================
        // METRIC GAUGE COMPONENT
        // ============================================================================

        /// <summary>
        /// Circular gauge for displaying single metrics
        /// </summary>
        public class MetricsGauge : Control
        {
            static MetricsGauge()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MetricsGauge),
                    new FrameworkPropertyMetadata(typeof(MetricsGauge)));
            }

            public double Value
            {
                get { return (double)GetValue(ValueProperty); }
                set { SetValue(ValueProperty, value); }
            }

            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register("Value", typeof(double), typeof(MetricsGauge),
                    new PropertyMetadata(0.0));

            public double MinValue
            {
                get { return (double)GetValue(MinValueProperty); }
                set { SetValue(MinValueProperty, value); }
            }

            public static readonly DependencyProperty MinValueProperty =
                DependencyProperty.Register("MinValue", typeof(double), typeof(MetricsGauge),
                    new PropertyMetadata(0.0));

            public double MaxValue
            {
                get { return (double)GetValue(MaxValueProperty); }
                set { SetValue(MaxValueProperty, value); }
            }

            public static readonly DependencyProperty MaxValueProperty =
                DependencyProperty.Register("MaxValue", typeof(double), typeof(MetricsGauge),
                    new PropertyMetadata(100.0));

            public string Unit
            {
                get { return (string)GetValue(UnitProperty); }
                set { SetValue(UnitProperty, value); }
            }

            public static readonly DependencyProperty UnitProperty =
                DependencyProperty.Register("Unit", typeof(string), typeof(MetricsGauge),
                    new PropertyMetadata("%"));

            public string Label
            {
                get { return (string)GetValue(LabelProperty); }
                set { SetValue(LabelProperty, value); }
            }

            public static readonly DependencyProperty LabelProperty =
                DependencyProperty.Register("Label", typeof(string), typeof(MetricsGauge));

            public Color GaugeColor
            {
                get { return (Color)GetValue(GaugeColorProperty); }
                set { SetValue(GaugeColorProperty, value); }
            }

            public static readonly DependencyProperty GaugeColorProperty =
                DependencyProperty.Register("GaugeColor", typeof(Color), typeof(MetricsGauge),
                    new PropertyMetadata(DesignSystemCore.Colors.Primary.Default));
        }

        // ============================================================================
        // ALERTS LIST COMPONENT
        // ============================================================================

        /// <summary>
        /// List component for displaying system alerts
        /// </summary>
        public class AlertsList : Control
        {
            public class Alert
            {
                public enum AlertLevel { Info, Warning, Error, Critical }

                public string Id { get; set; }
                public string Title { get; set; }
                public string Message { get; set; }
                public AlertLevel Level { get; set; }
                public DateTime Timestamp { get; set; }
                public bool IsRead { get; set; }
                public string Action { get; set; }

                public Color GetColor()
                {
                    return Level switch
                    {
                        AlertLevel.Info => DesignSystemCore.Colors.Semantic.Info,
                        AlertLevel.Warning => DesignSystemCore.Colors.Semantic.Warning,
                        AlertLevel.Error => DesignSystemCore.Colors.Semantic.Error,
                        AlertLevel.Critical => Color.FromRgb(192, 0, 0),
                        _ => DesignSystemCore.Colors.Primary.Default
                    };
                }
            }

            static AlertsList()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(AlertsList),
                    new FrameworkPropertyMetadata(typeof(AlertsList)));
            }

            public ObservableCollection<Alert> Alerts
            {
                get { return (ObservableCollection<Alert>)GetValue(AlertsProperty); }
                set { SetValue(AlertsProperty, value); }
            }

            public static readonly DependencyProperty AlertsProperty =
                DependencyProperty.Register("Alerts", typeof(ObservableCollection<Alert>),
                    typeof(AlertsList), new PropertyMetadata(new ObservableCollection<Alert>()));

            public bool GroupByLevel
            {
                get { return (bool)GetValue(GroupByLevelProperty); }
                set { SetValue(GroupByLevelProperty, value); }
            }

            public static readonly DependencyProperty GroupByLevelProperty =
                DependencyProperty.Register("GroupByLevel", typeof(bool), typeof(AlertsList),
                    new PropertyMetadata(true));

            public bool ShowTimestamp
            {
                get { return (bool)GetValue(ShowTimestampProperty); }
                set { SetValue(ShowTimestampProperty, value); }
            }

            public static readonly DependencyProperty ShowTimestampProperty =
                DependencyProperty.Register("ShowTimestamp", typeof(bool), typeof(AlertsList),
                    new PropertyMetadata(true));
        }

        // ============================================================================
        // PROCESS LIST COMPONENT
        // ============================================================================

        /// <summary>
        /// List component for displaying running processes
        /// </summary>
        public class ProcessList : Control
        {
            public class ProcessItem
            {
                public int ProcessId { get; set; }
                public string ProcessName { get; set; }
                public long MemoryBytes { get; set; }
                public float CPUPercent { get; set; }
                public string Status { get; set; }
                public DateTime StartTime { get; set; }

                public string DisplayMemory => FormatBytes(MemoryBytes);

                private string FormatBytes(long bytes)
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

            static ProcessList()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(ProcessList),
                    new FrameworkPropertyMetadata(typeof(ProcessList)));
            }

            public ObservableCollection<ProcessItem> Processes
            {
                get { return (ObservableCollection<ProcessItem>)GetValue(ProcessesProperty); }
                set { SetValue(ProcessesProperty, value); }
            }

            public static readonly DependencyProperty ProcessesProperty =
                DependencyProperty.Register("Processes", typeof(ObservableCollection<ProcessItem>),
                    typeof(ProcessList), new PropertyMetadata(new ObservableCollection<ProcessItem>()));

            public ProcessItem SelectedProcess
            {
                get { return (ProcessItem)GetValue(SelectedProcessProperty); }
                set { SetValue(SelectedProcessProperty, value); }
            }

            public static readonly DependencyProperty SelectedProcessProperty =
                DependencyProperty.Register("SelectedProcess", typeof(ProcessItem), typeof(ProcessList));

            public enum ProcessSortBy { Name, Memory, CPU, StartTime }

            public ProcessSortBy SortBy
            {
                get { return (ProcessSortBy)GetValue(SortByProperty); }
                set { SetValue(SortByProperty, value); }
            }

            public static readonly DependencyProperty SortByProperty =
                DependencyProperty.Register("SortBy", typeof(ProcessSortBy), typeof(ProcessList),
                    new PropertyMetadata(ProcessSortBy.Memory));
        }

        // ============================================================================
        // WIDGET BUILDER COMPONENT
        // ============================================================================

        /// <summary>
        /// Widget builder for creating custom dashboard widgets
        /// </summary>
        public class WidgetBuilder : Control
        {
            public class WidgetTemplate
            {
                public string Id { get; set; }
                public string Name { get; set; }
                public string Description { get; set; }
                public string Icon { get; set; }
                public string Category { get; set; }
                public WidgetType WidgetType { get; set; }
            }

            public enum WidgetType
            {
                Gauge,
                LineChart,
                BarChart,
                PieChart,
                Table,
                List,
                Stat,
                Timeline
            }

            static WidgetBuilder()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(WidgetBuilder),
                    new FrameworkPropertyMetadata(typeof(WidgetBuilder)));
            }

            public ObservableCollection<WidgetTemplate> AvailableTemplates
            {
                get { return (ObservableCollection<WidgetTemplate>)GetValue(AvailableTemplatesProperty); }
                set { SetValue(AvailableTemplatesProperty, value); }
            }

            public static readonly DependencyProperty AvailableTemplatesProperty =
                DependencyProperty.Register("AvailableTemplates", typeof(ObservableCollection<WidgetTemplate>),
                    typeof(WidgetBuilder), new PropertyMetadata(new ObservableCollection<WidgetTemplate>()));

            public WidgetTemplate SelectedTemplate
            {
                get { return (WidgetTemplate)GetValue(SelectedTemplateProperty); }
                set { SetValue(SelectedTemplateProperty, value); }
            }

            public static readonly DependencyProperty SelectedTemplateProperty =
                DependencyProperty.Register("SelectedTemplate", typeof(WidgetTemplate), typeof(WidgetBuilder));

            public bool IsOpen
            {
                get { return (bool)GetValue(IsOpenProperty); }
                set { SetValue(IsOpenProperty, value); }
            }

            public static readonly DependencyProperty IsOpenProperty =
                DependencyProperty.Register("IsOpen", typeof(bool), typeof(WidgetBuilder));
        }
    }
}

// MONADO BLADE v3.4.0 - ADVANCED UI COMPONENTS
// File: src/MonadoBlade.GUI/Components/AdvancedComponents.cs

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MonadoBlade.GUI.Design;

namespace MonadoBlade.GUI.Components
{
    /// <summary>
    /// Advanced UI components with complex interactions
    /// </summary>
    public partial class AdvancedComponents
    {
        // ============================================================================
        // DIALOG / MODAL COMPONENT
        // ============================================================================

        /// <summary>
        /// Modal dialog component with overlay
        /// </summary>
        public class MonadoDialog : Control
        {
            public enum DialogSize { Small, Medium, Large, ExtraLarge }
            public enum DialogResult { None, OK, Cancel, Yes, No }

            static MonadoDialog()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoDialog),
                    new FrameworkPropertyMetadata(typeof(MonadoDialog)));
            }

            public string Title
            {
                get { return (string)GetValue(TitleProperty); }
                set { SetValue(TitleProperty, value); }
            }

            public static readonly DependencyProperty TitleProperty =
                DependencyProperty.Register("Title", typeof(string), typeof(MonadoDialog));

            public object Content
            {
                get { return GetValue(ContentProperty); }
                set { SetValue(ContentProperty, value); }
            }

            public static readonly DependencyProperty ContentProperty =
                DependencyProperty.Register("Content", typeof(object), typeof(MonadoDialog));

            public DialogSize Size
            {
                get { return (DialogSize)GetValue(SizeProperty); }
                set { SetValue(SizeProperty, value); }
            }

            public static readonly DependencyProperty SizeProperty =
                DependencyProperty.Register("Size", typeof(DialogSize), typeof(MonadoDialog),
                    new PropertyMetadata(DialogSize.Medium, OnSizeChanged));

            public bool IsOpen
            {
                get { return (bool)GetValue(IsOpenProperty); }
                set { SetValue(IsOpenProperty, value); }
            }

            public static readonly DependencyProperty IsOpenProperty =
                DependencyProperty.Register("IsOpen", typeof(bool), typeof(MonadoDialog));

            public DialogResult Result
            {
                get { return (DialogResult)GetValue(ResultProperty); }
                set { SetValue(ResultProperty, value); }
            }

            public static readonly DependencyProperty ResultProperty =
                DependencyProperty.Register("Result", typeof(DialogResult), typeof(MonadoDialog));

            private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var dialog = d as MonadoDialog;
                if (dialog != null)
                {
                    ApplySizeStyle(dialog, (DialogSize)e.NewValue);
                }
            }

            private static void ApplySizeStyle(MonadoDialog dialog, DialogSize size)
            {
                switch (size)
                {
                    case DialogSize.Small:
                        dialog.Width = 300;
                        dialog.Height = 200;
                        break;
                    case DialogSize.Medium:
                        dialog.Width = 500;
                        dialog.Height = 350;
                        break;
                    case DialogSize.Large:
                        dialog.Width = 700;
                        dialog.Height = 500;
                        break;
                    case DialogSize.ExtraLarge:
                        dialog.Width = 900;
                        dialog.Height = 650;
                        break;
                }
            }
        }

        // ============================================================================
        // DROPDOWN / COMBO BOX COMPONENT
        // ============================================================================

        /// <summary>
        /// Styled dropdown component
        /// </summary>
        public class MonadoDropdown : ComboBox
        {
            static MonadoDropdown()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoDropdown),
                    new FrameworkPropertyMetadata(typeof(MonadoDropdown)));
            }

            public string Label
            {
                get { return (string)GetValue(LabelProperty); }
                set { SetValue(LabelProperty, value); }
            }

            public static readonly DependencyProperty LabelProperty =
                DependencyProperty.Register("Label", typeof(string), typeof(MonadoDropdown));

            public string Placeholder
            {
                get { return (string)GetValue(PlaceholderProperty); }
                set { SetValue(PlaceholderProperty, value); }
            }

            public static readonly DependencyProperty PlaceholderProperty =
                DependencyProperty.Register("Placeholder", typeof(string), typeof(MonadoDropdown));

            public bool IsSearchable
            {
                get { return (bool)GetValue(IsSearchableProperty); }
                set { SetValue(IsSearchableProperty, value); }
            }

            public static readonly DependencyProperty IsSearchableProperty =
                DependencyProperty.Register("IsSearchable", typeof(bool), typeof(MonadoDropdown),
                    new PropertyMetadata(true));

            public int MaxDropdownHeight
            {
                get { return (int)GetValue(MaxDropdownHeightProperty); }
                set { SetValue(MaxDropdownHeightProperty, value); }
            }

            public static readonly DependencyProperty MaxDropdownHeightProperty =
                DependencyProperty.Register("MaxDropdownHeight", typeof(int), typeof(MonadoDropdown),
                    new PropertyMetadata(300));
        }

        // ============================================================================
        // TABS COMPONENT
        // ============================================================================

        /// <summary>
        /// Tab control with animation support
        /// </summary>
        public class MonadoTabs : TabControl
        {
            public enum TabAlignment { Top, Bottom, Left, Right }

            static MonadoTabs()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoTabs),
                    new FrameworkPropertyMetadata(typeof(MonadoTabs)));
            }

            public TabAlignment Alignment
            {
                get { return (TabAlignment)GetValue(AlignmentProperty); }
                set { SetValue(AlignmentProperty, value); }
            }

            public static readonly DependencyProperty AlignmentProperty =
                DependencyProperty.Register("Alignment", typeof(TabAlignment), typeof(MonadoTabs),
                    new PropertyMetadata(TabAlignment.Top, OnAlignmentChanged));

            public bool AnimateTabSwitch
            {
                get { return (bool)GetValue(AnimateTabSwitchProperty); }
                set { SetValue(AnimateTabSwitchProperty, value); }
            }

            public static readonly DependencyProperty AnimateTabSwitchProperty =
                DependencyProperty.Register("AnimateTabSwitch", typeof(bool), typeof(MonadoTabs),
                    new PropertyMetadata(true));

            public Thickness TabPadding
            {
                get { return (Thickness)GetValue(TabPaddingProperty); }
                set { SetValue(TabPaddingProperty, value); }
            }

            public static readonly DependencyProperty TabPaddingProperty =
                DependencyProperty.Register("TabPadding", typeof(Thickness), typeof(MonadoTabs),
                    new PropertyMetadata(new Thickness(DesignSystemCore.Spacing.MD)));

            private static void OnAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var tabs = d as MonadoTabs;
                if (tabs != null)
                {
                    ApplyAlignmentStyle(tabs, (TabAlignment)e.NewValue);
                }
            }

            private static void ApplyAlignmentStyle(MonadoTabs tabs, TabAlignment alignment)
            {
                switch (alignment)
                {
                    case TabAlignment.Top:
                        tabs.TabStripPlacement = Dock.Top;
                        break;
                    case TabAlignment.Bottom:
                        tabs.TabStripPlacement = Dock.Bottom;
                        break;
                    case TabAlignment.Left:
                        tabs.TabStripPlacement = Dock.Left;
                        break;
                    case TabAlignment.Right:
                        tabs.TabStripPlacement = Dock.Right;
                        break;
                }
            }
        }

        // ============================================================================
        // NAVIGATION DRAWER COMPONENT
        // ============================================================================

        /// <summary>
        /// Side navigation drawer with collapsible support
        /// </summary>
        public class MonadoNavigationDrawer : Control
        {
            public class NavigationItem
            {
                public string Id { get; set; }
                public string Label { get; set; }
                public string Icon { get; set; }
                public bool IsActive { get; set; }
                public ObservableCollection<NavigationItem> Children { get; set; }

                public NavigationItem()
                {
                    Children = new ObservableCollection<NavigationItem>();
                }
            }

            static MonadoNavigationDrawer()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoNavigationDrawer),
                    new FrameworkPropertyMetadata(typeof(MonadoNavigationDrawer)));
            }

            public ObservableCollection<NavigationItem> Items
            {
                get { return (ObservableCollection<NavigationItem>)GetValue(ItemsProperty); }
                set { SetValue(ItemsProperty, value); }
            }

            public static readonly DependencyProperty ItemsProperty =
                DependencyProperty.Register("Items", typeof(ObservableCollection<NavigationItem>),
                    typeof(MonadoNavigationDrawer), new PropertyMetadata(new ObservableCollection<NavigationItem>()));

            public bool IsCollapsed
            {
                get { return (bool)GetValue(IsCollapsedProperty); }
                set { SetValue(IsCollapsedProperty, value); }
            }

            public static readonly DependencyProperty IsCollapsedProperty =
                DependencyProperty.Register("IsCollapsed", typeof(bool), typeof(MonadoNavigationDrawer),
                    new PropertyMetadata(false));

            public NavigationItem SelectedItem
            {
                get { return (NavigationItem)GetValue(SelectedItemProperty); }
                set { SetValue(SelectedItemProperty, value); }
            }

            public static readonly DependencyProperty SelectedItemProperty =
                DependencyProperty.Register("SelectedItem", typeof(NavigationItem), typeof(MonadoNavigationDrawer));
        }

        // ============================================================================
        // TOOLTIP COMPONENT
        // ============================================================================

        /// <summary>
        /// Enhanced tooltip with positioning and animations
        /// </summary>
        public class MonadoTooltip : ToolTip
        {
            public enum TooltipPosition { Top, Bottom, Left, Right }

            static MonadoTooltip()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoTooltip),
                    new FrameworkPropertyMetadata(typeof(MonadoTooltip)));
            }

            public TooltipPosition Position
            {
                get { return (TooltipPosition)GetValue(PositionProperty); }
                set { SetValue(PositionProperty, value); }
            }

            public static readonly DependencyProperty PositionProperty =
                DependencyProperty.Register("Position", typeof(TooltipPosition), typeof(MonadoTooltip),
                    new PropertyMetadata(TooltipPosition.Top));

            public int ShowDelay
            {
                get { return (int)GetValue(ShowDelayProperty); }
                set { SetValue(ShowDelayProperty, value); }
            }

            public static readonly DependencyProperty ShowDelayProperty =
                DependencyProperty.Register("ShowDelay", typeof(int), typeof(MonadoTooltip),
                    new PropertyMetadata(300));
        }

        // ============================================================================
        // MENU COMPONENT
        // ============================================================================

        /// <summary>
        /// Styled menu with support for items and separators
        /// </summary>
        public class MonadoMenu : Menu
        {
            public class MenuItem : System.Windows.Controls.MenuItem
            {
                public enum ItemType { Normal, Separator, Divider }

                static MenuItem()
                {
                    DefaultStyleKeyProperty.OverrideMetadata(
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(typeof(MenuItem)));
                }

                public ItemType Type
                {
                    get { return (ItemType)GetValue(TypeProperty); }
                    set { SetValue(TypeProperty, value); }
                }

                public static readonly DependencyProperty TypeProperty =
                    DependencyProperty.Register("Type", typeof(ItemType), typeof(MenuItem),
                        new PropertyMetadata(ItemType.Normal));

                public string Icon
                {
                    get { return (string)GetValue(IconProperty); }
                    set { SetValue(IconProperty, value); }
                }

                public static readonly DependencyProperty IconProperty =
                    DependencyProperty.Register("Icon", typeof(string), typeof(MenuItem));

                public string Shortcut
                {
                    get { return (string)GetValue(ShortcutProperty); }
                    set { SetValue(ShortcutProperty, value); }
                }

                public static readonly DependencyProperty ShortcutProperty =
                    DependencyProperty.Register("Shortcut", typeof(string), typeof(MenuItem));
            }

            static MonadoMenu()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoMenu),
                    new FrameworkPropertyMetadata(typeof(MonadoMenu)));
            }
        }

        // ============================================================================
        // SPINNER / LOADING COMPONENT
        // ============================================================================

        /// <summary>
        /// Animated spinner for loading states
        /// </summary>
        public class MonadoSpinner : Control
        {
            public enum SpinnerSize { Small, Medium, Large }

            static MonadoSpinner()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoSpinner),
                    new FrameworkPropertyMetadata(typeof(MonadoSpinner)));
            }

            public SpinnerSize Size
            {
                get { return (SpinnerSize)GetValue(SizeProperty); }
                set { SetValue(SizeProperty, value); }
            }

            public static readonly DependencyProperty SizeProperty =
                DependencyProperty.Register("Size", typeof(SpinnerSize), typeof(MonadoSpinner),
                    new PropertyMetadata(SpinnerSize.Medium, OnSizeChanged));

            public Color SpinnerColor
            {
                get { return (Color)GetValue(SpinnerColorProperty); }
                set { SetValue(SpinnerColorProperty, value); }
            }

            public static readonly DependencyProperty SpinnerColorProperty =
                DependencyProperty.Register("SpinnerColor", typeof(Color), typeof(MonadoSpinner),
                    new PropertyMetadata(DesignSystemCore.Colors.Primary.Default));

            private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var spinner = d as MonadoSpinner;
                if (spinner != null)
                {
                    ApplySizeStyle(spinner, (SpinnerSize)e.NewValue);
                }
            }

            private static void ApplySizeStyle(MonadoSpinner spinner, SpinnerSize size)
            {
                switch (size)
                {
                    case SpinnerSize.Small:
                        spinner.Width = 24;
                        spinner.Height = 24;
                        break;
                    case SpinnerSize.Medium:
                        spinner.Width = 48;
                        spinner.Height = 48;
                        break;
                    case SpinnerSize.Large:
                        spinner.Width = 64;
                        spinner.Height = 64;
                        break;
                }
            }
        }
    }
}

using CommonPluginsShared;
using CommonPluginsShared.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Display settings for the integration progress bar (colors, time placement).
    /// Owns color-picker and restore handlers; writes onto the editing settings instance.
    /// </summary>
    public partial class HltbDisplayProgressBarSettingsSection : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();

        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private HowLongToBeatSettings _settings;
        private TextBlock _activeColorPreview;
        private string _activeColorTag;
        private bool _wired;
        private bool _updatingTimePlacement;

        /// <summary>
        /// Initializes a new instance of the <see cref="HltbDisplayProgressBarSettingsSection"/> class.
        /// </summary>
        public HltbDisplayProgressBarSettingsSection()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Binds handlers and loads color previews from the editing settings instance.
        /// </summary>
        /// <param name="settings">Settings currently being edited.</param>
        /// <param name="dataContext">Host view model so bindings resolve before the section is parented.</param>
        public void Initialize(HowLongToBeatSettings settings, object dataContext)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (dataContext != null)
            {
                DataContext = dataContext;
            }

            EnsureTimePlacementDefault();
            WireHandlers();
            LoadColorPreviews();

            Loaded -= HltbDisplayProgressBarSettingsSection_Loaded;
            Loaded += HltbDisplayProgressBarSettingsSection_Loaded;
        }

        private void HltbDisplayProgressBarSettingsSection_Loaded(object sender, RoutedEventArgs e)
        {
            // Expander content may not be walkable until loaded; re-wire if the first pass found no buttons.
            WireHandlers();
            SyncTimePlacementRadiosFromSettings();
        }

        private void EnsureTimePlacementDefault()
        {
            if (_settings == null)
            {
                return;
            }

            if (!_settings.ProgressBarShowTimeAbove
                && !_settings.ProgressBarShowTimeInterior
                && !_settings.ProgressBarShowTimeBelow)
            {
                _settings.ProgressBarShowTimeInterior = true;
            }
        }

        private void WireHandlers()
        {
            if (!_wired)
            {
                PART_TM_ColorOK.Click += PART_TM_ColorOK_Click;
                PART_TM_ColorCancel.Click += PART_TM_ColorCancel_Click;
                HltB_IntegrationProgressBarShowTime.Checked += HltB_IntegrationProgressBarShowTime_Checked;
                HltB_IntegrationProgressBarShowTime.Unchecked += HltB_IntegrationProgressBarShowTime_Unchecked;

                PART_ProgressBarTimeAbove.Checked += HltB_ProgressBarTimeAbove_Checked;
                PART_ProgressBarTimeInterior.Checked += HltB_ProgressBarTimeInterior_Checked;
                PART_ProgressBarTimeBelow.Checked += HltB_ProgressBarTimeBelow_Checked;

                _wired = true;
            }

            WireColorButtons();
        }

        private void WireColorButtons()
        {
            foreach (Button button in EnumerateLogicalButtons(this))
            {
                if (IsPickColorButton(button))
                {
                    button.Click -= BtPickColor_Click;
                    button.Click += BtPickColor_Click;
                }
                else if (IsRestoreColorButton(button))
                {
                    button.Click -= BtRestore_Click;
                    button.Click += BtRestore_Click;
                }
            }
        }

        private static bool IsPickColorButton(Button button)
        {
            if (button?.Tag == null)
            {
                return false;
            }

            string content = button.Content as string ?? button.Content?.ToString();
            return content == "\u270f";
        }

        private static bool IsRestoreColorButton(Button button)
        {
            return button?.Tag is string && !IsPickColorButton(button);
        }

        private void LoadColorPreviews()
        {
            PART_SelectorColorPicker.OnlySimpleColor = false;

            tbThumb.Background = ResolveProgressColorBrush(_settings.ThumbSolidColorBrush, _settings.ThumbLinearGradient);
            tbColorFirst.Background = ResolveProgressColorBrush(_settings.FirstColorBrush, _settings.FirstLinearGradient);
            tbColorSecond.Background = ResolveProgressColorBrush(_settings.SecondColorBrush, _settings.SecondLinearGradient);
            tbColorThird.Background = ResolveProgressColorBrush(_settings.ThirdColorBrush, _settings.ThirdLinearGradient);
            tbColorFirstMulti.Background = ResolveProgressColorBrush(_settings.FirstMultiColorBrush, _settings.FirstMultiLinearGradient);
            tbColorSecondMulti.Background = ResolveProgressColorBrush(_settings.SecondMultiColorBrush, _settings.SecondMultiLinearGradient);
            tbColorThirdMulti.Background = ResolveProgressColorBrush(_settings.ThirdMultiColorBrush, _settings.ThirdMultiLinearGradient);

            spSettings.Visibility = Visibility.Visible;
        }

        private void BtPickColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                string tag = button?.Tag as string;
                if (string.IsNullOrEmpty(tag))
                {
                    return;
                }

                // StackPanel has a label TextBlock then the color swatch — never use FirstOrDefault().
                _activeColorTag = tag;
                _activeColorPreview = GetProgressColorPreviewBlock(tag);
                if (_activeColorPreview == null)
                {
                    return;
                }

                LoadPickerFromColorSlot(tag);

                PART_SelectorColor.Visibility = Visibility.Visible;
                spSettings.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        /// <summary>
        /// Seeds the color picker from the editing settings slot (fallback: swatch Background).
        /// </summary>
        private void LoadPickerFromColorSlot(string tag)
        {
            SolidColorBrush solid;
            ThemeLinearGradient gradient;
            ResolveColorSlot(tag, out solid, out gradient);

            LinearGradientBrush linear = gradient?.ToLinearGradientBrush;
            if (linear != null)
            {
                PART_SelectorColorPicker.SetColors(linear);
                return;
            }

            if (solid != null)
            {
                PART_SelectorColorPicker.SetColors(solid);
                return;
            }

            if (_activeColorPreview?.Background is LinearGradientBrush previewGradient)
            {
                PART_SelectorColorPicker.SetColors(previewGradient);
                return;
            }

            if (_activeColorPreview?.Background is SolidColorBrush previewSolid)
            {
                PART_SelectorColorPicker.SetColors(previewSolid);
                return;
            }

            PART_SelectorColorPicker.SetColors(Colors.DarkCyan);
        }

        private void ResolveColorSlot(string tag, out SolidColorBrush solid, out ThemeLinearGradient gradient)
        {
            solid = null;
            gradient = null;
            if (_settings == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            switch (tag)
            {
                case "0":
                    solid = _settings.ThumbSolidColorBrush;
                    gradient = _settings.ThumbLinearGradient;
                    break;
                case "1":
                    solid = _settings.FirstColorBrush;
                    gradient = _settings.FirstLinearGradient;
                    break;
                case "2":
                    solid = _settings.SecondColorBrush;
                    gradient = _settings.SecondLinearGradient;
                    break;
                case "3":
                    solid = _settings.ThirdColorBrush;
                    gradient = _settings.ThirdLinearGradient;
                    break;
                case "4":
                    solid = _settings.FirstMultiColorBrush;
                    gradient = _settings.FirstMultiLinearGradient;
                    break;
                case "5":
                    solid = _settings.SecondMultiColorBrush;
                    gradient = _settings.SecondMultiLinearGradient;
                    break;
                case "6":
                    solid = _settings.ThirdMultiColorBrush;
                    gradient = _settings.ThirdMultiLinearGradient;
                    break;
                default:
                    break;
            }
        }

        private void BtRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tag = (string)((Button)sender).Tag;

                switch (tag)
                {
                    case "0":
                        if (ResourceProvider.GetResource("NormalBrush") is LinearGradientBrush thumbGradient)
                        {
                            SetProgressColorSlot(tag, null, ThemeLinearGradient.ToThemeLinearGradient(thumbGradient), thumbGradient);
                        }
                        else
                        {
                            SolidColorBrush thumbSolid = (SolidColorBrush)ResourceProvider.GetResource("NormalBrush");
                            SetProgressColorSlot(tag, thumbSolid, null, thumbSolid);
                        }
                        break;

                    case "1":
                    case "4":
                        SetProgressColorSlot(tag, new SolidColorBrush(Brushes.DarkCyan.Color), null, Brushes.DarkCyan);
                        break;

                    case "2":
                    case "5":
                        SetProgressColorSlot(tag, new SolidColorBrush(Brushes.RoyalBlue.Color), null, Brushes.RoyalBlue);
                        break;

                    case "3":
                    case "6":
                        SetProgressColorSlot(tag, new SolidColorBrush(Brushes.ForestGreen.Color), null, Brushes.ForestGreen);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void PART_TM_ColorOK_Click(object sender, RoutedEventArgs e)
        {
            string tag = _activeColorTag;
            if (string.IsNullOrEmpty(tag) && _activeColorPreview != null)
            {
                tag = _activeColorPreview.Tag as string;
            }

            if (string.IsNullOrEmpty(tag))
            {
                Logger.Warn("One control is undefined");
                PART_SelectorColor.Visibility = Visibility.Collapsed;
                spSettings.Visibility = Visibility.Visible;
                return;
            }

            if (PART_SelectorColorPicker.IsSimpleColor)
            {
                Color color = PART_SelectorColorPicker.SimpleColor;
                SolidColorBrush solid = new SolidColorBrush(color);
                SetProgressColorSlot(tag, solid, null, solid);
            }
            else
            {
                LinearGradientBrush gradientBrush = PART_SelectorColorPicker.GetLinearGradientBrush();
                SetProgressColorSlot(tag, null, ThemeLinearGradient.ToThemeLinearGradient(gradientBrush), gradientBrush);
            }

            PART_SelectorColor.Visibility = Visibility.Collapsed;
            spSettings.Visibility = Visibility.Visible;
            _activeColorTag = null;
        }

        private void PART_TM_ColorCancel_Click(object sender, RoutedEventArgs e)
        {
            PART_SelectorColor.Visibility = Visibility.Collapsed;
            spSettings.Visibility = Visibility.Visible;
        }

        private void SetProgressColorSlot(string tag, SolidColorBrush solid, ThemeLinearGradient gradient, Brush preview)
        {
            if (_settings == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            switch (tag)
            {
                case "0":
                    _settings.ThumbSolidColorBrush = solid;
                    _settings.ThumbLinearGradient = gradient;
                    break;
                case "1":
                    _settings.FirstColorBrush = solid;
                    _settings.FirstLinearGradient = gradient;
                    break;
                case "2":
                    _settings.SecondColorBrush = solid;
                    _settings.SecondLinearGradient = gradient;
                    break;
                case "3":
                    _settings.ThirdColorBrush = solid;
                    _settings.ThirdLinearGradient = gradient;
                    break;
                case "4":
                    _settings.FirstMultiColorBrush = solid;
                    _settings.FirstMultiLinearGradient = gradient;
                    break;
                case "5":
                    _settings.SecondMultiColorBrush = solid;
                    _settings.SecondMultiLinearGradient = gradient;
                    break;
                case "6":
                    _settings.ThirdMultiColorBrush = solid;
                    _settings.ThirdMultiLinearGradient = gradient;
                    break;
                default:
                    return;
            }

            TextBlock previewBlock = GetProgressColorPreviewBlock(tag);
            if (previewBlock != null)
            {
                previewBlock.Background = preview ?? ResolveProgressColorBrush(solid, gradient);
            }
        }

        private static Brush ResolveProgressColorBrush(SolidColorBrush solid, ThemeLinearGradient gradient)
        {
            LinearGradientBrush linear = gradient?.ToLinearGradientBrush;
            if (linear != null)
            {
                return linear;
            }

            return solid;
        }

        private TextBlock GetProgressColorPreviewBlock(string tag)
        {
            switch (tag)
            {
                case "0":
                    return tbThumb;
                case "1":
                    return tbColorFirst;
                case "2":
                    return tbColorSecond;
                case "3":
                    return tbColorThird;
                case "4":
                    return tbColorFirstMulti;
                case "5":
                    return tbColorSecondMulti;
                case "6":
                    return tbColorThirdMulti;
                default:
                    return null;
            }
        }

        private void HltB_IntegrationProgressBarShowTime_Checked(object sender, RoutedEventArgs e)
        {
            if (_settings != null)
            {
                _settings.ProgressBarShowTime = true;
            }
        }

        private void HltB_IntegrationProgressBarShowTime_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_settings != null)
            {
                _settings.ProgressBarShowTime = false;
            }
        }

        private void HltB_ProgressBarTimeAbove_Checked(object sender, RoutedEventArgs e)
        {
            ApplyTimePlacement(above: true, interior: false, below: false);
        }

        private void HltB_ProgressBarTimeInterior_Checked(object sender, RoutedEventArgs e)
        {
            ApplyTimePlacement(above: false, interior: true, below: false);
        }

        private void HltB_ProgressBarTimeBelow_Checked(object sender, RoutedEventArgs e)
        {
            ApplyTimePlacement(above: false, interior: false, below: true);
        }

        private void ApplyTimePlacement(bool above, bool interior, bool below)
        {
            if (_updatingTimePlacement || _settings == null)
            {
                return;
            }

            _updatingTimePlacement = true;
            try
            {
                _settings.ProgressBarShowTimeAbove = above;
                _settings.ProgressBarShowTimeInterior = interior;
                _settings.ProgressBarShowTimeBelow = below;

                PART_ProgressBarTimeAbove.IsChecked = above;
                PART_ProgressBarTimeInterior.IsChecked = interior;
                PART_ProgressBarTimeBelow.IsChecked = below;
            }
            finally
            {
                _updatingTimePlacement = false;
            }
        }

        private void SyncTimePlacementRadiosFromSettings()
        {
            if (_settings == null)
            {
                return;
            }

            EnsureTimePlacementDefault();
            ApplyTimePlacement(
                _settings.ProgressBarShowTimeAbove,
                _settings.ProgressBarShowTimeInterior,
                _settings.ProgressBarShowTimeBelow);
        }

        /// <summary>
        /// Walks the logical tree (including collapsed Expander content) so color buttons
        /// are found before the expander is expanded in the visual tree.
        /// </summary>
        private static IEnumerable<Button> EnumerateLogicalButtons(DependencyObject root)
        {
            if (root == null)
            {
                yield break;
            }

            if (root is Button rootButton)
            {
                yield return rootButton;
            }

            if (root is Expander expander && expander.Content is DependencyObject expanderContent)
            {
                foreach (Button nested in EnumerateLogicalButtons(expanderContent))
                {
                    yield return nested;
                }
            }

            foreach (object child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is DependencyObject dep)
                {
                    foreach (Button nested in EnumerateLogicalButtons(dep))
                    {
                        yield return nested;
                    }
                }
            }
        }
    }
}

using HowLongToBeat.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Settings section for game-name aliases (grid scroll + selection follow-up).
    /// </summary>
    public partial class HltbDataSettingsSection : UserControl
    {
        private HowLongToBeatSettingsViewModel _viewModel;
        private bool _wired;

        /// <summary>
        /// Initializes a new instance of the <see cref="HltbDataSettingsSection"/> class.
        /// </summary>
        public HltbDataSettingsSection()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Hydrates the aliases list when empty and wires grid scroll / selection UX.
        /// </summary>
        /// <param name="viewModel">Settings view model hosting alias commands and selection.</param>
        public void Initialize(HowLongToBeatSettingsViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            HydrateAliases(_viewModel.Settings);
            WireHandlers();
        }

        /// <summary>
        /// Unsubscribes view-model listeners when the host settings view unloads.
        /// </summary>
        public void Detach()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        private static void HydrateAliases(HowLongToBeatSettings settings)
        {
            try
            {
                if (settings == null)
                {
                    return;
                }

                if ((settings.GameNameAliases == null || settings.GameNameAliases.Count == 0)
                    && (settings.GameNameAliasesList == null || settings.GameNameAliasesList.Count == 0))
                {
                    settings.GameNameAliases = GameNameAliases.GetDefaultPokemonAliases() ?? new Dictionary<string, string>();
                }

                settings.SyncAliasesListFromDictionary();
            }
            catch
            {
            }
        }

        private void WireHandlers()
        {
            if (_wired)
            {
                return;
            }

            try
            {
                PART_AliasesGrid.PreviewMouseWheel += PART_AliasesGrid_PreviewMouseWheel;
            }
            catch
            {
            }

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            _wired = true;
        }

        private void PART_AliasesGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            try
            {
                DependencyObject dep = (DependencyObject)sender;
                while (dep != null && !(dep is ScrollViewer))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                ScrollViewer sv = dep as ScrollViewer;
                if (sv != null)
                {
                    double newOffset = sv.VerticalOffset - (e.Delta / 3.0);
                    newOffset = Math.Max(0, Math.Min(newOffset, sv.ScrollableHeight));
                    sv.ScrollToVerticalOffset(newOffset);
                    e.Handled = true;
                }
            }
            catch
            {
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(HowLongToBeatSettingsViewModel.SelectedAliasEntry)
                || _viewModel?.SelectedAliasEntry == null
                || PART_AliasesGrid == null)
            {
                return;
            }

            try
            {
                PART_AliasesGrid.ScrollIntoView(_viewModel.SelectedAliasEntry);
                PART_AliasesGrid.Focus();
            }
            catch
            {
            }
        }
    }
}

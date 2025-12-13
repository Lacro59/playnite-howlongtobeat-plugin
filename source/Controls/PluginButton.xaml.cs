using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HowLongToBeat.Controls
{
    public partial class PluginButton : PluginUserControlExtend
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginButtonDataContext ControlDataContext = new PluginButtonDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = value as PluginButtonDataContext
                ?? throw new InvalidCastException($"Expected {nameof(PluginButtonDataContext)} but got {value?.GetType().FullName ?? "<null>"}");
        }

        private bool eventsWired;
        private volatile bool _isUnloading = false;
        private CancellationTokenSource _loadedCts;
        private int _initGate = 0;

        public PluginButton()
        {
            AlwaysShow = true;

            InitializeComponent();
            this.DataContext = ControlDataContext;

            this.Loaded += PluginButton_Loaded;
            this.Unloaded += PluginButton_Unloaded;
        }

        private void PluginButton_Unloaded(object sender, RoutedEventArgs e)
        {
            // Mark unloading to prevent a queued Dispatcher.InvokeAsync from re-wiring events
            _isUnloading = true;
            // Cancel any pending load wait
            try { _loadedCts?.Cancel(); } catch { }
            try { _loadedCts?.Dispose(); } catch { }
            _loadedCts = null;

            if (!eventsWired) return;
            eventsWired = false;

            if (PluginDatabase?.PluginSettings != null)
            {
                PluginDatabase.PluginSettings.PropertyChanged -= PluginSettings_PropertyChanged;
            }

            if (PluginDatabase?.Database != null)
            {
                PluginDatabase.Database.ItemUpdated -= Database_ItemUpdated;
                PluginDatabase.Database.ItemCollectionChanged -= Database_ItemCollectionChanged;
            }

            if (API.Instance?.Database?.Games != null)
            {
                API.Instance.Database.Games.ItemUpdated -= Games_ItemUpdated;
            }
        }

        private async void PluginButton_Loaded(object sender, RoutedEventArgs e)
        {
            // Clear unloading marker — we're now loading
            _isUnloading = false;
            // If already wired, nothing to do
            if (eventsWired) return;

            var localCts = new CancellationTokenSource();

            if (System.Threading.Interlocked.CompareExchange(ref _initGate, 1, 0) != 0)
            {
                try { localCts.Dispose(); } catch { }
                return;
            }

            _loadedCts = localCts;

            bool initialized = false;
            try
            {
                var sw = Stopwatch.StartNew();
                var token = localCts.Token;
                while (!PluginDatabase.IsLoaded)
                {
                    try { await Task.Delay(100, token).ConfigureAwait(false); } catch (OperationCanceledException) { return; }
                    if (sw.Elapsed > TimeSpan.FromSeconds(30))
                    {
                        try
                        {
                            if (PluginDatabase?.PluginSettings?.Settings?.EnableVerboseLogging == true)
                            {
                                Debug.WriteLine("PluginButton init timeout waiting for PluginDatabase.IsLoaded");
                            }
                        }
                        catch { }
                        return;
                    }
                }

                try { localCts.Dispose(); } catch { }
                try { if (_loadedCts == localCts) _loadedCts = null; } catch { }

                await this.Dispatcher.InvokeAsync((Action)(() =>
                {
                    // Ensure the control is still loaded and not in the middle of unloading to avoid re-wiring after Unloaded.
                    if (!this.IsLoaded || _isUnloading) return;

                    if (!eventsWired)
                    {
                        try { PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged; } catch { }
                        try { PluginDatabase.Database.ItemUpdated += Database_ItemUpdated; } catch { }
                        try { PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged; } catch { }
                        try { API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated; } catch { }
                        eventsWired = true;
                    }

                    try { PluginSettings_PropertyChanged(null, null); } catch { }
                })).Task.ConfigureAwait(false);

                initialized = true;
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _initGate, 0);

                if (!initialized)
                {
                    try
                    {
                        if (_loadedCts == localCts)
                        {
                            try { localCts.Dispose(); } catch { }
                            _loadedCts = null;
                        }
                    }
                    catch { }
                }
            }
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton;
            ControlDataContext.Text = "\ue90d";
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;
        }


        #region Events
        private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
        {
            GameHowLongToBeat gameHowLongToBeat = PluginDatabase.Get(GameContext);

            if (gameHowLongToBeat.HasData)
            {
                HowLongToBeatView ViewExtension = new HowLongToBeatView(gameHowLongToBeat);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PluginName, ViewExtension);
                _ = windowExtension.ShowDialog();
            }
        }
        #endregion
    }


    public class PluginButtonDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private string text = "\ue90d";
        public string Text { get => text; set => SetValue(ref text, value); }
    }
}

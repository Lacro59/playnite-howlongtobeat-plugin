using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HowLongToBeat.Controls
{
    public partial class PluginViewItem : PluginUserControlExtend
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginViewItemDataContext ControlDataContext = new PluginViewItemDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = value as PluginViewItemDataContext
                ?? throw new InvalidCastException($"Expected {nameof(PluginViewItemDataContext)}, got {value?.GetType().FullName ?? "null"}");
        }

        private CancellationTokenSource _loadedCts;
        private bool _eventsWired;

        public PluginViewItem()
        {
            InitializeComponent();
            this.DataContext = ControlDataContext;

            // Use async Loaded handler to perform awaited initialization without compiler warnings
            this.Loaded += PluginViewItem_Loaded;
            this.Unloaded += PluginViewItem_Unloaded;
        }

        private async void PluginViewItem_Loaded(object sender, EventArgs e)
        {
            // If already initialized, nothing to do
            if (_eventsWired) return;

            // Create CTS tied to this control's lifetime
            _loadedCts?.Dispose();
            _loadedCts = new CancellationTokenSource();

            const int PluginLoadTimeoutSeconds = 10;
            try
            {
                // Wait for PluginDatabase to be loaded with timeout and cancellation support
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(PluginLoadTimeoutSeconds)))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(_loadedCts.Token, timeoutCts.Token))
                {
                    while (!PluginDatabase.IsLoaded)
                    {
                        if (linked.Token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }
                        await Task.Delay(100, linked.Token).ConfigureAwait(false);
                    }
                }

                // Ensure the registration runs on the UI thread and await its completion
                await this.Dispatcher.InvokeAsync((Action)delegate
                {
                    try { PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged; }
                    catch (Exception ex) { try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { } }
                    try { PluginDatabase.Database.ItemUpdated += Database_ItemUpdated; }
                    catch (Exception ex) { try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { } }
                    try { PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged; }
                    catch (Exception ex) { try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { } }
                    try { API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated; }
                    catch (Exception ex) { try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { } }

                    // Apply settings
                    try { PluginSettings_PropertyChanged(null, null); }
                    catch (Exception ex) { try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { } }
                }).Task.ConfigureAwait(false);

                // Mark as initialized so subsequent Loaded events skip initialization until Unloaded resets it
                _eventsWired = true;
            }
            catch (OperationCanceledException)
            {
                // Timeout or control unloaded before initialization completed - bail out gracefully
                try { Common.LogDebug(true, "PluginViewItem initialization cancelled or timed out"); } catch { }
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
            }
        }

        private void PluginViewItem_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Cancel any pending initialization waits
                _loadedCts?.Cancel();
            }
            catch { }
            finally
            {
                try { _loadedCts?.Dispose(); } catch { }
                _loadedCts = null;
            }

            // If we previously wired events, unsubscribe them so control can be reinitialized later
            if (_eventsWired)
            {
                Action unsub = () =>
                {
                    try { PluginDatabase.PluginSettings.PropertyChanged -= PluginSettings_PropertyChanged; } catch { }
                    try { PluginDatabase.Database.ItemUpdated -= Database_ItemUpdated; } catch { }
                    try { PluginDatabase.Database.ItemCollectionChanged -= Database_ItemCollectionChanged; } catch { }
                    try { API.Instance.Database.Games.ItemUpdated -= Games_ItemUpdated; } catch { }
                };

                if (this.Dispatcher.CheckAccess())
                {
                    try { unsub(); } catch { }
                }
                else
                {
                    try { this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, unsub); } catch { }
                }

                _eventsWired = false;
            }
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;
            ControlDataContext.Text = string.Empty;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;

            if (gameHowLongToBeat?.GetData().GameHltbData != null)
            {
                PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();
                PlayTimeFormat playTimeFormat = PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat;
                ControlDataContext.Text = (string)converter.Convert(gameHowLongToBeat.GetData().GameHltbData.TimeToBeat, null, playTimeFormat, CultureInfo.CurrentCulture);
            }
        }
    }


    public class PluginViewItemDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private string text = "1h 18m";
        public string Text { get => text; set => SetValue(ref text, value); }
    }
}

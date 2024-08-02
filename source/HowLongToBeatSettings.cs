using CommonPluginsShared.Models;
using FuzzySharp;
using HowLongToBeat.Models;
using HowLongToBeat.Models.StartPage;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace HowLongToBeat
{
    public class HowLongToBeatSettings : ObservableObject
    {
        #region Settings variables
        public bool MenuInExtensions { get; set; } = true;
        public DateTime LastAutoLibUpdateAssetsDownload { get; set; } = DateTime.Now;

        public bool EnableIntegrationButtonHeader { get; set; } = false;
        public bool EnableIntegrationButtonSide { get; set; } = true;

        public string UserLogin { get; set; } = string.Empty;

        public bool EnableTag { get; set; } = false;

        public bool ShowHltbImg { get; set; } = true;

        public bool AutoSetCurrentPlayTime { get; set; } = false;
        public bool AutoSetCurrentPlayTimeWithoutConfirmation { get; set; } = true;

        public bool AutoImport { get; set; } = true;
        public bool AutoAccept { get; set; } = true;
        public bool ShowWhenMismatch { get; set; } = false;
        public bool UseMatchValue { get; set; } = false;
        public double MatchValue { get; set; } = 95;

        public bool EnableSucessNotification { get; set; } = true;

        public bool EnableProgressBarInDataView { get; set; } = true;

        private bool enableIntegrationViewItem = true;
        public bool EnableIntegrationViewItem { get => enableIntegrationViewItem; set => SetValue(ref enableIntegrationViewItem, value); }

        public bool IntegrationViewItemOnlyHour { get; set; } = false;

        private bool enableIntegrationButton = true;
        public bool EnableIntegrationButton { get => enableIntegrationButton; set => SetValue(ref enableIntegrationButton, value); }

        private bool enableIntegrationProgressBar = true;
        public bool EnableIntegrationProgressBar { get => enableIntegrationProgressBar; set => SetValue(ref enableIntegrationProgressBar, value); }

        public bool ProgressBarShowToolTip { get; set; } = true;
        public bool ProgressBarShowTimeAbove { get; set; } = false;
        public bool ProgressBarShowTimeInterior { get; set; } = true;
        public bool ProgressBarShowTimeBelow { get; set; } = false;

        public bool ProgressBarShowTimeUser { get; set; } = false;
        public bool ProgressBarShowTime { get; set; } = false;


        public SolidColorBrush ThumbSolidColorBrush { get; set; } = null;
        public ThemeLinearGradient ThumbLinearGradient { get; set; } = null;

        public SolidColorBrush FirstColorBrush { get; set; } = new SolidColorBrush(Brushes.DarkCyan.Color);
        public ThemeLinearGradient FirstLinearGradient { get; set; } = null;

        public SolidColorBrush SecondColorBrush { get; set; } = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public ThemeLinearGradient SecondLinearGradient { get; set; } = null;

        public SolidColorBrush ThirdColorBrush { get; set; } = new SolidColorBrush(Brushes.ForestGreen.Color);
        public ThemeLinearGradient ThirdLinearGradient { get; set; } = null;

        public SolidColorBrush FirstMultiColorBrush { get; set; } = new SolidColorBrush(Brushes.DarkCyan.Color);
        public ThemeLinearGradient FirstMultiLinearGradient { get; set; } = null;

        public SolidColorBrush SecondMultiColorBrush { get; set; } = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public ThemeLinearGradient SecondMultiLinearGradient { get; set; } = null;

        public SolidColorBrush ThirdMultiColorBrush { get; set; } = new SolidColorBrush(Brushes.ForestGreen.Color);
        public ThemeLinearGradient ThirdMultiLinearGradient { get; set; } = null;


        private SolidColorBrush colorFirst = new SolidColorBrush(Brushes.DarkCyan.Color);
        public SolidColorBrush ColorFirst { get => colorFirst; set => SetValue(ref colorFirst, value); }

        private SolidColorBrush colorSecond = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public SolidColorBrush ColorSecond { get => colorSecond; set => SetValue(ref colorSecond, value); }

        private SolidColorBrush colorThird = new SolidColorBrush(Brushes.ForestGreen.Color);
        public SolidColorBrush ColorThird { get => colorThird; set => SetValue(ref colorThird, value); }

        private SolidColorBrush colorFirstMulti = new SolidColorBrush(Brushes.DarkCyan.Color);
        public SolidColorBrush ColorFirstMulti { get => colorFirstMulti; set => SetValue(ref colorFirstMulti, value); }

        private SolidColorBrush colorSecondMulti = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public SolidColorBrush ColorSecondMulti { get => colorSecondMulti; set => SetValue(ref colorSecondMulti, value); }

        private SolidColorBrush colorThirdMulti = new SolidColorBrush(Brushes.ForestGreen.Color);
        public SolidColorBrush ColorThirdMulti { get => colorThirdMulti; set => SetValue(ref colorThirdMulti, value); }


        public TitleListSort TitleListSort { get; set; } = TitleListSort.Completion;
        public bool IsAsc { get; set; } = false;

        // TODO TMP
        public bool IsConverted { get; set; } = false;
        public List<Storefront> StorefrontElements { get; set; } = new List<Storefront>();
        // TODO TMP
        public List<Storefront> Storefronts { get; set; } = new List<Storefront>();
        public List<HltbPlatformMatch> Platforms { get; set; } = new List<HltbPlatformMatch>();


        public FilterSettings filterSettings { get; set; } = new FilterSettings();


        public bool AutoSetGameStatus { get; set; } = false;
        public Guid GameStatusPlaying { get; set; }
        public Guid GameStatusCompleted { get; set; }
        public Guid GameStatusCompletionist { get; set; }
        #endregion


        #region Settings StartPage
        private HltbChartStatsOptions _hltbChartStatsOptions = new HltbChartStatsOptions();
        public HltbChartStatsOptions hltbChartStatsOptions { get => _hltbChartStatsOptions; set => SetValue(ref _hltbChartStatsOptions, value); }
        #endregion


        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        private bool _HasData = false;
        [DontSerialize]
        public bool HasData { get => _HasData; set => SetValue(ref _HasData, value); }


        private long _MainStory = 0;
        [DontSerialize]
        public long MainStory { get => _MainStory; set => SetValue(ref _MainStory, value); }
        private string _MainStoryFormat = string.Empty;
        [DontSerialize]
        public string MainStoryFormat { get => _MainStoryFormat; set => SetValue(ref _MainStoryFormat, value); }

        private long _MainExtra = 0;
        [DontSerialize]
        public long MainExtra { get => _MainExtra; set => SetValue(ref _MainExtra, value); }
        private string _MainExtraFormat = string.Empty;
        [DontSerialize]
        public string MainExtraFormat { get => _MainExtraFormat; set => SetValue(ref _MainExtraFormat, value); }

        private long _Completionist = 0;
        [DontSerialize]
        public long Completionist { get => _Completionist; set => SetValue(ref _Completionist, value); }
        private string _CompletionistFormat = string.Empty;
        [DontSerialize]
        public string CompletionistFormat { get => _CompletionistFormat; set => SetValue(ref _CompletionistFormat, value); }

        private long _Solo = 0;
        [DontSerialize]
        public long Solo { get => _Solo; set => SetValue(ref _Solo, value); }
        private string _SoloFormat = string.Empty;
        [DontSerialize]
        public string SoloFormat { get => _SoloFormat; set => SetValue(ref _SoloFormat, value); }

        private long _CoOp = 0;
        [DontSerialize]
        public long CoOp { get => _CoOp; set => SetValue(ref _CoOp, value); }
        private string _CoOpFormat = string.Empty;
        [DontSerialize]
        public string CoOpFormat { get => _CoOpFormat; set => SetValue(ref _CoOpFormat, value); }

        private long _Vs = 0;
        [DontSerialize]
        public long Vs { get => _Vs; set => SetValue(ref _Vs, value); }
        private string _VsFormat = string.Empty;
        [DontSerialize]
        public string VsFormat { get => _VsFormat; set => SetValue(ref _VsFormat, value); }


        private long _TimeToBeat = 0;
        [DontSerialize]
        public long TimeToBeat { get => _TimeToBeat; set => SetValue(ref _TimeToBeat, value); }
        private string _TimeToBeatFormat = string.Empty;
        [DontSerialize]
        public string TimeToBeatFormat { get => _TimeToBeatFormat; set => SetValue(ref _TimeToBeatFormat, value); }
        #endregion  
    }


    public class HowLongToBeatSettingsViewModel : ObservableObject, ISettings
    {
        private readonly HowLongToBeat Plugin;
        private HowLongToBeatSettings EditingClone { get; set; }

        private HowLongToBeatSettings settings;
        public HowLongToBeatSettings Settings { get => settings; set => SetValue(ref settings, value); }


        public HowLongToBeatSettingsViewModel(HowLongToBeat plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            HowLongToBeatSettings savedSettings = plugin.LoadPluginSettings<HowLongToBeatSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            Settings = savedSettings ?? new HowLongToBeatSettings();

            // TODO TMP
            if (Settings.Storefronts.Count == 0)
            {
                Settings.Storefronts = new List<Storefront>
                {
                    new Storefront { HltbStorefrontId = HltbStorefront.AmazonGameApp },
                    new Storefront { HltbStorefrontId = HltbStorefront.AppleAppStore },
                    new Storefront { HltbStorefrontId = HltbStorefront.Arc },
                    new Storefront { HltbStorefrontId = HltbStorefront.Battlenet },
                    new Storefront { HltbStorefrontId = HltbStorefront.Bethesda },
                    new Storefront { HltbStorefrontId = HltbStorefront.DirectDownload },
                    new Storefront { HltbStorefrontId = HltbStorefront.Discord },
                    new Storefront { HltbStorefrontId = HltbStorefront.EpicGames },
                    new Storefront { HltbStorefrontId = HltbStorefront.GameCenter },
                    new Storefront { HltbStorefrontId = HltbStorefront.GOG },
                    new Storefront { HltbStorefrontId = HltbStorefront.GooglePlay },
                    new Storefront { HltbStorefrontId = HltbStorefront.GoogleStadia },
                    new Storefront { HltbStorefrontId = HltbStorefront.HumbleBundle },
                    new Storefront { HltbStorefrontId = HltbStorefront.IndieGala },
                    new Storefront { HltbStorefrontId = HltbStorefront.itchio },
                    new Storefront { HltbStorefrontId = HltbStorefront.Kartridge },
                    new Storefront { HltbStorefrontId = HltbStorefront.MicrosoftStore },
                    new Storefront { HltbStorefrontId = HltbStorefront.NintendoeShop },
                    new Storefront { HltbStorefrontId = HltbStorefront.Oculus },
                    new Storefront { HltbStorefrontId = HltbStorefront.Origin },
                    new Storefront { HltbStorefrontId = HltbStorefront.ParadoxGames },
                    new Storefront { HltbStorefrontId = HltbStorefront.PlayStationStore },
                    new Storefront { HltbStorefrontId = HltbStorefront.RockstarGames },
                    new Storefront { HltbStorefrontId = HltbStorefront.Steam },
                    new Storefront { HltbStorefrontId = HltbStorefront.UbisoftConnect },
                    new Storefront { HltbStorefrontId = HltbStorefront.XboxStore },
                    new Storefront { HltbStorefrontId = HltbStorefront.AmazonLuna },
                    new Storefront { HltbStorefrontId = HltbStorefront.GameJolt },
                    new Storefront { HltbStorefrontId = HltbStorefront.JastUsa },
                    new Storefront { HltbStorefrontId = HltbStorefront.LegacyGames },
                    new Storefront { HltbStorefrontId = HltbStorefront.RobotCache }
                };

                Settings.Storefronts = Settings.Storefronts.OrderBy(x => x.HltbStorefrontName).ToList();
            }

            // TODO TMP
            if (Settings.Storefronts.Find(x => x.HltbStorefrontId == HltbStorefront.AmazonLuna) == null)
            {
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.AmazonLuna });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.GameJolt });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.JastUsa });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.LegacyGames });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.RobotCache });

                Settings.Storefronts = Settings.Storefronts.OrderBy(x => x.HltbStorefrontName).ToList();
            }

            _ = Task.Run(() =>
            {
                _ = System.Threading.SpinWait.SpinUntil(() => API.Instance.Database.IsOpen, -1);
                if (Settings.StorefrontElements.Count == 0)
                {
                    API.Instance.Database.Sources.ForEach(x =>
                    {
                        Settings.StorefrontElements.Add(new Storefront { SourceId = x.Id });
                    });

                    // TODO TMP
                    if (!Settings.IsConverted)
                    {
                        Settings.Storefronts.ForEach(x =>
                        {
                            if (x.HltbStorefrontId != HltbStorefront.None)
                            {
                                if (Settings.StorefrontElements.Find(y => y.SourceId == x.SourceId) != null)
                                {
                                    Settings.StorefrontElements.Find(y => y.SourceId == x.SourceId).HltbStorefrontId = x.HltbStorefrontId;
                                }
                            }
                        });
                        Settings.IsConverted = true;
                        try
                        {
                            Application.Current.Dispatcher?.Invoke(() => { Plugin.SavePluginSettings(Settings); });
                        }
                        catch { }
                    }
                }

                // Delete missing source
                Settings.StorefrontElements = Settings.StorefrontElements.Where(x => !x.SourceName.IsNullOrEmpty()).ToList();
                Settings.StorefrontElements = Settings.StorefrontElements.OrderBy(x => x.SourceName).ToList();
            });


            if (Settings.Platforms.Count == 0)
            {
                _ = Task.Run(() =>
                {
                    _ = System.Threading.SpinWait.SpinUntil(() => API.Instance.Database.IsOpen, -1);
                    API.Instance.Database.Platforms.ForEach(x =>
                    {
                        foreach (HltbPlatform hltbPlatform in (HltbPlatform[])Enum.GetValues(typeof(HltbPlatform)))
                        {
                            string tmpName = string.Empty;
                            if (x.Name.Contains("Sony", StringComparison.InvariantCultureIgnoreCase) && x.Name.Contains("PlayStation", StringComparison.InvariantCultureIgnoreCase))
                            {
                                tmpName = x.Name.Replace("Sony", "").Replace("sony", "").Trim();
                            }
                            if (x.Name.Contains("SNK", StringComparison.InvariantCultureIgnoreCase) && x.Name.Contains("Neo Geo", StringComparison.InvariantCultureIgnoreCase))
                            {
                                tmpName = x.Name.Replace("SNK", "").Replace("snk", "").Trim();
                            }
                            if (x.Name.Contains("Nintendo", StringComparison.InvariantCultureIgnoreCase))
                            {
                                tmpName = x.Name.Replace("Nintendo", "").Replace("nintendo", "").Trim();
                            }
                            if (x.Name.Contains("Microsoft", StringComparison.InvariantCultureIgnoreCase))
                            {
                                tmpName = x.Name.Replace("Microsoft", "").Replace("microsoft", "").Trim();
                            }

                            if (Fuzz.Ratio(x.Name, hltbPlatform.GetDescription()) >= 99 || Fuzz.Ratio(tmpName, hltbPlatform.GetDescription()) >= 99)
                            {
                                HltbPlatformMatch a = new HltbPlatformMatch
                                {
                                    HltbPlatform = hltbPlatform,
                                    Platform = x
                                };
                                Settings.Platforms.Add(a);
                            }

                            if (x.Name == "PC (DOS)" && hltbPlatform == HltbPlatform.PC)
                            {
                                HltbPlatformMatch a = new HltbPlatformMatch
                                {
                                    HltbPlatform = hltbPlatform,
                                    Platform = x
                                };
                                Settings.Platforms.Add(a);
                            }

                            if (x.Name == "PC (Windows)" && hltbPlatform == HltbPlatform.PC)
                            {
                                HltbPlatformMatch a = new HltbPlatformMatch
                                {
                                    HltbPlatform = hltbPlatform,
                                    Platform = x
                                };
                                Settings.Platforms.Add(a);
                            }
                        }
                    });
                });
            }

            if (Settings.ThumbLinearGradient == null && Settings.ThumbSolidColorBrush == null)
            {
                if (ResourceProvider.GetResource("NormalBrush") is LinearGradientBrush brush)
                {
                    Settings.ThumbLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(brush);
                }
                else
                {
                    Settings.ThumbSolidColorBrush = (SolidColorBrush)ResourceProvider.GetResource("NormalBrush");
                }
            }
        }

        // Code executed when settings view is opened and user starts editing values.
        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
        }

        // Code executed when user decides to cancel any changes made since BeginEdit was called.
        // This method should revert any changes made to Option1 and Option2.
        public void CancelEdit()
        {
            Settings = EditingClone;
        }

        // Code executed when user decides to confirm changes made since BeginEdit was called.
        // This method should save settings made to Option1 and Option2.
        public void EndEdit()
        {
            Settings.ThumbSolidColorBrush = HowLongToBeatSettingsView.ThumbSolidColorBrush;
            Settings.ThumbLinearGradient = HowLongToBeatSettingsView.ThumbLinearGradient;

            Settings.FirstColorBrush = HowLongToBeatSettingsView.FirstColorBrush;
            Settings.FirstLinearGradient = HowLongToBeatSettingsView.FirstLinearGradient;
            Settings.SecondColorBrush = HowLongToBeatSettingsView.SecondColorBrush;
            Settings.SecondLinearGradient = HowLongToBeatSettingsView.SecondLinearGradient;
            Settings.ThirdColorBrush = HowLongToBeatSettingsView.ThirdColorBrush;
            Settings.ThirdLinearGradient = HowLongToBeatSettingsView.ThirdLinearGradient;

            Settings.FirstMultiColorBrush = HowLongToBeatSettingsView.FirstMultiColorBrush;
            Settings.FirstMultiLinearGradient = HowLongToBeatSettingsView.FirstMultiLinearGradient;
            Settings.SecondMultiColorBrush = HowLongToBeatSettingsView.SecondMultiColorBrush;
            Settings.SecondMultiLinearGradient = HowLongToBeatSettingsView.SecondMultiLinearGradient;
            Settings.ThirdMultiColorBrush = HowLongToBeatSettingsView.ThirdMultiColorBrush;
            Settings.ThirdMultiLinearGradient = HowLongToBeatSettingsView.ThirdMultiLinearGradient;


            if (!Settings.ProgressBarShowTimeAbove && !Settings.ProgressBarShowTimeInterior && !Settings.ProgressBarShowTimeBelow)
            {
                HowLongToBeatSettings savedSettings = Plugin.LoadPluginSettings<HowLongToBeatSettings>();
                if (savedSettings != null)
                {
                    Settings.ProgressBarShowTimeAbove = savedSettings.ProgressBarShowTimeAbove;
                    Settings.ProgressBarShowTimeInterior = savedSettings.ProgressBarShowTimeInterior;
                    Settings.ProgressBarShowTimeBelow = savedSettings.ProgressBarShowTimeBelow;
                }
            }

            Plugin.SavePluginSettings(Settings);
            HowLongToBeat.PluginDatabase.PluginSettings = this;

            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                Plugin.TopPanelItem.Visible = Settings.EnableIntegrationButtonHeader;
                Plugin.SidebarItem.Visible = Settings.EnableIntegrationButtonSide;
            }

            this.OnPropertyChanged();
        }

        // Code execute when user decides to confirm changes made since BeginEdit was called.
        // Executed before EndEdit is called and EndEdit is not called if false is returned.
        // List of errors is presented to user if verification fails.
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}

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
using CommonPluginsShared.Plugins;
using HowLongToBeat.Services;
using HowLongToBeat.Models.Enumerations;

namespace HowLongToBeat
{
    public class HowLongToBeatSettings : PluginSettings
    {
        #region Settings variables

        public bool EnableIntegrationButtonHeader { get; set; } = false;
        public bool EnableIntegrationButtonSide { get; set; } = true;

        public string UserLogin { get; set; } = string.Empty;

        public bool ShowHltbImg { get; set; } = true;

        public bool AutoSetCurrentPlayTime { get; set; } = false;
        public bool AutoSetCurrentPlayTimeWithoutConfirmation { get; set; } = true;
        public bool UsedStartDateFromGameActivity { get; set; } = false;

        public bool AutoAccept { get; set; } = true;
        public bool ShowWhenMismatch { get; set; } = false;
        public bool UseMatchValue { get; set; } = false;
        public double MatchValue { get; set; } = 95;


        public bool UseHtltbClassic { get; set; } = true;
        public bool UseHtltbAverage { get; set; } = false;
        public bool UseHtltbMedian { get; set; } = false;
        public bool UseHtltbRushed { get; set; } = false;
        public bool UseHtltbLeisure { get; set; } = false;


        public bool EnableSucessNotification { get; set; } = true;

        public bool EnableProgressBarInDataView { get; set; } = true;

        private bool _enableIntegrationViewItem = true;
        public bool EnableIntegrationViewItem { get => _enableIntegrationViewItem; set => SetValue(ref _enableIntegrationViewItem, value); }

        public bool IntegrationViewItemOnlyHour { get; set; } = false;

        private bool _enableIntegrationButton = true;
        public bool EnableIntegrationButton { get => _enableIntegrationButton; set => SetValue(ref _enableIntegrationButton, value); }

        private bool _enableIntegrationProgressBar = true;
        public bool EnableIntegrationProgressBar { get => _enableIntegrationProgressBar; set => SetValue(ref _enableIntegrationProgressBar, value); }

        public bool ProgressBarShowToolTip { get; set; } = true;
        public bool ProgressBarShowTimeAbove { get; set; } = false;
        public bool ProgressBarShowTimeInterior { get; set; } = true;
        public bool ProgressBarShowTimeBelow { get; set; } = false;

        public bool ProgressBarShowTimeUser { get; set; } = false;
        public bool ProgressBarShowTime { get; set; } = false;


        public TimeType PreferredForTimeToBeat { get; set; } = TimeType.MainStory;

        public bool ShowMainTime { get; set; } = true;
        public bool ShowExtraTime { get; set; } = true;
        public bool ShowCompletionistTime { get; set; } = true;

        public bool ShowSoloTime { get; set; } = true;
        public bool ShowCoOpTime { get; set; } = true;
        public bool ShowVsTime { get; set; } = true;


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


        private SolidColorBrush _colorFirst = new SolidColorBrush(Brushes.DarkCyan.Color);
        public SolidColorBrush ColorFirst { get => _colorFirst; set => SetValue(ref _colorFirst, value); }

        private SolidColorBrush _colorSecond = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public SolidColorBrush ColorSecond { get => _colorSecond; set => SetValue(ref _colorSecond, value); }

        private SolidColorBrush _colorThird = new SolidColorBrush(Brushes.ForestGreen.Color);
        public SolidColorBrush ColorThird { get => _colorThird; set => SetValue(ref _colorThird, value); }

        private SolidColorBrush _colorFirstMulti = new SolidColorBrush(Brushes.DarkCyan.Color);
        public SolidColorBrush ColorFirstMulti { get => _colorFirstMulti; set => SetValue(ref _colorFirstMulti, value); }

        private SolidColorBrush _colorSecondMulti = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public SolidColorBrush ColorSecondMulti { get => _colorSecondMulti; set => SetValue(ref _colorSecondMulti, value); }

        private SolidColorBrush _colorThirdMulti = new SolidColorBrush(Brushes.ForestGreen.Color);
        public SolidColorBrush ColorThirdMulti { get => _colorThirdMulti; set => SetValue(ref _colorThirdMulti, value); }


        public TitleListSort TitleListSort { get; set; } = TitleListSort.Completion;
        public bool IsAsc { get; set; } = false;

        // TODO TMP
        public bool IsConvertedDb { get; set; } = false;
        public bool IsConverted { get; set; } = false;
        public List<Storefront> StorefrontElements { get; set; } = new List<Storefront>();
        // TODO TMP
        public List<Storefront> Storefronts { get; set; } = new List<Storefront>();
        public List<HltbPlatformMatch> Platforms { get; set; } = new List<HltbPlatformMatch>();


        public FilterSettings filterSettings { get; set; } = new FilterSettings();


        public bool AutoSetGameStatus { get; set; } = false;
        public bool AutoSetGameStatusToHltb { get; set; } = false;
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

        private bool _hasDataEmpty = true;
        [DontSerialize]
        public bool HasDataEmpty { get => _hasDataEmpty; set => SetValue(ref _hasDataEmpty, value); }

        private long _mainStory = 0;
        [DontSerialize]
        public long MainStory { get => _mainStory; set => SetValue(ref _mainStory, value); }

        private string _mainStoryFormat = string.Empty;
        [DontSerialize]
        public string MainStoryFormat { get => _mainStoryFormat; set => SetValue(ref _mainStoryFormat, value); }

        private long _mainExtra = 0;
        [DontSerialize]
        public long MainExtra { get => _mainExtra; set => SetValue(ref _mainExtra, value); }

        private string _mainExtraFormat = string.Empty;
        [DontSerialize]
        public string MainExtraFormat { get => _mainExtraFormat; set => SetValue(ref _mainExtraFormat, value); }

        private long _completionist = 0;
        [DontSerialize]
        public long Completionist { get => _completionist; set => SetValue(ref _completionist, value); }

        private string _completionistFormat = string.Empty;
        [DontSerialize]
        public string CompletionistFormat { get => _completionistFormat; set => SetValue(ref _completionistFormat, value); }

        private long _solo = 0;
        [DontSerialize]
        public long Solo { get => _solo; set => SetValue(ref _solo, value); }

        private string _soloFormat = string.Empty;
        [DontSerialize]
        public string SoloFormat { get => _soloFormat; set => SetValue(ref _soloFormat, value); }

        private long _coOp = 0;
        [DontSerialize]
        public long CoOp { get => _coOp; set => SetValue(ref _coOp, value); }

        private string _coOpFormat = string.Empty;
        [DontSerialize]
        public string CoOpFormat { get => _coOpFormat; set => SetValue(ref _coOpFormat, value); }

        private long _vs = 0;
        [DontSerialize]
        public long Vs { get => _vs; set => SetValue(ref _vs, value); }

        private string _vsFormat = string.Empty;
        [DontSerialize]
        public string VsFormat { get => _vsFormat; set => SetValue(ref _vsFormat, value); }


        private long _timeToBeat = 0;
        [DontSerialize]
        public long TimeToBeat { get => _timeToBeat; set => SetValue(ref _timeToBeat, value); }

        private string _timeToBeatFormat = string.Empty;
        [DontSerialize]
        public string TimeToBeatFormat { get => _timeToBeatFormat; set => SetValue(ref _timeToBeatFormat, value); }

        #endregion  
    }


    public class HowLongToBeatSettingsViewModel : ObservableObject, ISettings
    {
        private readonly HowLongToBeat Plugin;
        private HowLongToBeatSettings EditingClone { get; set; }

        private HowLongToBeatSettings _settings;
        public HowLongToBeatSettings Settings { get => _settings; set => SetValue(ref _settings, value); }


        public HowLongToBeatSettingsViewModel(HowLongToBeat plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            HowLongToBeatSettings savedSettings = plugin.LoadPluginSettings<HowLongToBeatSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            Settings = savedSettings ?? new HowLongToBeatSettings();

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
                    new Storefront { HltbStorefrontId = HltbStorefront.Origin },
                    new Storefront { HltbStorefrontId = HltbStorefront.ParadoxGames },
                    new Storefront { HltbStorefrontId = HltbStorefront.PlayStationStore },
                    new Storefront { HltbStorefrontId = HltbStorefront.RockstarGames },
                    new Storefront { HltbStorefrontId = HltbStorefront.Steam },
                    new Storefront { HltbStorefrontId = HltbStorefront.UbisoftConnect },
                    new Storefront { HltbStorefrontId = HltbStorefront.AmazonLuna },
                    new Storefront { HltbStorefrontId = HltbStorefront.GameJolt },
                    new Storefront { HltbStorefrontId = HltbStorefront.JastUsa },
                    new Storefront { HltbStorefrontId = HltbStorefront.LegacyGames },
                    new Storefront { HltbStorefrontId = HltbStorefront.RobotCache },
                    new Storefront { HltbStorefrontId = HltbStorefront.EAApp },
                    new Storefront { HltbStorefrontId = HltbStorefront.MetaStore },
                    new Storefront { HltbStorefrontId = HltbStorefront.Physical },
                    new Storefront { HltbStorefrontId = HltbStorefront.PlaydateCatalog },
                    new Storefront { HltbStorefrontId = HltbStorefront.UbisoftPlus },
                    new Storefront { HltbStorefrontId = HltbStorefront.Borrowed },
                    new Storefront { HltbStorefrontId = HltbStorefront.Rented },
                    new Storefront { HltbStorefrontId = HltbStorefront.AntstreamArcade },
                    new Storefront { HltbStorefrontId = HltbStorefront.AppleArcade },
                    new Storefront { HltbStorefrontId = HltbStorefront.EAPlay },
                    new Storefront { HltbStorefrontId = HltbStorefront.GooglePlayPass },
                    new Storefront { HltbStorefrontId = HltbStorefront.GoogleStadiaPro },
                    new Storefront { HltbStorefrontId = HltbStorefront.MetaQuestPlus },
                    new Storefront { HltbStorefrontId = HltbStorefront.Netflix },
                    new Storefront { HltbStorefrontId = HltbStorefront.NintendoOnline },
                    new Storefront { HltbStorefrontId = HltbStorefront.PlayStationNow },
                    new Storefront { HltbStorefrontId = HltbStorefront.PlayStationPlus },
                    new Storefront { HltbStorefrontId = HltbStorefront.Viveport },
                    new Storefront { HltbStorefrontId = HltbStorefront.XboxGamePass },
                    new Storefront { HltbStorefrontId = HltbStorefront.XboxGamesWithGold }
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
            if (Settings.Storefronts.Find(x => x.HltbStorefrontId == HltbStorefront.EAApp) == null)
            {
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.EAApp });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.MetaStore });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.Physical });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.PlaydateCatalog });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.UbisoftPlus });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.Borrowed });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.Rented });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.AntstreamArcade });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.AppleArcade });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.EAPlay });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.GooglePlayPass });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.GoogleStadiaPro });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.MetaQuestPlus });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.Netflix });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.NintendoOnline });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.PlayStationNow });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.PlayStationPlus });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.Viveport });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.XboxGamePass });
                Settings.Storefronts.Add(new Storefront { HltbStorefrontId = HltbStorefront.XboxGamesWithGold });

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

            if (!Settings.UseHtltbClassic && !Settings.UseHtltbAverage && !Settings.UseHtltbMedian && !Settings.UseHtltbRushed && !Settings.UseHtltbLeisure)
            {
                Settings.UseHtltbClassic = EditingClone.UseHtltbClassic;
                Settings.UseHtltbAverage = EditingClone.UseHtltbAverage;
                Settings.UseHtltbMedian = EditingClone.UseHtltbMedian;
                Settings.UseHtltbRushed = EditingClone.UseHtltbRushed;
                Settings.UseHtltbLeisure = EditingClone.UseHtltbLeisure;
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

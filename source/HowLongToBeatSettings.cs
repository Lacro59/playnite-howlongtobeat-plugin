using CommonPluginsShared.Models;
using FuzzySharp;
using HowLongToBeat.Models;
using HowLongToBeat.Models.StartPage;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

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

        private bool _EnableIntegrationViewItem = true;
        public bool EnableIntegrationViewItem { get => _EnableIntegrationViewItem; set => SetValue(ref _EnableIntegrationViewItem, value); }

        public bool IntegrationViewItemOnlyHour { get; set; } = false;

        private bool _EnableIntegrationButton = true;
        public bool EnableIntegrationButton { get => _EnableIntegrationButton; set => SetValue(ref _EnableIntegrationButton, value); }

        private bool _EnableIntegrationProgressBar = true;
        public bool EnableIntegrationProgressBar { get => _EnableIntegrationProgressBar; set => SetValue(ref _EnableIntegrationProgressBar, value); }

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


        private SolidColorBrush _ColorFirst = new SolidColorBrush(Brushes.DarkCyan.Color);
        public SolidColorBrush ColorFirst { get => _ColorFirst; set => SetValue(ref _ColorFirst, value); }

        private SolidColorBrush _ColorSecond = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public SolidColorBrush ColorSecond { get => _ColorSecond; set => SetValue(ref _ColorSecond, value); }

        private SolidColorBrush _ColorThird = new SolidColorBrush(Brushes.ForestGreen.Color);
        public SolidColorBrush ColorThird { get => _ColorThird; set => SetValue(ref _ColorThird, value); }

        private SolidColorBrush _ColorFirstMulti = new SolidColorBrush(Brushes.DarkCyan.Color);
        public SolidColorBrush ColorFirstMulti { get => _ColorFirstMulti; set => SetValue(ref _ColorFirstMulti, value); }

        private SolidColorBrush _ColorSecondMulti = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public SolidColorBrush ColorSecondMulti { get => _ColorSecondMulti; set => SetValue(ref _ColorSecondMulti, value); }

        private SolidColorBrush _ColorThirdMulti = new SolidColorBrush(Brushes.ForestGreen.Color);
        public SolidColorBrush ColorThirdMulti { get => _ColorThirdMulti; set => SetValue(ref _ColorThirdMulti, value); }


        public TitleListSort TitleListSort { get; set; } = TitleListSort.Completion;
        public bool IsAsc { get; set; } = false;


        public List<Storefront> Storefronts { get; set; } = new List<Storefront>();
        public List<HltbPlatformMatch> Platforms { get; set; } = new List<HltbPlatformMatch>();
        #endregion


        #region Settings StartPage
        private HltbChartStatsOptions _hltbChartStatsOptions { get; set; } = new HltbChartStatsOptions();
        public HltbChartStatsOptions hltbChartStatsOptions
        {
            get => _hltbChartStatsOptions;
            set
            {
                _hltbChartStatsOptions = value;
                OnPropertyChanged();
            }
        }
        #endregion


        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        private bool _HasData { get; set; } = false;
        [DontSerialize]
        public bool HasData
        {
            get => _HasData;
            set
            {
                _HasData = value;
                OnPropertyChanged();
            }
        }


        private long _MainStory { get; set; } = 0;
        [DontSerialize]
        public long MainStory
        {
            get => _MainStory;
            set
            {
                _MainStory = value;
                OnPropertyChanged();
            }
        }
        private string _MainStoryFormat { get; set; } = string.Empty;
        [DontSerialize]
        public string MainStoryFormat
        {
            get => _MainStoryFormat;
            set
            {
                _MainStoryFormat = value;
                OnPropertyChanged();
            }
        }

        private long _MainExtra { get; set; } = 0;
        [DontSerialize]
        public long MainExtra
        {
            get => _MainExtra;
            set
            {
                _MainExtra = value;
                OnPropertyChanged();
            }
        }
        private string _MainExtraFormat { get; set; } = string.Empty;
        [DontSerialize]
        public string MainExtraFormat
        {
            get => _MainExtraFormat;
            set
            {
                _MainExtraFormat = value;
                OnPropertyChanged();
            }
        }

        private long _Completionist { get; set; } = 0;
        [DontSerialize]
        public long Completionist
        {
            get => _Completionist;
            set
            {
                _Completionist = value;
                OnPropertyChanged();
            }
        }
        private string _CompletionistFormat { get; set; } = string.Empty;
        [DontSerialize]
        public string CompletionistFormat
        {
            get => _CompletionistFormat;
            set
            {
                _CompletionistFormat = value;
                OnPropertyChanged();
            }
        }

        private long _Solo { get; set; } = 0;
        [DontSerialize]
        public long Solo
        {
            get => _Solo;
            set
            {
                _Solo = value;
                OnPropertyChanged();
            }
        }
        private string _SoloFormat { get; set; } = string.Empty;
        [DontSerialize]
        public string SoloFormat
        {
            get => _SoloFormat;
            set
            {
                _SoloFormat = value;
                OnPropertyChanged();
            }
        }

        private long _CoOp { get; set; } = 0;
        [DontSerialize]
        public long CoOp
        {
            get => _CoOp;
            set
            {
                _CoOp = value;
                OnPropertyChanged();
            }
        }
        private string _CoOpFormat { get; set; } = string.Empty;
        [DontSerialize]
        public string CoOpFormat
        {
            get => _CoOpFormat;
            set
            {
                _CoOpFormat = value;
                OnPropertyChanged();
            }
        }

        private long _Vs { get; set; } = 0;
        [DontSerialize]
        public long Vs
        {
            get => _Vs;
            set
            {
                _Vs = value;
                OnPropertyChanged();
            }
        }
        private string _VsFormat { get; set; } = string.Empty;
        [DontSerialize]
        public string VsFormat
        {
            get => _VsFormat;
            set
            {
                _VsFormat = value;
                OnPropertyChanged();
            }
        }


        private long _TimeToBeat { get; set; } = 0;
        [DontSerialize]
        public long TimeToBeat
        {
            get => _TimeToBeat;
            set
            {
                _TimeToBeat = value;
                OnPropertyChanged();
            }
        }
        private string _TimeToBeatFormat { get; set; } = string.Empty;
        [DontSerialize]
        public string TimeToBeatFormat
        {
            get => _TimeToBeatFormat;
            set
            {
                _TimeToBeatFormat = value;
                OnPropertyChanged();
            }
        }
        #endregion  
    }


    public class HowLongToBeatSettingsViewModel : ObservableObject, ISettings
    {
        private readonly HowLongToBeat Plugin;
        private HowLongToBeatSettings EditingClone { get; set; }

        private HowLongToBeatSettings _Settings;
        public HowLongToBeatSettings Settings { get => _Settings; set => SetValue(ref _Settings, value); }


        public HowLongToBeatSettingsViewModel(HowLongToBeat plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<HowLongToBeatSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new HowLongToBeatSettings();
            }

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
                    new Storefront { HltbStorefrontId = HltbStorefront.XboxStore }
                };
            }

            if (Settings.Platforms.Count == 0)
            {
                Task.Run(() => 
                {
                    System.Threading.SpinWait.SpinUntil(() => API.Instance.Database.IsOpen, -1);
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
                                HltbPlatformMatch a = new HltbPlatformMatch();
                                a.HltbPlatform = hltbPlatform;
                                a.Platform = x;
                                Settings.Platforms.Add(a);
                            }

                            if (x.Name == "PC (DOS)" && hltbPlatform == HltbPlatform.PC)
                            {
                                HltbPlatformMatch a = new HltbPlatformMatch();
                                a.HltbPlatform = hltbPlatform;
                                a.Platform = x;
                                Settings.Platforms.Add(a);
                            }

                            if (x.Name == "PC (Windows)" && hltbPlatform == HltbPlatform.PC)
                            {
                                HltbPlatformMatch a = new HltbPlatformMatch();
                                a.HltbPlatform = hltbPlatform;
                                a.Platform = x;
                                Settings.Platforms.Add(a);
                            }
                        }
                    });
                });
            }

            if (Settings.ThumbLinearGradient == null && Settings.ThumbSolidColorBrush == null)
            {
                if (ResourceProvider.GetResource("NormalBrush") is LinearGradientBrush)
                {
                    Settings.ThumbLinearGradient = ThemeLinearGradient.ToThemeLinearGradient((LinearGradientBrush)ResourceProvider.GetResource("NormalBrush"));
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
                var savedSettings = Plugin.LoadPluginSettings<HowLongToBeatSettings>();
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
                Plugin.topPanelItem.Visible = Settings.EnableIntegrationButtonHeader;
                Plugin.howLongToBeatViewSidebar.Visible = Settings.EnableIntegrationButtonSide;
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

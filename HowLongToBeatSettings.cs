using HowLongToBeat.Services;
using HowLongToBeat.Views;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HowLongToBeat
{
    public class HowLongToBeatSettings : ObservableObject
    {
        #region Settings variables
        public bool MenuInExtensions { get; set; } = true;

        public string UserLogin { get; set; } = string.Empty;

        public bool EnableTag { get; set; } = false;

        public bool ShowHltbImg { get; set; } = true;

        public bool AutoSetCurrentPlayTime { get; set; } = false;
        public bool AutoSetCurrentPlayTimeWithoutConfirmation { get; set; } = false;

        public bool AutoAccept { get; set; } = true;
        public bool ShowWhenMismatch { get; set; } = false;


        public bool EnableIntegrationButtonHeader { get; set; } = false;

        private bool _EnableIntegrationButton { get; set; } = false;
        public bool EnableIntegrationButton
        {
            get => _EnableIntegrationButton;
            set
            {
                _EnableIntegrationButton = value;
                OnPropertyChanged();
            }
        }

        private bool _EnableIntegrationProgressBar { get; set; } = false;
        public bool EnableIntegrationProgressBar
        {
            get => _EnableIntegrationProgressBar;
            set
            {
                _EnableIntegrationProgressBar = value;
                OnPropertyChanged();
            }
        }

        public bool ProgressBarShowToolTip { get; set; } = true;
        public bool ProgressBarShowTimeAbove { get; set; } = false;
        public bool ProgressBarShowTimeInterior { get; set; } = true;
        public bool ProgressBarShowTimeBelow { get; set; } = false;

        public bool ProgressBarShowTimeUser { get; set; } = false;
        public bool ProgressBarShowTime { get; set; } = false;


        private Color _ColorFirst { get; set; } = Brushes.DarkCyan.Color;
        public Color ColorFirst
        {
            get => _ColorFirst;
            set
            {
                _ColorFirst = value;
                OnPropertyChanged();
            }
        }

        private Color _ColorSecond { get; set; } = Brushes.RoyalBlue.Color;
        public Color ColorSecond
        {
            get => _ColorSecond;
            set
            {
                _ColorSecond = value;
                OnPropertyChanged();
            }
        }

        private Color _ColorThird { get; set; } = Brushes.ForestGreen.Color;
        public Color ColorThird
        {
            get => _ColorThird;
            set
            {
                _ColorThird = value;
                OnPropertyChanged();
            }
        }

        private Color _ColorFirstMulti { get; set; } = Brushes.DarkCyan.Color;
        public Color ColorFirstMulti
        {
            get => _ColorFirstMulti;
            set
            {
                _ColorFirstMulti = value;
                OnPropertyChanged();
            }
        }

        private Color _ColorSecondMulti { get; set; } = Brushes.RoyalBlue.Color;
        public Color ColorSecondMulti
        {
            get => _ColorSecondMulti;
            set
            {
                _ColorSecondMulti = value;
                OnPropertyChanged();
            }
        }

        private Color _ColorThirdMulti { get; set; } = Brushes.ForestGreen.Color;
        public Color ColorThirdMulti
        {
            get => _ColorThirdMulti;
            set
            {
                _ColorThirdMulti = value;
                OnPropertyChanged();
            }
        }

        public bool EnableIntegrationFS { get; set; } = false;
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        [DontSerialize]
        private bool _HasData { get; set; } = false;
        public bool HasData
        {
            get => _HasData;
            set
            {
                _HasData = value;
                OnPropertyChanged();
            }
        }


        [DontSerialize]
        private long _MainStory { get; set; } = 0;
        public long MainStory
        {
            get => _MainStory;
            set
            {
                _MainStory = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        private string _MainStoryFormat { get; set; } = string.Empty;
        public string MainStoryFormat
        {
            get => _MainStoryFormat;
            set
            {
                _MainStoryFormat = value;
                OnPropertyChanged();
            }
        }

        [DontSerialize]
        private long _MainExtra { get; set; } = 0;
        public long MainExtra
        {
            get => _MainExtra;
            set
            {
                _MainExtra = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        private string _MainExtraFormat { get; set; } = string.Empty;
        public string MainExtraFormat
        {
            get => _MainExtraFormat;
            set
            {
                _MainExtraFormat = value;
                OnPropertyChanged();
            }
        }

        [DontSerialize]
        private long _Completionist { get; set; } = 0;
        public long Completionist
        {
            get => _Completionist;
            set
            {
                _Completionist = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        private string _CompletionistFormat { get; set; } = string.Empty;
        public string CompletionistFormat
        {
            get => _CompletionistFormat;
            set
            {
                _CompletionistFormat = value;
                OnPropertyChanged();
            }
        }

        [DontSerialize]
        private long _Solo { get; set; } = 0;
        public long Solo
        {
            get => _Solo;
            set
            {
                _Solo = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        private string _SoloFormat { get; set; } = string.Empty;
        public string SoloFormat
        {
            get => _SoloFormat;
            set
            {
                _SoloFormat = value;
                OnPropertyChanged();
            }
        }

        [DontSerialize]
        private long _CoOp { get; set; } = 0;
        public long CoOp
        {
            get => _CoOp;
            set
            {
                _CoOp = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        private string _CoOpFormat { get; set; } = string.Empty;
        public string CoOpFormat
        {
            get => _CoOpFormat;
            set
            {
                _CoOpFormat = value;
                OnPropertyChanged();
            }
        }

        [DontSerialize]
        private long _Vs { get; set; } = 0;
        public long Vs
        {
            get => _Vs;
            set
            {
                _Vs = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        private string _VsFormat { get; set; } = string.Empty;
        public string VsFormat
        {
            get => _VsFormat;
            set
            {
                _VsFormat = value;
                OnPropertyChanged();
            }
        }


        [DontSerialize]
        private long _TimeToBeat { get; set; } = 0;
        public long TimeToBeat
        {
            get => _TimeToBeat;
            set
            {
                _TimeToBeat = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        private string _TimeToBeatFormat { get; set; } = string.Empty;
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
        public HowLongToBeatSettings Settings
        {
            get => _Settings;
            set
            {
                _Settings = value;
                OnPropertyChanged();
            }
        }


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
            Settings.ColorFirst = HowLongToBeatSettingsView.ColorFirst;
            Settings.ColorSecond = HowLongToBeatSettingsView.ColorSecond;
            Settings.ColorThird = HowLongToBeatSettingsView.ColorThird;
            Settings.ColorFirstMulti = HowLongToBeatSettingsView.ColorFirstMulti;
            Settings.ColorSecondMulti = HowLongToBeatSettingsView.ColorSecondMulti;
            Settings.ColorThirdMulti = HowLongToBeatSettingsView.ColorThirdMulti;

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

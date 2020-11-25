using HowLongToBeat.Models;
using HowLongToBeat.Views.Interfaces;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatUI : PlayniteUiHelper
    {
        private readonly HowLongToBeatSettings _Settings;

        public override string _PluginUserDataPath { get; set; } = string.Empty;

        public override bool IsFirstLoad { get; set; } = true;

        public override string BtActionBarName { get; set; } = string.Empty;
        public override FrameworkElement PART_BtActionBar { get; set; }

        public override string SpDescriptionName { get; set; } = string.Empty;
        public override FrameworkElement PART_SpDescription { get; set; }

        public override List<CustomElement> ListCustomElements { get; set; } = new List<CustomElement>();


        public HowLongToBeatUI(IPlayniteAPI PlayniteApi, HowLongToBeatSettings Settings, string PluginUserDataPath) : base(PlayniteApi, PluginUserDataPath)
        {
            _Settings = Settings;
            _PluginUserDataPath = PluginUserDataPath;

            BtActionBarName = "PART_HltbButton";
            SpDescriptionName = "PART_HltbDescriptionIntegration";
        }


        public override void Initial()
        {

        }

        public override DispatcherOperation AddElements()
        {
            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                if (IsFirstLoad)
                {
#if DEBUG
                    logger.Debug($"HowLongToBeat - IsFirstLoad");
#endif
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        System.Threading.SpinWait.SpinUntil(() => IntegrationUI.SearchElementByName("PART_HtmlDescription") != null, 5000);
                    })).Wait();
                    IsFirstLoad = false;
                }

                return Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    CheckTypeView();

                    if (_Settings.EnableIntegrationButton)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat - AddBtActionBar()");
#endif
                        AddBtActionBar();
                    }

                    if (_Settings.EnableIntegrationInDescription)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat - AddSpDescription()");
#endif
                        AddSpDescription();
                    }

                    if (_Settings.EnableIntegrationInCustomTheme)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat - AddCustomElements()");
#endif
                        AddCustomElements();
                    }
                }));
            }

            return null;
        }

        public override void RefreshElements(Game GameSelected, bool Force = false)
        {
            taskHelper.Check();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            Task TaskRefresh = Task.Run(() => 
            {
                try
                {
                    Initial();

                    // Reset resources
                    List<ResourcesList> resourcesLists = new List<ResourcesList>();
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_HasData", Value = false });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStory", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStoryFormat", Value = string.Empty });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtra", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtraFormat", Value = string.Empty });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_Completionist", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_CompletionistFormat", Value = string.Empty });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_Solo", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_SoloFormat", Value = string.Empty });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOp", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOpFormat", Value = string.Empty });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_Vs", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_VsFormat", Value = string.Empty });

                    resourcesLists.Add(new ResourcesList { Key = "Htlb_EnableIntegrationInCustomTheme", Value = _Settings.EnableIntegrationInCustomTheme });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorFirst", Value = new SolidColorBrush(_Settings.ColorFirst) });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorSecond", Value = new SolidColorBrush(_Settings.ColorSecond) });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorThird", Value = new SolidColorBrush(_Settings.ColorThird) });
                    ui.AddResources(resourcesLists);


                    // Load data
                    if (!HowLongToBeat.PluginDatabase.IsLoaded)
                    {
                        return;
                    }
                    GameHowLongToBeat gameLocalizations = HowLongToBeat.PluginDatabase.Get(GameSelected, true);
                    HltbDataUser hltbDataUser;

                    if (gameLocalizations.HasData)
                    {
                        hltbDataUser = gameLocalizations.GetData();

                        resourcesLists = new List<ResourcesList>();
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_HasData", Value = gameLocalizations.HasData });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStory", Value = hltbDataUser.GameHltbData.MainStory });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStoryFormat", Value = hltbDataUser.GameHltbData.MainStoryFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtra", Value = hltbDataUser.GameHltbData.MainExtra });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtraFormat", Value = hltbDataUser.GameHltbData.MainExtraFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_Completionist", Value = hltbDataUser.GameHltbData.Completionist });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_CompletionistFormat", Value = hltbDataUser.GameHltbData.CompletionistFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_Solo", Value = hltbDataUser.GameHltbData.Solo });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_SoloFormat", Value = hltbDataUser.GameHltbData.SoloFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOp", Value = hltbDataUser.GameHltbData.CoOp });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOpFormat", Value = hltbDataUser.GameHltbData.CoOpFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_Vs", Value = hltbDataUser.GameHltbData.Vs });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_VsFormat", Value = hltbDataUser.GameHltbData.VsFormat });

                        resourcesLists.Add(new ResourcesList { Key = "Htlb_EnableIntegrationInCustomTheme", Value = _Settings.EnableIntegrationInCustomTheme });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorFirst", Value = new SolidColorBrush(_Settings.ColorFirst) });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorSecond", Value = new SolidColorBrush(_Settings.ColorSecond) });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorThird", Value = new SolidColorBrush(_Settings.ColorThird) });
                    }
                    else
                    {
                        logger.Warn("HowLongToBeat - No data for " + GameSelected.Name);
                    }

                    // If not cancel, show
                    if (!ct.IsCancellationRequested && GameSelected.Id == HowLongToBeat.GameSelected.Id)
                    {
                        ui.AddResources(resourcesLists);

                        if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
                        {
                            HowLongToBeat.PluginDatabase.SetCurrent(gameLocalizations);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat", $"Error on TaskRefreshBtActionBar()");
                }
            }, ct);

            taskHelper.Add(TaskRefresh, tokenSource);
        }


        #region BtActionBar
        public override void InitialBtActionBar()
        {

        }

        public override void AddBtActionBar()
        {
            if (PART_BtActionBar != null)
            {
#if DEBUG
                logger.Debug($"HowLongToBeat - PART_BtActionBar allready insert");
#endif
                return;
            }

            HltbButton BtActionBar = new HltbButton();
            BtActionBar.Click += OnBtActionBarClick;
            BtActionBar.Name = BtActionBarName;
            BtActionBar.Margin = new Thickness(10, 0, 0, 0);

            try
            {
                ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(BtActionBar);
                PART_BtActionBar = IntegrationUI.SearchElementByName(BtActionBarName);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", "Error on AddBtActionBar()");
            }
        }

        public override void RefreshBtActionBar()
        {

        }


        public void OnBtActionBarClick(object sender, RoutedEventArgs e)
        {
            GameHowLongToBeat gameHowLongToBeat = HowLongToBeat.PluginDatabase.Get(HowLongToBeat.GameSelected);

            if (gameHowLongToBeat.HasData)
            {
                var ViewExtension = new Views.HowLongToBeatView(_PlayniteApi, _Settings, gameHowLongToBeat);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, "HowLongToBeat", ViewExtension);
                windowExtension.ShowDialog();

                var TaskIntegrationUI = Task.Run(() =>
                {
                    HowLongToBeat.howLongToBeatUI.RefreshElements(HowLongToBeat.GameSelected);
                });
            }
        }

        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            if (_Settings.EnableIntegrationInCustomTheme)
            {
                string ButtonName = string.Empty;
                try
                {
                    ButtonName = ((Button)sender).Name;
                    if (ButtonName == "PART_HltbCustomButton")
                    {
                        OnBtActionBarClick(sender, e);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat", "OnCustomThemeButtonClick() error");
                }
            }
        }
        #endregion


        #region SpDescription
        public override void InitialSpDescription()
        {

        }

        public override void AddSpDescription()
        {
            if (PART_SpDescription != null)
            {
#if DEBUG
                logger.Debug($"HowLongToBeat - PART_SpDescription allready insert");
#endif
                return;
            }

            try
            {
                HltbDescriptionIntegration SpDescription = new HltbDescriptionIntegration();
                SpDescription.Name = SpDescriptionName;

                ui.AddElementInGameSelectedDescription(SpDescription, _Settings.IntegrationTopGameDetails);
                PART_SpDescription = IntegrationUI.SearchElementByName(SpDescriptionName);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat");
            }
        }

        public override void RefreshSpDescription()
        {

        }
        #endregion


        #region CustomElements
        public override void InitialCustomElements()
        {

        }

        public override void AddCustomElements()
        {
            if (ListCustomElements.Count > 0)
            {
#if DEBUG
                logger.Debug($"HowLongToBeat - CustomElements allready insert - {ListCustomElements.Count}");
#endif
                return;
            }

            FrameworkElement PART_HltbButtonWithJustIcon = null;
            FrameworkElement PART_hltbProgressBar = null;
            try
            {
                PART_HltbButtonWithJustIcon = IntegrationUI.SearchElementByName("PART_HltbButtonWithJustIcon", false, true);
                PART_hltbProgressBar = IntegrationUI.SearchElementByName("PART_hltbProgressBar", false, true);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on find custom element");
            }

            if (PART_HltbButtonWithJustIcon != null)
            {
                PART_HltbButtonWithJustIcon = new HltbButton();
                PART_HltbButtonWithJustIcon.Name = "HltbButtonWithJustIcon";
                ((Button)PART_HltbButtonWithJustIcon).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_HltbButtonWithJustIcon, "PART_HltbButtonWithJustIcon");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_HltbButtonWithJustIcon", Element = PART_HltbButtonWithJustIcon });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"HowLongToBeat - PART_HltbButtonWithJustIcon not find");
#endif
            }

            if (PART_hltbProgressBar != null)
            {
                PART_hltbProgressBar = new HltbProgressBar();
                PART_hltbProgressBar.Name = "hltbProgressBar";
                try
                {
                    ui.AddElementInCustomTheme(PART_hltbProgressBar, "PART_hltbProgressBar");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_hltbProgressBar", Element = PART_hltbProgressBar });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"HowLongToBeat - PART_HltbButtonWithJustIcon not find");
#endif
            }
        }

        public override void RefreshCustomElements()
        {

        }
        #endregion
    }
}

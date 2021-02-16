﻿using CommonPluginsShared;
using HowLongToBeat.Models;
using HowLongToBeat.Views.Interfaces;
using HowLongToBeat.Views.InterfacesFS;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
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
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        public override string _PluginUserDataPath { get; set; } = string.Empty;

        public override bool IsFirstLoad { get; set; } = true;

        public override string BtActionBarName { get; set; } = string.Empty;
        public override FrameworkElement PART_BtActionBar { get; set; }

        public override string SpDescriptionName { get; set; } = string.Empty;
        public override FrameworkElement PART_SpDescription { get; set; }


        public override string SpInfoBarFSName { get; set; } = string.Empty;
        public override FrameworkElement PART_SpInfoBarFS { get; set; }

        public override string BtActionBarFSName { get; set; } = string.Empty;
        public override FrameworkElement PART_BtActionBarFS { get; set; }


        public override List<CustomElement> ListCustomElements { get; set; } = new List<CustomElement>();


        public HowLongToBeatUI(IPlayniteAPI PlayniteApi, HowLongToBeatSettings Settings, string PluginUserDataPath) : base(PlayniteApi, PluginUserDataPath)
        {
            _PluginUserDataPath = PluginUserDataPath;

            BtActionBarName = "PART_HltbButton";
            SpDescriptionName = "PART_HltbDescriptionIntegration";

            SpInfoBarFSName = "PART_HltbSpInfoBar";
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
                    logger.Debug($"HowLongToBeat [Ignored] - IsFirstLoad");
#endif
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        System.Threading.SpinWait.SpinUntil(() => IntegrationUI.SearchElementByName("PART_HtmlDescription") != null, 5000);
                    })).Wait();
                    IsFirstLoad = false;
                }

                return Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    CheckTypeView();

                    if (PluginDatabase.PluginSettings.EnableIntegrationButton)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat [Ignored] - AddBtActionBar()");
#endif
                        AddBtActionBar();
                    }

                    if (PluginDatabase.PluginSettings.EnableIntegrationInDescription)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat [Ignored] - AddSpDescription()");
#endif
                        AddSpDescription();
                    }

                    if (PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat [Ignored] - AddCustomElements()");
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
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_TimeToBeat", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_TimeToBeatFormat", Value = string.Empty });

                    resourcesLists.Add(new ResourcesList { Key = "Htlb_EnableIntegrationInCustomTheme", Value = PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorFirst", Value = new SolidColorBrush(PluginDatabase.PluginSettings.ColorFirst) });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorSecond", Value = new SolidColorBrush(PluginDatabase.PluginSettings.ColorSecond) });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorThird", Value = new SolidColorBrush(PluginDatabase.PluginSettings.ColorThird) });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorFirstMulti", Value = new SolidColorBrush(PluginDatabase.PluginSettings.ColorFirstMulti) });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorSecondMulti", Value = new SolidColorBrush(PluginDatabase.PluginSettings.ColorSecondMulti) });
                    resourcesLists.Add(new ResourcesList { Key = "Htlb_ColorThirdMulti", Value = new SolidColorBrush(PluginDatabase.PluginSettings.ColorThirdMulti) });
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
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_TimeToBeat", Value = hltbDataUser.GameHltbData.TimeToBeat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_TimeToBeatFormat", Value = hltbDataUser.GameHltbData.TimeToBeatFormat });

                        resourcesLists.Add(new ResourcesList { Key = "Htlb_EnableIntegrationInCustomTheme", Value = PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme });
                    }
                    else
                    {
                        logger.Warn("HowLongToBeat - No data for " + GameSelected.Name);
                    }

                    // If not cancel, show
                    if (!ct.IsCancellationRequested && GameSelected.Id == HowLongToBeatDatabase.GameSelected.Id)
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
                logger.Debug($"HowLongToBeat [Ignored] - PART_BtActionBar allready insert");
#endif
                return;
            }

            HltbButton BtActionBar = new HltbButton();
            BtActionBar.Click += OnBtActionBarClick;
            BtActionBar.Name = BtActionBarName;

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
            GameHowLongToBeat gameHowLongToBeat = HowLongToBeat.PluginDatabase.Get(HowLongToBeatDatabase.GameSelected);

            if (gameHowLongToBeat.HasData || gameHowLongToBeat.HasDataEmpty)
            {
                var ViewExtension = new Views.HowLongToBeatView(_PlayniteApi, PluginDatabase.PluginSettings, gameHowLongToBeat);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, "HowLongToBeat", ViewExtension);
                windowExtension.ShowDialog();

                var TaskIntegrationUI = Task.Run(() =>
                {
                    HowLongToBeat.howLongToBeatUI.RefreshElements(HowLongToBeatDatabase.GameSelected);
                });
            }
        }

        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            if (PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme)
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
                logger.Debug($"HowLongToBeat [Ignored] - PART_SpDescription allready insert");
#endif
                return;
            }

            try
            {
                HltbDescriptionIntegration SpDescription = new HltbDescriptionIntegration();
                SpDescription.Name = SpDescriptionName;

                ui.AddElementInGameSelectedDescription(SpDescription, PluginDatabase.PluginSettings.IntegrationTopGameDetails, PluginDatabase.PluginSettings.IntegrationShowTitle);
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
                logger.Debug($"HowLongToBeat [Ignored] - CustomElements allready insert - {ListCustomElements.Count}");
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
                logger.Debug($"HowLongToBeat [Ignored] - PART_HltbButtonWithJustIcon not find");
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
                logger.Debug($"HowLongToBeat [Ignored] - PART_HltbButtonWithJustIcon not find");
#endif
            }
        }

        public override void RefreshCustomElements()
        {

        }
        #endregion




        public override DispatcherOperation AddElementsFS()
        {
            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                if (IsFirstLoad)
                {
#if DEBUG
                    logger.Debug($"HowLongToBeat [Ignored] - IsFirstLoad");
#endif
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        System.Threading.SpinWait.SpinUntil(() => IntegrationUI.SearchElementByName("PART_ButtonContext") != null, 5000);
                    })).Wait();
                    IsFirstLoad = false;
                }

                return Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    if (PluginDatabase.PluginSettings.EnableIntegrationFS)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat [Ignored] - AddBtInfoBarFS()");
#endif
                        AddSpInfoBarFS();
                    }
                }));
            }

            return null;
        }


        #region SpInfoBarFS
        public override void InitialSpInfoBarFS()
        {

        }

        public override void AddSpInfoBarFS()
        {
            if (PART_SpInfoBarFS != null)
            {
#if DEBUG
                logger.Debug($"HowLongToBeat [Ignored] - PART_BtInfoBar allready insert");
#endif

                ((HLTBInfoBarFS)PART_SpInfoBarFS).SetData(PluginDatabase.Get(HowLongToBeatDatabase.GameSelected));
                return;
            }

            FrameworkElement SpInfoBar;
            SpInfoBar = new HLTBInfoBarFS();

            SpInfoBar.Name = SpInfoBarFSName;
            SpInfoBar.Margin = new Thickness(50, 0, 0, 0);

            try
            {
                ui.AddStackPanelInGameSelectedInfoBarFS(SpInfoBar);
                PART_SpInfoBarFS = IntegrationUI.SearchElementByName(SpInfoBarFSName);

                if (PART_SpInfoBarFS != null)
                {
                    ((HLTBInfoBarFS)PART_SpInfoBarFS).SetData(PluginDatabase.Get(HowLongToBeatDatabase.GameSelected));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat");
            }
        }

        public override void RefreshSpInfoBarFS()
        {

        }
        #endregion


        #region BtActionBarFS
        public override void InitialBtActionBarFS()
        {

        }

        public override void AddBtActionBarFS()
        {

        }

        public override void RefreshBtActionBarFS()
        {

        }
        #endregion
    }
}

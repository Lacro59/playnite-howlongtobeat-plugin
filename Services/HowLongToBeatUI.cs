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
            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                if (_Settings.EnableIntegrationButton)
                {
#if DEBUG
                    logger.Debug($"HowLongToBeat - InitialBtActionBar()");
#endif
                    InitialBtActionBar();
                }

                if (_Settings.EnableIntegrationInDescription)
                {
#if DEBUG
                    logger.Debug($"HowLongToBeat - InitialSpDescription()");
#endif
                    InitialSpDescription();
                }

                if (_Settings.EnableIntegrationInCustomTheme)
                {
#if DEBUG
                    logger.Debug($"HowLongToBeat - InitialCustomElements()");
#endif
                    InitialCustomElements();
                }
            }
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
                    Thread.Sleep(1000);
                    IsFirstLoad = false;
                }

                return Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
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
                    try
                    {
                        HowLongToBeat.HltbGameData = new HowLongToBeatData(GameSelected, _PluginUserDataPath, _PlayniteApi, false);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "HowLongToBeat", "Error to load data");
                        _PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCDatabaseErroTitle"), "HowLongToBeat");
                    }

                    if (HowLongToBeat.HltbGameData.hasData)
                    {
                        resourcesLists = new List<ResourcesList>();
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_HasData", Value = HowLongToBeat.HltbGameData.hasData });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStory", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.MainStory });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStoryFormat", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.MainStoryFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtra", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.MainExtra });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtraFormat", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.MainExtraFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_Completionist", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.Completionist });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_CompletionistFormat", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.CompletionistFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_Solo", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.Solo });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_SoloFormat", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.SoloFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOp", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.CoOp });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOpFormat", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.CoOpFormat });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_Vs", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.Vs });
                        resourcesLists.Add(new ResourcesList { Key = "Htlb_VsFormat", Value = HowLongToBeat.HltbGameData.GetData().GameHltbData.VsFormat });
                    }
                    else
                    {
                        logger.Warn("HowLongToBeat - No data for " + GameSelected.Name);
                    }

                    if (_Settings.EnableTag)
                    {
                        HowLongToBeat.HltbGameData.AddTag();
                    }

                    // If not cancel, show
                    if (!ct.IsCancellationRequested && _PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
                    {
                        ui.AddResources(resourcesLists);

                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                        {
                            if (_Settings.EnableIntegrationButton)
                            {
#if DEBUG
                                logger.Debug($"HowLongToBeat - RefreshBtActionBar()");
#endif
                                RefreshBtActionBar();
                            }

                            if (_Settings.EnableIntegrationInDescription)
                            {
#if DEBUG
                                logger.Debug($"HowLongToBeat - RefreshSpDescription()");
#endif
                                RefreshSpDescription();
                            }

                            if (_Settings.EnableIntegrationInCustomTheme)
                            {
#if DEBUG
                                logger.Debug($"HowLongToBeat - RefreshCustomElements()");
#endif
                                RefreshCustomElements();
                            }
                        }));
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
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                if (PART_BtActionBar != null)
                {
                    PART_BtActionBar.Visibility = Visibility.Collapsed;
                }
            });
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
            if (PART_BtActionBar != null)
            {
                PART_BtActionBar.Visibility = Visibility.Visible;
            }
            else
            {
                logger.Warn($"HowLongToBeat - PART_BtActionBar is not defined");
            }
        }


        public void OnBtActionBarClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            logger.Debug($"HowLongToBeat - HowLongToBeat.HltbGameData: {JsonConvert.SerializeObject(HowLongToBeat.HltbGameData)}");
#endif
            if (!HowLongToBeat.HltbGameData.hasData)
            {
                HowLongToBeat.HltbGameData.SearchData(HowLongToBeat.GameSelected);
            }

            if (HowLongToBeat.HltbGameData.hasData)
            {
                if (_Settings.EnableTag)
                {
                    HowLongToBeat.HltbGameData.AddTag();
                }

                var ViewExtension = new Views.HowLongToBeatView(HowLongToBeat.HltbGameData, HowLongToBeat.GameSelected, _PlayniteApi, _Settings);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, "HowLongToBeat", ViewExtension);
                windowExtension.ShowDialog();
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
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                if (PART_SpDescription != null)
                {
                    PART_SpDescription.Visibility = Visibility.Collapsed;
                }
            });
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
                HltbDescriptionIntegration SpDescription = new HltbDescriptionIntegration(_Settings.IntegrationShowTitle);
                SpDescription.Name = SpDescriptionName;

                ui.AddElementInGameSelectedDescription(SpDescription, _Settings.IntegrationTopGameDetails);
                PART_SpDescription = IntegrationUI.SearchElementByName(SpDescriptionName);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", "Error on AddSpDescription()");
            }
        }

        public override void RefreshSpDescription()
        {
            if (PART_SpDescription != null)
            {
#if DEBUG
                logger.Debug($"HowLongToBeat - HowLongToBeat.HltbGameData: {JsonConvert.SerializeObject(HowLongToBeat.HltbGameData)}");
#endif
                if (HowLongToBeat.HltbGameData.hasData && !HowLongToBeat.HltbGameData.isEmpty)
                {
                    PART_SpDescription.Visibility = Visibility.Visible;

                    if (PART_SpDescription is HltbDescriptionIntegration)
                    {
                        ((HltbDescriptionIntegration)PART_SpDescription).SetHltbData(
                            HowLongToBeat.GameSelected.Playtime, HowLongToBeat.HltbGameData, _Settings
                        );
                    }
                }
                else
                {
#if DEBUG
                    logger.Debug($"HowLongToBeat - No data for {HowLongToBeat.GameSelected.Name}");
#endif
                }
            }
            else
            {
                logger.Warn($"HowLongToBeat - PART_SpDescription is not defined");
            }
        }
        #endregion


        #region CustomElements
        public override void InitialCustomElements()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                foreach (CustomElement customElement in ListCustomElements)
                {
                    customElement.Element.Visibility = Visibility.Collapsed;
                }
            });
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
            FrameworkElement PART_hltbProgressBarWithTitle = null;
            FrameworkElement PART_hltbProgressBar = null;
            try
            {
                PART_HltbButtonWithJustIcon = IntegrationUI.SearchElementByName("PART_HltbButtonWithJustIcon", false, true);
                PART_hltbProgressBarWithTitle = IntegrationUI.SearchElementByName("PART_hltbProgressBarWithTitle", false, true);
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

            if (PART_hltbProgressBarWithTitle != null)
            {
                PART_hltbProgressBarWithTitle = new HltbDescriptionIntegration(true);
                PART_hltbProgressBarWithTitle.Name = "hltbProgressBarWithTitle";
                try
                {
                    ui.AddElementInCustomTheme(PART_hltbProgressBarWithTitle, "PART_hltbProgressBarWithTitle");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_hltbProgressBarWithTitle", Element = PART_hltbProgressBarWithTitle });
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
                PART_hltbProgressBar = PART_hltbProgressBarWithTitle = new HltbDescriptionIntegration(false);
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
#if DEBUG
            logger.Debug($"HowLongToBeat - ListCustomElements - {ListCustomElements.Count}");
#endif
            foreach (CustomElement customElement in ListCustomElements)
            {
                try
                {
                    bool isFind = false;

                    if (customElement.Element is HltbButton)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat - customElement.Element is HltbButton");
#endif
                        customElement.Element.Visibility = Visibility.Visible;
                        isFind = true;
                    }

                    if (customElement.Element is HltbDescriptionIntegration)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat - customElement.Element is HltbDescriptionIntegration");
#endif
                        isFind = true;
                        if (HowLongToBeat.HltbGameData.hasData && !HowLongToBeat.HltbGameData.isEmpty)
                        {
                            customElement.Element.Visibility = Visibility.Visible;

                            ((HltbDescriptionIntegration)customElement.Element).SetHltbData(
                                HowLongToBeat.GameSelected.Playtime, HowLongToBeat.HltbGameData, _Settings
                            );
                        }
                        else
                        {
#if DEBUG
                            logger.Debug($"HowLongToBeat - customElement.Element is HltbDescriptionIntegration with no data");
#endif
                        }
                    }

                    if (!isFind)
                    {
                        logger.Warn($"HowLongToBeat - RefreshCustomElements({customElement.ParentElementName})");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat", $"Error on RefreshCustomElements()");
                }
            }
        }
        #endregion
    }
}

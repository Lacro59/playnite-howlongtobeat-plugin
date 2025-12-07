using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Enumerations;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatView.xaml
    /// </summary>
    public partial class HowLongToBeatView : UserControl
    {
        private HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        private GameHowLongToBeat GameHowLongToBeat { get; set; }
        private CancellationTokenSource _coverLoadCts;
        private HowLongToBeatViewData _viewDataContext;


        public HowLongToBeatView(GameHowLongToBeat gameHowLongToBeat)
        {
            GameHowLongToBeat = gameHowLongToBeat;

            InitializeComponent();
            DataContext = new HowLongToBeatViewData();

            // Attach and track the DataContext so we can unsubscribe later to avoid leaks
            AttachViewModel();

            // Monitor DataContext changes and unload to detach handlers
            this.DataContextChanged += OnDataContextChanged;
            this.Unloaded += HowLongToBeatView_Unloaded;

            Init(gameHowLongToBeat);
        }

        private void CoverImageControl_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            try
            {
                if (!(sender is Image img)) return;

                if (img.Source is BitmapImage source && source.UriSource != null)
                {
                    Common.LogDebug(true, $"Cover TargetUpdated: UriSource={source.UriSource}");

                    var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                    img.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

                    if (img.Effect is BlurEffect blur)
                    {
                        var blurAnimation = new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(400)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
                        blur.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);
                    }
                }
                else
                {
                    Common.LogDebug(true, "Cover TargetUpdated: non-Uri or stream source — skipping automatic reload");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void SetCoverImageSource(string path)
        {
            try
            {
                Common.LogDebug(true, $"SetCoverImageSource request for path='{path}'");

                _coverLoadCts?.Cancel();
                _coverLoadCts?.Dispose();
            }
            catch { }

            _coverLoadCts = new CancellationTokenSource();
            var token = _coverLoadCts.Token;
            _ = LoadCoverAsync(path, token);
        }

        private static readonly HttpClient _sharedHttpClient = new HttpClient();

        private async Task LoadCoverAsync(string path, CancellationToken token)
        {
            try
            {
                Common.LogDebug(true, $"LoadCoverAsync start path='{path}'");

                var img = null as Image;
                if (Dispatcher != null)
                {
                    await Dispatcher.InvokeAsync(() => { img = FindName("CoverImageControl") as Image; });
                }
                if (img == null || token.IsCancellationRequested)
                {
                    Common.LogDebug(true, "LoadCoverAsync: image control missing or cancelled");
                    return;
                }

                if (Dispatcher != null)
                {
                    await Dispatcher.InvokeAsync(new Action(() =>
                    {
                        img.Opacity = 0;
                        var blur = img.Effect as BlurEffect;
                        if (blur == null)
                        {
                            img.Effect = new BlurEffect { Radius = 8 };
                        }
                        else
                        {
                            blur.Radius = 8;
                        }
                    }));
                }

                if (string.IsNullOrEmpty(path) || token.IsCancellationRequested)
                {
                    Common.LogDebug(true, "LoadCoverAsync: empty path or cancelled");
                    if (Dispatcher != null)
                    {
                        await Dispatcher.InvokeAsync(new Action(() => img.Source = null));
                    }
                    return;
                }

                byte[] bytes = null;
                if (Uri.IsWellFormedUriString(path, UriKind.Absolute) && (path.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https:", StringComparison.OrdinalIgnoreCase)))
                {
                    Common.LogDebug(true, $"LoadCoverAsync: applying remote Uri directly '{path}' on UI thread");
                    if (Dispatcher != null)
                    {
                        await Dispatcher.InvokeAsync(new Action(() =>
                        {
                            try
                            {
                                var b = new BitmapImage();
                                b.BeginInit();
                                b.UriSource = new Uri(path, UriKind.Absolute);
                                b.CacheOption = BitmapCacheOption.OnLoad;
                                b.CreateOptions = BitmapCreateOptions.None;
                                b.EndInit();
                                img.Source = b;

                                var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                                img.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

                                if (img.Effect is BlurEffect blur3)
                                {
                                    var blurAnimation = new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(400)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
                                    blur3.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);
                                }

                                Common.LogDebug(true, "LoadCoverAsync: remote Uri applied to control");
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                            }
                        }));
                    }

                    return;
                }

                if (File.Exists(path))
                {
                    Common.LogDebug(true, $"LoadCoverAsync: loading local file '{path}'");
                    bytes = await Task.Run(() => File.ReadAllBytes(path));
                }
                else if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                {
                    Common.LogDebug(true, $"LoadCoverAsync: downloading remote url '{path}'");
                    try
                    {
                        bytes = await _sharedHttpClient.GetByteArrayAsync(path);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        bytes = null;
                    }
                }
                else
                {
                    Common.LogDebug(true, $"LoadCoverAsync: attempting read relative path '{path}'");
                    try
                    {
                        bytes = await Task.Run(() => File.ReadAllBytes(path));
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        bytes = null;
                    }
                }

                if (token.IsCancellationRequested)
                {
                    Common.LogDebug(true, "LoadCoverAsync: cancelled after load");
                    return;
                }

                if (bytes == null || bytes.Length == 0)
                {
                    Common.LogDebug(true, "LoadCoverAsync: no bytes loaded");
                    if (Dispatcher != null)
                    {
                        await Dispatcher.InvokeAsync(new Action(() => img.Source = null));
                    }
                    return;
                }

                BitmapImage bmp = null;
                await Task.Run(() =>
                {
                    try
                    {
                        using (var ms = new MemoryStream(bytes))
                        {
                            var b = new BitmapImage();
                            b.BeginInit();
                            b.CacheOption = BitmapCacheOption.OnLoad;
                            b.CreateOptions = BitmapCreateOptions.None;
                            b.StreamSource = ms;
                            b.EndInit();
                            try { b.Freeze(); } catch { }
                            bmp = b;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        bmp = null;
                    }
                });

                if (token.IsCancellationRequested)
                {
                    Common.LogDebug(true, "LoadCoverAsync: cancelled after bitmap creation");
                    return;
                }

                if (bmp == null)
                {
                    Common.LogDebug(true, "LoadCoverAsync: bmp is null after creation");
                    if (Dispatcher != null)
                    {
                        await Dispatcher.InvokeAsync(new Action(() => img.Source = null));
                    }
                    return;
                }

                if (Dispatcher != null)
                {
                    await Dispatcher.InvokeAsync(new Action(() =>
                    {
                        try
                        {
                            img.Source = bmp;

                            var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                            img.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

                            if (img.Effect is BlurEffect blur2)
                            {
                                var blurAnimation = new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(400)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
                                blur2.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);
                            }

                            Common.LogDebug(true, "LoadCoverAsync: image applied to control");
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        }
                    }));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        private void Init(GameHowLongToBeat gameHowLongToBeat)
        {
            Hltb_El1_Data.Text = string.Empty;
            Hltb_El1_DataUser.Text = string.Empty;
            Hltb_El1_DataUser_1.Text = string.Empty;
            Hltb_El1_DataUser_2.Text = string.Empty;
            Hltb_El1_DataUser_3.Text = string.Empty;

            Hltb_El2_Data.Text = string.Empty;
            Hltb_El2_DataUser.Text = string.Empty;
            Hltb_El2_DataUser_1.Text = string.Empty;
            Hltb_El2_DataUser_2.Text = string.Empty;
            Hltb_El2_DataUser_3.Text = string.Empty;

            Hltb_El3_Data.Text = string.Empty;
            Hltb_El3_DataUser.Text = string.Empty;
            Hltb_El3_DataUser_1.Text = string.Empty;
            Hltb_El3_DataUser_2.Text = string.Empty;
            Hltb_El3_DataUser_3.Text = string.Empty;


            HltbDataUser gameData = gameHowLongToBeat?.Items?.FirstOrDefault();

            if (gameData == null || gameData.Name.IsNullOrEmpty())
            {
                return;
            }

            if (gameData != null)
            {
                ((HowLongToBeatViewData)DataContext).CoverImage = gameData.UrlImg;

                if (!PluginDatabase.PluginSettings.Settings.ShowHltbImg)
                {
                    if (!gameHowLongToBeat.CoverImage.IsNullOrEmpty())
                    {
                        ((HowLongToBeatViewData)DataContext).CoverImage = API.Instance.Database.GetFullFilePath(gameHowLongToBeat.CoverImage);
                    }
                }

                ((HowLongToBeatViewData)DataContext).GameContext = API.Instance.Database.Games.Get(gameHowLongToBeat.Id);
                ((HowLongToBeatViewData)DataContext).SourceLink = gameHowLongToBeat.SourceLink;

                try
                {
                    var hltb = gameHowLongToBeat.GetData()?.GameHltbData;
                    if (hltb != null)
                    {
                        // Keep numeric values in the same unit as HltbData (seconds) for proportional calculation
                        ((HowLongToBeatViewData)DataContext).MainHours = hltb.MainStory;
                        ((HowLongToBeatViewData)DataContext).MainExtraHours = hltb.MainExtra;
                        ((HowLongToBeatViewData)DataContext).CompletionistHours = hltb.Completionist;

                        // Provide formatted strings for display (e.g. "6h 6m")
                        ((HowLongToBeatViewData)DataContext).MainHoursFormat = hltb.MainStoryFormat;
                        ((HowLongToBeatViewData)DataContext).MainExtraHoursFormat = hltb.MainExtraFormat;
                        ((HowLongToBeatViewData)DataContext).CompletionistHoursFormat = hltb.CompletionistFormat;
                    }
                }
                catch { }
            }

            if (!gameHowLongToBeat.HasData)
            {
                PART_GridProgressBar.Visibility = Visibility.Hidden;
                PART_TextBlock.Visibility = Visibility.Hidden;
            }

            if (gameData != null)
            {
                Hltb_El1.Visibility = Visibility.Hidden;
                Hltb_El1_Color.Visibility = Visibility.Hidden;
                Hltb_El2.Visibility = Visibility.Hidden;
                Hltb_El2_Color.Visibility = Visibility.Hidden;
                Hltb_El3.Visibility = Visibility.Hidden;
                Hltb_El3_Color.Visibility = Visibility.Hidden;


                List<TitleList> titleLists = PluginDatabase.GetUserHltbDataAll(gameHowLongToBeat.GetData().Id);
                TitleList titleList = null;
                if (titleLists != null && titleLists.Count > 0)
                {
                    for (int idx = 0; idx < titleLists.Count; idx++)
                    {
                        int ElIndicator = 0;
                        titleList = titleLists[idx];

                        RadioButton Hltb_0 = new RadioButton();
                        switch (idx)
                        {
                            case 0:
                                Hltb_0 = Hltb_El0;
                                break;
                            case 1:
                                Hltb_0 = Hltb_El0_1;
                                break;
                            case 2:
                                Hltb_0 = Hltb_El0_2;
                                break;
                            case 3:
                                Hltb_0 = Hltb_El0_3;
                                break;
                            default:
                                break;
                        }


                        if (!gameHowLongToBeat.UserGameId.IsNullOrEmpty() && gameHowLongToBeat.UserGameId.IsEqual(titleList.UserGameId))
                        {
                            Hltb_0.IsChecked = true;
                        }


                        LocalDateConverter localDateConverter = new LocalDateConverter();
                        string dateStart = localDateConverter.Convert(titleList.StartDate, null, null, CultureInfo.CurrentCulture).ToString();
                        string lastUpdate = localDateConverter.Convert(titleList.LastUpdate, null, null, CultureInfo.CurrentCulture).ToString();
                        string content = string.Empty;
                        if (!dateStart.IsNullOrEmpty())
                        {
                            content += ResourceProvider.GetString("LOCCommonStartDate") + ": " + dateStart;
                        }
                        if (!lastUpdate.IsNullOrEmpty())
                        {
                            if (!content.IsNullOrEmpty())
                            {
                                content += Environment.NewLine;
                            }
                            content += ResourceProvider.GetString("LOCCommonLastUpdate") + ": " + lastUpdate;
                        }
                        Hltb_0.Content = content;
                        Hltb_0.Tag = titleList.UserGameId;
                        Hltb_0.Visibility = Visibility.Visible;

                        if (idx == 0)
                        {
                            Hltb_El0_tb.Text = content;
                            Hltb_El0_tb.Tag = titleList.UserGameId;
                            Hltb_El0_tb.Visibility = Visibility.Visible;
                            Hltb_0.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            Hltb_El0.Visibility = Visibility.Visible;
                            Hltb_El0_tb.Visibility = Visibility.Collapsed;
                        }

                        if (gameData.GameType != GameType.Multi)
                        {
                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatMainStory"), gameData.GameHltbData.MainStoryFormat, (titleList != null) ? titleList.HltbUserData.MainStoryFormat : string.Empty, idx);
                            SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirst.Color);

                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatMainExtra"), gameData.GameHltbData.MainExtraFormat, (titleList != null) ? titleList.HltbUserData.MainExtraFormat : string.Empty, idx);
                            SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecond.Color);

                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatCompletionist"), gameData.GameHltbData.CompletionistFormat, (titleList != null) ? titleList.HltbUserData.CompletionistFormat : string.Empty, idx);
                            SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThird.Color);
                        }
                        else
                        {
                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatSolo"), gameData.GameHltbData.SoloFormat, (titleList != null) ? titleList.HltbUserData.SoloFormat : string.Empty, idx);
                            SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirstMulti.Color);

                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatCoOp"), gameData.GameHltbData.CoOpFormat, (titleList != null) ? titleList.HltbUserData.CoOpFormat : string.Empty, idx);

                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatVs"), gameData.GameHltbData.VsFormat, (titleList != null) ? titleList.HltbUserData.VsFormat : string.Empty, idx);
                        }
                    }
                }
                else if (gameData?.GameHltbData != null)
                {
                    Hltb_El0.Visibility = Visibility.Collapsed;
                    int ElIndicator = 0;

                    if (gameData.GameType != GameType.Multi)
                    {
                        ElIndicator += 1;
                        SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatMainStory"), gameData.GameHltbData.MainStoryFormat, (titleList != null) ? titleList.HltbUserData.MainStoryFormat : string.Empty, 0);
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirst.Color);

                        ElIndicator += 1;
                        SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatMainExtra"), gameData.GameHltbData.MainExtraFormat, (titleList != null) ? titleList.HltbUserData.MainExtraFormat : string.Empty, 0);
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecond.Color);

                        ElIndicator += 1;
                        SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatCompletionist"), gameData.GameHltbData.CompletionistFormat, (titleList != null) ? titleList.HltbUserData.CompletionistFormat : string.Empty, 0);
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThird.Color);
                    }
                    else
                    {
                        ElIndicator += 1;
                        SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatSolo"), gameData.GameHltbData.SoloFormat, (titleList != null) ? titleList.HltbUserData.SoloFormat : string.Empty, 0);
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirstMulti.Color);

                        ElIndicator += 1;
                        SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatCoOp"), gameData.GameHltbData.CoOpFormat, (titleList != null) ? titleList.HltbUserData.CoOpFormat : string.Empty, 0);

                        ElIndicator += 1;
                        SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatVs"), gameData.GameHltbData.VsFormat, (titleList != null) ? titleList.HltbUserData.VsFormat : string.Empty, 0);
                    }
                }

                long rt = (gameHowLongToBeat.GetData()?.GameHltbData?.TimeToBeat ?? 0) - (long)gameHowLongToBeat.Game.Playtime;
                PlayTimeToStringConverterWithZero playTimeToStringConverterWithZero = new PlayTimeToStringConverterWithZero();
                TbRemainingTime.Text = rt > 0 ? (string)playTimeToStringConverterWithZero.Convert(rt, null, null, CultureInfo.CurrentCulture) : string.Empty;

                // Type data
                if (!gameHowLongToBeat?.HasDataEmpty ?? false)
                {
                    switch (gameHowLongToBeat.GetData().GameHltbData.DataType)
                    {
                        case DataType.Classic:
                            Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeClassic");
                            break;
                        case DataType.Average:
                            Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeAverage");
                            break;
                        case DataType.Median:
                            Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeMedian");
                            break;
                        case DataType.Rushed:
                            Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeRushed");
                            break;
                        case DataType.Leisure:
                            Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeLeisure");
                            break;
                        default:
                            Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeLeisure");
                            break;
                    }
                }
            }
            else
            {
                Hltb_El0.Visibility = Visibility.Collapsed;
            }
        }


        private void SetColor(int elIndicator, Color color)
        {
            switch (elIndicator)
            {
                case 1:
                    Hltb_El1_Color.Background = new SolidColorBrush(color);
                    break;

                case 2:
                    Hltb_El2_Color.Background = new SolidColorBrush(color);
                    break;

                case 3:
                    Hltb_El3_Color.Background = new SolidColorBrush(color);
                    break;

                default:
                    break;
            }
        }

        private void SetDataInView(int elIndicator, string elText, string elData, string elDataUser, int idx)
        {
            TextBlock hltb_1 = new TextBlock();
            TextBlock hltb_2 = new TextBlock();
            TextBlock hltb_3 = new TextBlock();

            switch (idx)
            {
                case 0:
                    hltb_1 = Hltb_El1_DataUser;
                    hltb_2 = Hltb_El2_DataUser;
                    hltb_3 = Hltb_El3_DataUser;
                    break;

                case 1:
                    hltb_1 = Hltb_El1_DataUser_1;
                    hltb_2 = Hltb_El2_DataUser_1;
                    hltb_3 = Hltb_El3_DataUser_1;
                    break;

                case 2:
                    hltb_1 = Hltb_El1_DataUser_2;
                    hltb_2 = Hltb_El2_DataUser_2;
                    hltb_3 = Hltb_El3_DataUser_2;
                    break;

                case 3:
                    hltb_1 = Hltb_El1_DataUser_3;
                    hltb_2 = Hltb_El2_DataUser_3;
                    hltb_3 = Hltb_El3_DataUser_3;
                    break;

                default:
                    break;
            }


            switch (elIndicator)
            {
                case 1:
                    Hltb_El1.Text = elText;
                    Hltb_El1_Data.Text = elData;
                    hltb_1.Text = elDataUser;

                    hltb_1.Visibility = Visibility.Visible;
                    Hltb_El1.Visibility = Visibility.Visible;
                    Hltb_El1_Color.Visibility = Visibility.Visible;
                    break;

                case 2:
                    Hltb_El2.Text = elText;
                    Hltb_El2_Data.Text = elData;
                    hltb_2.Text = elDataUser;

                    hltb_2.Visibility = Visibility.Visible;
                    Hltb_El2.Visibility = Visibility.Visible;
                    Hltb_El2_Color.Visibility = Visibility.Visible;
                    break;

                case 3:
                    Hltb_El3.Text = elText;
                    Hltb_El3_Data.Text = elData;
                    hltb_3.Text = elDataUser;

                    hltb_3.Visibility = Visibility.Visible;
                    Hltb_El3.Visibility = Visibility.Visible;
                    Hltb_El3_Color.Visibility = Visibility.Visible;
                    break;

                default:
                    break;
            }
        }


        private void PART_SourceLink_Click(object sender, RoutedEventArgs e)
        {
            string url = (string)((Hyperlink)sender).Tag;
            if (!url.IsNullOrEmpty())
            {
                _ = Process.Start((string)((Hyperlink)sender).Tag);
            }
        }

        private void CoverImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var fe = sender as FrameworkElement;
                string url = fe?.Tag as string;
                if (url.IsNullOrEmpty())
                {
                    if (DataContext is HowLongToBeatViewData ctx && ctx.SourceLink != null)
                    {
                        url = ctx.SourceLink.Url;
                    }
                }
                if (!url.IsNullOrEmpty())
                {
                    _ = Process.Start(url);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Guid id = (Guid)((FrameworkElement)sender).Tag;
                if (id != default)
                {
                    _ = HowLongToBeat.PluginDatabase.Remove(id);
                    ((Window)Parent).Close();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Guid id = (Guid)((FrameworkElement)sender).Tag;
                if (id != default)
                {
                    HowLongToBeat.PluginDatabase.Refresh(id);
                    GameHowLongToBeat gameHowLongToBeat = PluginDatabase.Get(id, true);
                    Init(gameHowLongToBeat);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;

            Typeface typeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch);

            FormattedText formattedText = new FormattedText(
                textBlock.Text,
                System.Threading.Thread.CurrentThread.CurrentCulture,
                textBlock.FlowDirection,
                typeface,
                textBlock.FontSize,
                textBlock.Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            ((ToolTip)((TextBlock)sender).ToolTip).Visibility = formattedText.Width > textBlock.DesiredSize.Width ? Visibility.Visible : Visibility.Hidden;
        }


        private void Hltb_El0_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                RadioButton rb = sender as RadioButton;
                if (rb.Tag != null)
                {
                    GameHowLongToBeat.UserGameId = rb.Tag.ToString();
                    PluginDatabase.Update(GameHowLongToBeat);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ViewData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HowLongToBeatViewData.CoverImage))
            {
                if (sender is HowLongToBeatViewData vm)
                {
                    if (Dispatcher != null)
                    {
                        _ = Dispatcher.BeginInvoke(new Action(() => SetCoverImageSource(vm.CoverImage)));
                    }
                }
            }
        }

        private void AttachViewModel()
        {
            try
            {
                // Detach previous
                if (_viewDataContext != null)
                {
                    _viewDataContext.PropertyChanged -= ViewData_PropertyChanged;

                    try
                    {
                        _coverLoadCts?.Cancel();
                        _coverLoadCts?.Dispose();
                    }
                    catch { }
                }

                _viewDataContext = DataContext as HowLongToBeatViewData;
                if (_viewDataContext != null)
                {
                    _viewDataContext.PropertyChanged += ViewData_PropertyChanged;
                }
            }
            catch { }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel();
        }

        private void HowLongToBeatView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewDataContext != null)
                {
                    _viewDataContext.PropertyChanged -= ViewData_PropertyChanged;
                    _viewDataContext = null;
                }

                try
                {
                    _coverLoadCts?.Cancel();
                    _coverLoadCts?.Dispose();
                    _coverLoadCts = null;
                }
                catch { }

                this.DataContextChanged -= OnDataContextChanged;
                this.Unloaded -= HowLongToBeatView_Unloaded;
            }
            catch { }
        }
    }


    public class HowLongToBeatViewData : ObservableObject
    {
        private Game _gameContext;
        public Game GameContext { get => _gameContext; set => SetValue(ref _gameContext, value); }

        private SourceLink _sourceLink;
        public SourceLink SourceLink { get => _sourceLink; set => SetValue(ref _sourceLink, value); }

        private string _coverImage = string.Empty;
        public string CoverImage { get => _coverImage; set => SetValue(ref _coverImage, value); }

        private double _mainHours = 0;
        public double MainHours { get => _mainHours; set => SetValue(ref _mainHours, value); }

        private double _mainExtraHours = 0;
        public double MainExtraHours { get => _mainExtraHours; set => SetValue(ref _mainExtraHours, value); }

        private double _completionistHours = 0;
        public double CompletionistHours { get => _completionistHours; set => SetValue(ref _completionistHours, value); }

        private string _mainHoursFormat = string.Empty;
        public string MainHoursFormat { get => _mainHoursFormat; set => SetValue(ref _mainHoursFormat, value); }

        private string _mainExtraHoursFormat = string.Empty;
        public string MainExtraHoursFormat { get => _mainExtraHoursFormat; set => SetValue(ref _mainExtraHoursFormat, value); }

        private string _completionistHoursFormat = string.Empty;
        public string CompletionistHoursFormat { get => _completionistHoursFormat; set => SetValue(ref _completionistHoursFormat, value); }
    }
}

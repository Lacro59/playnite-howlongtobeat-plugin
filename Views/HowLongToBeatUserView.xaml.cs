using HowLongToBeat.Services;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatUserView.xaml
    /// </summary>
    public partial class HowLongToBeatUserView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection;


        public HowLongToBeatUserView()
        {
            InitializeComponent();

            ListViewGames.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList;
            Sorting();
        }


        #region Functions sorting ListviewGames.
        private void Sorting()
        {
            // Sorting
            try
            {
                var columnBinding = _lastHeaderClicked.Column.DisplayMemberBinding as Binding;
                var sortBy = columnBinding?.Path.Path ?? _lastHeaderClicked.Column.Header as string;
            }
            // If first view
            catch
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListViewGames.ItemsSource);
                if (view != null)
                {
                    _lastHeaderClicked = lvName;
                    _lastHeaderClicked.Content += " ▲";
                    view.SortDescriptions.Add(new SortDescription("Name", _lastDirection));
                }
            }
        }

        private void ListviewGames_onHeaderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                lvMainStoryValue.IsEnabled = true;
                lvMainExtraValue.IsEnabled = true;
                lvCompletionistValue.IsEnabled = true;
                lvSoloValue.IsEnabled = true;
                lvCoOpValue.IsEnabled = true;
                lvVsValue.IsEnabled = true;


                var headerClicked = e.OriginalSource as GridViewColumnHeader;
                ListSortDirection direction;

                if (headerClicked != null)
                {
                    if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                    {
                        if (headerClicked != _lastHeaderClicked)
                        {
                            direction = ListSortDirection.Ascending;
                        }
                        else
                        {
                            if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }
                        }

                        var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                        var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                        // Specific sort with another column
                        if (headerClicked.Name == "lvMainStory")
                        {
                            columnBinding = lvMainStoryValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvMainExtra")
                        {
                            columnBinding = lvMainExtraValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvCompletionist")
                        {
                            columnBinding = lvCompletionistValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvSolo")
                        {
                            columnBinding = lvSoloValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvCoOp")
                        {
                            columnBinding = lvCoOpValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvVs")
                        {
                            columnBinding = lvVsValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }

                        Sort(sortBy, direction);

                        if (_lastHeaderClicked != null)
                        {
                            _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▲", string.Empty);
                            _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▼", string.Empty);
                        }

                        if (direction == ListSortDirection.Ascending)
                        {
                            headerClicked.Content += " ▲";
                        }
                        else
                        {
                            headerClicked.Content += " ▼";
                        }

                        // Remove arrow from previously sorted header
                        if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                        {
                            _lastHeaderClicked.Column.HeaderTemplate = null;
                        }

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }

                lvMainStoryValue.IsEnabled = false;
                lvMainExtraValue.IsEnabled = false;
                lvCompletionistValue.IsEnabled = false;
                lvSoloValue.IsEnabled = false;
                lvCoOpValue.IsEnabled = false;
                lvVsValue.IsEnabled = false;
            }
            catch
            {

            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(ListViewGames.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
        #endregion


        private void PART_BtRefreshUserData_Click(object sender, RoutedEventArgs e)
        {
            ListViewGames.ItemsSource = null;

            PluginDatabase.RefreshUserData();

            ListViewGames.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList;
            Sorting();
        }
    }
}

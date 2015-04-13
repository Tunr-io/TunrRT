using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TunrLibrary.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Phone.UI.Input;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace TunrRT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private ListView ListPlaylist;
        private bool ListViewReorderPressed = false;
        private ListViewReorderMode _PlaylistReorderMode;
        public ListViewReorderMode PlaylistReorderMode {
            get
            {
                return _PlaylistReorderMode;
            }
            set
            {
                if (_PlaylistReorderMode == ListViewReorderMode.Disabled && value == ListViewReorderMode.Enabled)
                {
                    ListPlaylist.SelectionMode = ListViewSelectionMode.Single;
                    // Collapse all other app bar buttons (assume we are on playlist hubitem)
                    AppBarClearList.Visibility = Visibility.Collapsed;
                    _PlaylistReorderMode = value;
                    // Show playlist edit controls
                    AppBarPlaylistRemove.Visibility = Visibility.Visible;
                    
                } else if (_PlaylistReorderMode == ListViewReorderMode.Enabled && value == ListViewReorderMode.Disabled)
                {
                    ListPlaylist.SelectionMode = ListViewSelectionMode.None;
                    // Collapse playlist edit controls
                    AppBarPlaylistRemove.Visibility = Visibility.Collapsed;
                    _PlaylistReorderMode = value;
                    // Update other appbar buttons
                    UpdateAppBarButtons();
                }
                
                OnPropertyChanged("PlaylistReorderMode");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        // Create the OnPropertyChanged method to raise the event 
        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Fix up that status bar
            var statusBar = StatusBar.GetForCurrentView();
            statusBar.BackgroundColor = Colors.Black;
            statusBar.ForegroundColor = Colors.White;
            statusBar.BackgroundOpacity = 0.5;

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
#pragma warning disable CS4014
            (DataContext as DataSource).Synchronize();
#pragma warning restore CS4014

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if ((string)Hub.SectionsInView[0].Tag == "0")
            {
                if ((DataContext as DataSource).BrowseLists.Count > 1)
                {
                    (DataContext as DataSource).GoBackTo((DataContext as DataSource).BrowseLists[(DataContext as DataSource).BrowseLists.Count-2]);
                    e.Handled = true;
                }
            }
        }

        private void ListPlaylist_Loaded(object sender, RoutedEventArgs e)
        {
            ListPlaylist = (ListView)sender;
        }

        private void ListPlaylist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            PlaylistReorderMode = ListViewReorderMode.Enabled;
        }

        private void ListPlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Item click: " + ListPlaylist.SelectedIndex + " / " + (e.ClickedItem as Song).TagTitle);
            if (PlaylistReorderMode == ListViewReorderMode.Enabled)
            {
                ListViewReorderPressed = true;
                ListPlaylist.SelectedItem = e.ClickedItem;
            } else
            {
                (App.Current as App).BackgroundAudioHandler.PlayItem(e.ClickedItem as PlaylistItem);
            }
        }

        private void ListPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("tapped: " + ListPlaylist.SelectedIndex);
            if (PlaylistReorderMode == ListViewReorderMode.Enabled)
            {
                if (!ListViewReorderPressed)
                {
                    PlaylistReorderMode = ListViewReorderMode.Disabled;
                }
                ListViewReorderPressed = false;
            }
        }

        private void Hub_SectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
        {
            UpdateAppBarButtons();
        }

        private void UpdateAppBarButtons()
        {
            switch ((string)Hub.SectionsInView[0].Tag)
            {
                case "0":
                    AppBarSearch.Visibility = Visibility.Visible;
                    AppBarClearList.Visibility = Visibility.Collapsed;
                    AppBarRepeat.Visibility = Visibility.Collapsed;
                    AppBarShuffle.Visibility = Visibility.Collapsed;
                    PlaylistReorderMode = ListViewReorderMode.Disabled; // force disable re-order mode if we pan hub items
                    break;
                case "1":
                    AppBarSearch.Visibility = Visibility.Collapsed;
                    AppBarClearList.Visibility = Visibility.Visible;
                    AppBarRepeat.Visibility = Visibility.Collapsed;
                    AppBarShuffle.Visibility = Visibility.Collapsed;
                    break;
                case "2":
                    AppBarSearch.Visibility = Visibility.Collapsed;
                    AppBarClearList.Visibility = Visibility.Collapsed;
                    AppBarRepeat.Visibility = Visibility.Visible;
                    AppBarShuffle.Visibility = Visibility.Visible;
                    PlaylistReorderMode = ListViewReorderMode.Disabled; // force disable re-order mode if we pan hub items
                    break;
            }
        }

        private async void AppBarClearList_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog md = new MessageDialog("This will clear your entire playlist, and cannot be undone.", "Are you sure?");
            bool? result = null;
            md.Commands.Add(new UICommand("Clear", new UICommandInvokedHandler((cmd) => result = true)));
            md.Commands.Add(new UICommand("Cancel", new UICommandInvokedHandler((cmd) => result = false)));

            await md.ShowAsync();
            if (result == true)
            {
                (DataContext as DataSource).PlaylistItems.Clear();
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App).BackgroundAudioHandler.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App).BackgroundAudioHandler.Pause();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App).BackgroundAudioHandler.Previous();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App).BackgroundAudioHandler.Next();
        }

        private void AppBarPlaylistRemove_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistReorderMode == ListViewReorderMode.Enabled)
            {
                var toRemove = (ListPlaylist.SelectedItem as PlaylistItem);
                if (toRemove != null)
                {
                    (DataContext as DataSource).PlaylistItems.Remove(toRemove);
                }
            }
        }

        private async void AppBarSearch_Click(object sender, RoutedEventArgs e)
        {
            var md = new MessageDialog("This feature will be coming soon. We'll let you know when it's working!", "Oops!");
            await md.ShowAsync();
        }

        private async void AppBarRepeat_Click(object sender, RoutedEventArgs e)
        {
            var md = new MessageDialog("This feature will be coming soon. We'll let you know when it's working!", "Oops!");
            await md.ShowAsync();
        }

        private async void AppBarShuffle_Click(object sender, RoutedEventArgs e)
        {
            var md = new MessageDialog("This feature will be coming soon. We'll let you know when it's working!", "Oops!");
            await md.ShowAsync();
        }

        private async void AppBarLogout_Click(object sender, RoutedEventArgs e)
        {
            var md = new MessageDialog("This feature will be coming soon. We'll let you know when it's working! (In the meantime, you can re-install the app).", "Oops!");
            await md.ShowAsync();
        }
    }
}

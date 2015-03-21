using TunrRT.Common;
using TunrRT.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Playback;
using TunrLibrary;
using TunrLibrary.Models;

// The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace TunrRT
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private readonly NavigationHelper navigationHelper;
		private ListView ListPlaylist;

		private bool ListViewReorderPressed = false;

        public HubPage()
        {
            this.InitializeComponent();
			DataContext = (App.Current as App).DataSource;

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
			StatusBar statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
			await statusBar.HideAsync();
			(DataContext as DataSource).Synchronize();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			(App.Current as App).BackgroundAudioHandler.Play();
		}

		private void Hub_SectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
		{
			switch ((string)Hub.SectionsInView[0].Tag)
			{
				case "0":
					AppBarSearch.Visibility = Visibility.Visible;
					AppBarClearList.Visibility = Visibility.Collapsed;
					AppBarRepeat.Visibility = Visibility.Collapsed;
					AppBarShuffle.Visibility = Visibility.Collapsed;
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
					break;
			}
		}

		private async void AppBarClearList_Click(object sender, RoutedEventArgs e)
		{
			await LibraryManager.ClearPlaylist(Guid.Empty);
			System.Diagnostics.Debug.WriteLine("Playlist cleared.");
		}

		private void ListPlaylist_Holding(object sender, HoldingRoutedEventArgs e)
		{
			ListPlaylist.SelectionMode = ListViewSelectionMode.Single;
			ListPlaylist.ReorderMode = ListViewReorderMode.Enabled;
			ListPlaylist.SelectedIndex = 0;
		}

		private void ListPlaylist_Loaded(object sender, RoutedEventArgs e)
		{
			ListPlaylist = (ListView)sender;
		}

		private void ListPlaylist_ItemClick(object sender, ItemClickEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("Item click: " + ListPlaylist.SelectedIndex + " / " + (e.ClickedItem as Song).TagTitle);
			if (ListPlaylist.ReorderMode == ListViewReorderMode.Enabled)
			{
				ListViewReorderPressed = true;
			}
		}

		private void ListPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("tapped: " + ListPlaylist.SelectedIndex);
			if (ListPlaylist.ReorderMode == ListViewReorderMode.Enabled)
			{
				if (!ListViewReorderPressed)
				{
					ListPlaylist.SelectionMode = ListViewSelectionMode.None;
					ListPlaylist.ReorderMode = ListViewReorderMode.Disabled;
					ListViewReorderPressed = false;
				}
				ListViewReorderPressed = false;
			}
			
			
		}
    }
}

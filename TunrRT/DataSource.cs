using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary;
using TunrLibrary.Models;
using TunrRT.DataModel.Models;
using TunrRT.Models;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;

namespace TunrRT
{
    public class DataSource : INotifyPropertyChanged
    {
        /// <summary>
        /// The base URL of the Tunr web service.
        /// </summary>
#if DEBUG
        public const string BASEURL = "https://play.tunr.io";
#else
		public const string BASEURL = "https://play.tunr.io";
#endif
        /// <summary>
        /// Store authentication token for web requests
        /// </summary>
        public AuthenticationToken AuthToken { get; set; }

        /// <summary>
        /// Each list in the library browser
        /// </summary>
        public ObservableCollection<LibraryList> BrowseLists { get; set; }

        /// <summary>
        /// Returns the 'history' of browse lists - items in the back-stack
        /// </summary>
        public List<LibraryList> LibraryListBackStack
        {
            get
            {
                if (BrowseLists.Count > 1)
                {
                    return BrowseLists.Take(BrowseLists.Count - 1).ToList();
                }
                return new List<LibraryList>();
            }
        }

        /// <summary>
        /// Returns the last browse list - the currently viewed list
        /// </summary>
        public LibraryList CurrentLibraryList {
            get
            {
                return BrowseLists.LastOrDefault();
            }
        }

        /// <summary>
        /// Current playlist to be displayed to the user
        /// </summary>
        public ObservableCollection<PlaylistItem> PlaylistItems { get; set; }

        /// <summary>
        /// Returns true if the background task is currently playing
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return (App.Current as App).BackgroundAudioHandler.PlayerState == Windows.Media.Playback.MediaPlayerState.Playing;
            }
        }

        /// <summary>
        /// Returns the song currently queued/playing in the background task
        /// </summary>
        public Song PlayingSong
        {
            get
            {
                var playlistItemId = (Guid?)(ApplicationSettingsHelper.ReadSettingsValue(GlobalConstants.KeyCurrentPlaylistItemId));
                if (playlistItemId == null)
                {
                    return null;
                }
                return LibraryManager.FetchPlaylistItem((Guid)playlistItemId).Song;
            }
        }

        /// <summary>
        /// Is busy boolean. Bind indeterminate progress bar to this to determine
        /// if we're ever busy doing something.
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return IsSynchronizing;
            }
        }
        
        /// <summary>
        /// Whether or not we are currently busy synchronizing.
        /// </summary>
        protected bool IsSynchronizing
        {
            get
            {
                return _isSynchronizing;
            }
            set
            {
                _isSynchronizing = value;
                OnPropertyChanged("IsBusy");
            }
        }
        protected bool _isSynchronizing = false;

        /// <summary>
		/// List of property names in the order they're used to filter
		/// songs in the library browser.
		/// </summary>
		public string[] LibraryFilterTreePropertyNames = { "TagFirstPerformer", "TagAlbum", "TagTitle" };

        /// <summary>
        /// Associates filtered properties with their data models
        /// </summary>
        public Dictionary<string, Type> FilterListTypes = new Dictionary<string, Type> {
            { "TagFirstPerformer", typeof(ArtistList) },
            { "TagAlbum", typeof(AlbumList) },
            { "TagTitle", typeof(TrackList) }
        };

        /// <summary>
        /// Contains a list of the actual reflrection propertyinfo objects
        /// used to filter songs in the library browser.
        /// </summary>
        protected PropertyInfo[] LibraryFilterTreeProperties
        {
            get
            {
                if (_LibraryFilterTreeProperties == null
                    || _LibraryFilterTreeProperties.Length != LibraryFilterTreePropertyNames.Length)
                {
                    _LibraryFilterTreeProperties = new PropertyInfo[LibraryFilterTreePropertyNames.Length];
                    for (int i = 0; i < _LibraryFilterTreeProperties.Length; i++)
                    {
                        _LibraryFilterTreeProperties[i] = typeof(Song).GetRuntimeProperty(LibraryFilterTreePropertyNames[i]);
                    }
                }
                return _LibraryFilterTreeProperties;
            }
        }
        protected PropertyInfo[] _LibraryFilterTreeProperties;

        /// <summary>
        /// Property changed event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public DataSource()
        {
            BrowseLists = new ObservableCollection<LibraryList>();
            PlaylistItems = new ObservableCollection<PlaylistItem>();
            (App.Current as App).BackgroundAudioHandler.PropertyChanged += BackgroundAudioHandler_PropertyChanged;
            (App.Current as App).BackgroundAudioHandler.TrackChanged += BackgroundAudioHandler_TrackChanged;

            // Load playlist items
            var loadedPlaylist = LibraryManager.FetchPlaylistItems(Guid.Empty);
            foreach (var pitem in loadedPlaylist)
            {
                PlaylistItems.Add(pitem);
            }

            PlaylistItems.CollectionChanged += PlaylistItems_CollectionChanged;

            // Set auth if we've authenticated in the past...
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Authentication"))
            {
                AuthToken = JsonConvert.DeserializeObject<AuthenticationToken>((string)ApplicationData.Current.LocalSettings.Values["Authentication"]);
            }

            // Trigger a library cache preload
            LibraryManager.PreloadSongs();
            // Bind an event to detect library updates
            LibraryManager.LibraryUpdate += LibraryManager_LibraryUpdate;
            
            // Trigger event when BrowseLists is updated
            BrowseLists.CollectionChanged += BrowseLists_CollectionChanged;

            var firstProperty = LibraryFilterTreeProperties.FirstOrDefault();
            var firstType = FilterListTypes[firstProperty.Name];
            var firstList = (LibraryList)(Activator.CreateInstance(firstType, this, "Music", LibraryFilterTreeProperties.FirstOrDefault(), new List<SongFilter>()));
            BrowseLists.Add(firstList);
        }

        /// <summary>
        /// Triggers property updates when browselists are added/removed
        /// </summary>
        private void BrowseLists_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("LibraryListBackStack");
            OnPropertyChanged("CurrentLibraryList");
        }


        /// <summary>
        /// Runs whenever the playlist viewmodel is changed in order to update the backing database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    LibraryManager.DeletePlaylistItem(((PlaylistItem)item).PlaylistItemId);
                }
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                LibraryManager.UpdatePlaylistItemsAsync(PlaylistItems.ToList());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        private void BackgroundAudioHandler_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "PlayerState":
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    {
                        OnPropertyChanged("IsPlaying");
                    });
                    break;
            }
        }

        private void BackgroundAudioHandler_TrackChanged(object sender, EventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            {
                OnPropertyChanged("PlayingSong");
            });
        }

        private void LibraryManager_LibraryUpdate(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Library update detected by datasource.");
            // TODO: Update the browse lists
        }

        public async Task<bool> Authenticate(string email, string password)
        {
            using (var client = new HttpClient())
            {
                // New code:
                client.BaseAddress = new Uri(BASEURL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", email),
                    new KeyValuePair<string, string>("password", password)
                });
                HttpResponseMessage response = await client.PostAsync("/Token", content);
                if (response.IsSuccessStatusCode)
                {
                    var auth = await response.Content.ReadAsAsync<AuthenticationToken>();
                    AuthToken = auth;
                    ApplicationData.Current.LocalSettings.Values["Authentication"] = JsonConvert.SerializeObject(auth);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task Synchronize()
        {
            System.Diagnostics.Debug.WriteLine("Synchronizing with Tunr...");
            IsSynchronizing = true;
            var latestGuid = (Guid?)ApplicationSettingsHelper.ReadSettingsValue(GlobalConstants.KeyLatestSyncId);
            // If we have no sync id, we have to obtain one
            try { 
                if (latestGuid == null)
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(BASEURL);
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken.access_token);
                        var response = await client.GetAsync("api/Library/Sync");
                        var syncResult = await response.Content.ReadAsAsync<SyncBase>();
                        System.Diagnostics.Debug.WriteLine("Fetched " + syncResult.Library.Count + " songs.");
                        await LibraryManager.AddOrUpdateSongs(syncResult.Library);
                        System.Diagnostics.Debug.WriteLine("Updating to sync id " + syncResult.LastSyncId);
                        ApplicationSettingsHelper.SaveSettingsValue(GlobalConstants.KeyLatestSyncId, syncResult.LastSyncId);
                        // TODO: Remove songs that have been deleted from Tunr
                    }
                    System.Diagnostics.Debug.WriteLine("Synchronization complete.");
                    IsSynchronizing = false;
                } else
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(BASEURL);
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken.access_token);
                        var response = await client.GetAsync("api/Library/Sync/" + latestGuid);
                        var changeSets = await response.Content.ReadAsAsync<List<ChangeSetDetails>>();
                        var songsToAddUpdate = new List<Song>();
                        System.Diagnostics.Debug.WriteLine("Fetched " + changeSets.Count + " change sets");
                        if (changeSets.Count > 0)
                        {
                            foreach (var changeSet in changeSets)
                            {
                                System.Diagnostics.Debug.WriteLine("Processing changeset " + changeSet.ChangeSetId + "...");
                                foreach (var change in changeSet.Changes)
                                {
                                    if (change.Key == ChangeSetDetails.ChangeType.Create || change.Key == ChangeSetDetails.ChangeType.Update)
                                    {
                                        songsToAddUpdate.Add(change.Value);
                                    }

                                    // TODO: Handle delete
                                }
                            }
                            System.Diagnostics.Debug.WriteLine("Adding " + songsToAddUpdate.Count + " to library from changes...");
                            await LibraryManager.AddOrUpdateSongs(songsToAddUpdate);
                            System.Diagnostics.Debug.WriteLine("Updating to sync id " + changeSets.LastOrDefault().ChangeSetId);
                            ApplicationSettingsHelper.SaveSettingsValue(GlobalConstants.KeyLatestSyncId, changeSets.LastOrDefault().ChangeSetId);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("Synchronization complete.");
                    IsSynchronizing = false;
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Failed to synchronize with Tunr.io.");
                IsSynchronizing = false;
            }
        }

        /// <summary>
		/// This method prepares a new browselist based on a selected song,
		/// and a 'target property' on which to filter.
		/// </summary>
		/// <param name="target">Selected song</param>
		/// <param name="targetProperty">Property on which to filter the next list</param>
		public async void SelectFilter(Song target, PropertyInfo targetProperty)
        {
            if (targetProperty.Equals(LibraryFilterTreeProperties.LastOrDefault()))
            {
                var newPlaylistItem = await LibraryManager.AddSongToPlaylistAsync(target);
                PlaylistItems.Add(newPlaylistItem);
                if (PlaylistItems.Count == 1)
                {
                    ((App.Current) as App).BackgroundAudioHandler.Play();
                }
                return;
            }
            var propertyValue = targetProperty.GetValue(target, null);
            var newFilter = new List<SongFilter>(BrowseLists.Last().Filters);
            newFilter.Add(new SongFilter()
            {
                Property = targetProperty,
                Value = propertyValue
            });

            var nextProperty = LibraryFilterTreeProperties[BrowseLists.Count];
            var nextType = FilterListTypes[nextProperty.Name];
            var nextList = (LibraryList)(Activator.CreateInstance(nextType, this, (string)propertyValue, nextProperty, newFilter));
            BrowseLists.LastOrDefault().IsVisible = false;
            BrowseLists.Add(nextList);
        }

        /// <summary>
        /// Navigate back to this list in the back-stack
        /// </summary>
        /// <param name="list"></param>
        public void GoBackTo(LibraryList list)
        {
            var listIndex = BrowseLists.IndexOf(list);
            for (int i = BrowseLists.Count - 1; i > listIndex; i--)
            {
                BrowseLists.RemoveAt(i);
            }
            BrowseLists.LastOrDefault().IsVisible = true;
        }
    }
}

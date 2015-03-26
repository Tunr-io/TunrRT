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
using Windows.Storage;

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
        private bool IsSynchronizing
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
        private bool _isSynchronizing = false;

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
        private PropertyInfo[] LibraryFilterTreeProperties
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
        private PropertyInfo[] _LibraryFilterTreeProperties;

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
            // Instantiate browselists
            BrowseLists = new ObservableCollection<LibraryList>();

            // Set auth if we've authenticated in the past...
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Authentication"))
            {
                AuthToken = JsonConvert.DeserializeObject<AuthenticationToken>((string)ApplicationData.Current.LocalSettings.Values["Authentication"]);
            }

            // Bind an event to detect library updates
            LibraryManager.LibraryUpdate += LibraryManager_LibraryUpdate;

            var firstProperty = LibraryFilterTreeProperties.FirstOrDefault();
            var firstType = FilterListTypes[firstProperty.Name];
            var firstList = (LibraryList)(Activator.CreateInstance(firstType, this, "Music", LibraryFilterTreeProperties.FirstOrDefault(), new List<SongFilter>()));
            BrowseLists.Add(firstList);
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
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(BASEURL);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken.access_token);
                    var response = await client.GetAsync("api/Library");
                    var songs = await response.Content.ReadAsAsync<List<Song>>();
                    System.Diagnostics.Debug.WriteLine("Fetched " + songs.Count + " songs.");
                    await LibraryManager.AddOrUpdateSongs(songs);
                    // TODO: Remove songs that have been deleted from Tunr
                }
                System.Diagnostics.Debug.WriteLine("Synchronization complete.");
                IsSynchronizing = false;
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
		public void SelectFilter(Song target, PropertyInfo targetProperty)
        {
            //if (targetProperty.Name.ToLower() == "tagtitle")
            //{
            //    await LibraryManager.AddSongToPlaylistAsync(target);
            //    PlaylistItems.Add(target);
            //    OnPropertyChanged("PlaylistItems");
            //    ((App.Current) as App).BackgroundAudioHandler.Play();
            //    return;
            //}
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

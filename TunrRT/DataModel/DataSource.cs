﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TunrRT.DataModel.Models;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Reflection;
using TunrLibrary.Models;
using TunrLibrary;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using System.Threading;

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace TunrRT.DataModel
{
    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// SampleDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public class DataSource : INotifyPropertyChanged
    {
		/// <summary>
		/// Contains the names of properties that can be used for filtering
		/// songs in the library browser.
		/// </summary>
		public readonly string[] FilterablePropertyNames = { "TagPerformers", "TagAlbum", "TagTitle", "TagGenre", "TagYear" };

		/// <summary>
		/// List of property names in the order they're used to filter
		/// songs in the library browser.
		/// </summary>
		public string[] LibraryFilterTreePropertyNames = { "TagFirstPerformer", "TagAlbum", "TagTitle" };

		private PropertyInfo[] _LibraryFilterTreeProperties;
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

		/// <summary>
		/// The base URL of the Tunr web service.
		/// </summary>
#if DEBUG
		public const string BASEURL = "https://play.tunr.io";
#else
		public const string BASEURL = "https://play.tunr.io";
#endif

		/// <summary>
		/// Authentication token used to authenticate web requests.
		/// </summary>
		private AuthenticationToken AuthToken;

		/// <summary>
		/// Collection of each loaded list in the library browser.
		/// </summary>
		public ObservableCollection<LibraryList> BrowseLists { get; set; }

		/// <summary>
		/// The library list currently being displayed to the user.
		/// </summary>
		public LibraryList ActiveList
		{
			get
			{
				return BrowseLists.LastOrDefault();
			}
		}

		/// <summary>
		/// Property changed event.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Used to trigger property changed events
		/// </summary>
		/// <param name="name">Name of property changed</param>
		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

		public DataSource(AuthenticationToken auth = null)
		{
			BrowseLists = new ObservableCollection<LibraryList>();
			BrowseLists.CollectionChanged += BrowseLists_CollectionChanged;

			AuthToken = auth;

			BrowseLists.Add(new LibraryList(this)
			{
				Filters = new List<SongFilter>(),
				ListName = "Music",
				TargetProperty = LibraryFilterTreeProperties.FirstOrDefault()
			});
		}

		void BrowseLists_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged("ActiveList");
		}
		
		/// <summary>
		/// Checks to see that we have a valid authentication token set
		/// </summary>
		/// <returns>True on authenticated, false otherwise</returns>
		public bool IsAuthenticated()
		{
			// TODO: Actually verify that this token hasn't expired, etc.
			return (AuthToken != null);
		}

        public async Task<AuthenticationToken> SetCredentialsAsync(string email, string password)
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
					return auth;
				}
				else
				{
					throw new Exception("Failed to authenticate.");
				}
			}
		}

		public async void Synchronize()
		{
			System.Diagnostics.Debug.WriteLine("Synchronizing with Tunr...");
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
					System.Diagnostics.Debug.WriteLine("Database updated.");
					foreach (var list in BrowseLists)
					{
						list.UpdateResults();
					}
				}
			}
			catch (Exception)
			{
				System.Diagnostics.Debug.WriteLine("Failed to synchronize with Tunr.io.");
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
			if (targetProperty.Name.ToLower() == "tagtitle")
			{
				await LibraryManager.AddSongToPlaylistAsync(target);
				((App.Current) as App).BackgroundAudioHandler.Play();
				return;
			}
			var propertyValue = targetProperty.GetValue(target, null);
			var newFilter = new List<SongFilter>(BrowseLists.Last().Filters);
			newFilter.Add(new SongFilter()
			{
				Property = targetProperty,
				Value = propertyValue
			});
			var nextList = new LibraryList(this)
			{
				Filters = newFilter,
				TargetProperty = LibraryFilterTreeProperties[BrowseLists.Count],
				ListName = (string)propertyValue
			};
			BrowseLists.Add(nextList);
		}

		public void SelectList(LibraryList list)
		{
			var index = BrowseLists.IndexOf(list);
			var toRemove = BrowseLists.Where(l => BrowseLists.IndexOf(l) > index).ToList();
			foreach (var removeList in toRemove)
			{
				BrowseLists.Remove(removeList);
			}
		}

	}
}
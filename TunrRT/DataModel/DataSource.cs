using SQLite;
using System;
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
		public readonly string[] FilterProperties = {"Artist", "Album", "Title"};
		public const string BASEURL = "https://play.tunr.io";
		private AuthenticationToken AuthToken;
		private SQLiteAsyncConnection SqlLiteConnection;

		public ObservableCollection<LibraryList> BrowseLists { get; set; }

		public LibraryList ActiveList
		{
			get
			{
				return BrowseLists.LastOrDefault();
			}
		}

		// Implement INotifyPropertyChanged ...
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

		public DataSource()
		{
			BrowseLists = new ObservableCollection<LibraryList>();
			BrowseLists.CollectionChanged += BrowseLists_CollectionChanged;
			InitDb().GetAwaiter().GetResult();

			BrowseLists.Add(new LibraryList(this)
			{
				FilteredPropertyName = FilterProperties[0],
				FilterSong = new Song(),
				ListName = "Music"
			});
		}

		void BrowseLists_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged("ActiveList");
		}

		public async Task InitDb()
		{
			SqlLiteConnection = new SQLiteAsyncConnection("Library.db");
			await SqlLiteConnection.CreateTableAsync<Song>();
		}

        public async Task SetCredentialsAsync(string email, string password)
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
				}
				else
				{
					throw new Exception("Failed to authenticate.");
				}
			}
		}

		public async Task Synchronize()
		{
			System.Diagnostics.Debug.WriteLine("Synchronizing with Tunr...");
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(BASEURL);
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken.access_token);
				var response = await client.GetAsync("api/Library");
				var songs = await response.Content.ReadAsAsync<List<Song>>();
				System.Diagnostics.Debug.WriteLine("Fetched " + songs.Count + " songs.");
				await SqlLiteConnection.InsertOrReplaceAllAsync(songs);
				// TODO: Remove songs that have been deleted from Tunr
				System.Diagnostics.Debug.WriteLine("Database updated.");
			}
		}

		public List<Song> QueryFilteredSongs(Song targetFilter)
		{
			var props = targetFilter.GetType().GetRuntimeProperties();
			var nonnull = props.Where(p => p.GetValue(targetFilter, null) != null).ToList();

			string sqlQuery = "SELECT * FROM Songs ";
			if (nonnull.Count > 0)
			{
				sqlQuery += " WHERE";
			}
			for (int i=0; i<nonnull.Count; i++) {
				var prop = nonnull[i];
				if (i > 0) {
					sqlQuery += " AND";
				}
				sqlQuery += " " + prop.Name + " = ? ";
			}
			var sqlParams = nonnull.Select(p => p.GetValue(targetFilter, null)).ToArray<object>();

			var matches = SqlLiteConnection.QueryAsync<Song>(sqlQuery, sqlParams).GetAwaiter().GetResult();
			return matches;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="targetProperty"></param>
		public void SelectFilter(Song target, string targetProperty)
		{
			var property = target.GetType().GetRuntimeProperties().Where(p => p.Name.ToLower() == targetProperty.ToLower()).FirstOrDefault();
			var propertyValue = property.GetValue(target, null);
			var newSongFilter = BrowseLists.Last().FilterSong.Clone();
			property.SetValue(newSongFilter, propertyValue);
			var nextList = new LibraryList(this)
			{
				FilteredPropertyName = FilterProperties[BrowseLists.Count],
				FilterSong = newSongFilter,
				ListName = (string)propertyValue
			};
			BrowseLists.Add(nextList);
		}

	}
}
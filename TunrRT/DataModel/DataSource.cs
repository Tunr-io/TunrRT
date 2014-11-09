using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TunrRT.DataModel.Models;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace TunrRT.Data
{
    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// SampleDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public class DataSource
    {
		public const string BASEURL = "https://play.tunr.io";
		private AuthenticationToken AuthToken;
		private SQLiteAsyncConnection SqlLiteConnection;

		public DataSource()
		{
			InitDb().GetAwaiter().GetResult();
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
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(BASEURL);
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken.access_token);
				var response = await client.GetAsync("api/Library");
				var songs = await response.Content.ReadAsAsync<List<Song>>();
			}
		}


    }
}
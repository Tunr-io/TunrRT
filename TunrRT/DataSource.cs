using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TunrRT.DataModel.Models;
using Windows.Storage;

namespace TunrRT
{
    public class DataSource
    {
        /// <summary>
        /// The base URL of the Tunr web service.
        /// </summary>
#if DEBUG
        public const string BASEURL = "https://play.tunr.io";
#else
		public const string BASEURL = "https://play.tunr.io";
#endif
        public AuthenticationToken AuthToken { get; set; }

        public DataSource()
        {
            // Set auth if we've authenticated in the past...
            AuthenticationToken auth = null;
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Authentication"))
            {
                auth = JsonConvert.DeserializeObject<AuthenticationToken>((string)ApplicationData.Current.LocalSettings.Values["Authentication"]);
            }
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
    }
}
